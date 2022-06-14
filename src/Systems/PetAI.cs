﻿using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using ProtoBuf;
using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.MathTools;

namespace PetAI
{
    public class PetAI : ModSystem
    {

        Harmony harmony = new Harmony("gerste.petai");
        ICoreServerAPI serverAPI;

        ICoreClientAPI clientAPI;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            MultiplyPatch.Patch(harmony);
            PoulticePatch.Patch(harmony);
            HarvestablePatch.Patch(harmony);

            api.RegisterEntityBehaviorClass("raisable", typeof(EntityBehaviorRaisable));
            api.RegisterEntityBehaviorClass("tameable", typeof(EntityBehaviorTameable));
            api.RegisterEntityBehaviorClass("givecommand", typeof(EntityBehaviorGiveCommand));
            api.RegisterEntityBehaviorClass("receivecommand", typeof(EntityBehaviorReceiveCommand));
            api.RegisterEntityBehaviorClass("interpolatemount", typeof(EntityBehaviorInterpolateMount));
            api.RegisterEntityBehaviorClass("pettableextended", typeof(EntityBehaviorPettableExtended));

            api.RegisterCollectibleBehaviorClass("considerpetfood", typeof(BehaviorConsiderHumanFoodForPetsToo));

            api.RegisterBlockEntityClass("PetNest", typeof(BlockEntityPetNest));
            api.RegisterBlockClass("PetNest", typeof(BlockPetNest));

            AiTaskRegistry.Register<AiTaskTrick>("simplecommand");
            AiTaskRegistry.Register<AiTaskFollowMaster>("followmaster");
            AiTaskRegistry.Register<AiTaskStay>("stay");
            AiTaskRegistry.Register<AiTaskPetMeleeAttack>("petmeleeattack");
            AiTaskRegistry.Register<AiTaskPetSeekEntity>("petseekentity");
            AiTaskRegistry.Register<AiTaskSeekNest>("seeknest");
            AiTaskRegistry.Register<AiTaskBeAMount>("beamount");

            api.RegisterEntity("EntityPet", typeof(EntityPet));
            api.RegisterEntity("EntityMount", typeof(EntityMount));
            api.RegisterMountable("EntityMount", EntityMount.GetMountable);

            api.RegisterItemClass("ItemPetAccessory", typeof(ItemPetAccessory));
            api.RegisterItemClass("ItemPetWhistle", typeof(ItemPetWhistle));

            try
            {
                var Config = api.LoadModConfig<PetConfig>("petconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    PetConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    PetConfig.Current = PetConfig.getDefault();
                }
            }
            catch
            {
                PetConfig.Current = PetConfig.getDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                if (PetConfig.Current.difficulty == null)
                    PetConfig.Current.difficulty = PetConfig.getDefault().difficulty;
                if (PetConfig.Current.petResurrectors == null)
                    PetConfig.Current.petResurrectors = PetConfig.getDefault().petResurrectors;
                if (PetConfig.Current.respawningPets == null)
                    PetConfig.Current.respawningPets = PetConfig.getDefault().respawningPets;

                api.StoreModConfig(PetConfig.Current, "petconfig.json");
            }
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            this.clientAPI = api;

            api.Network.RegisterChannel("petainetwork")
                .RegisterMessageType<PetCommandMessage>()
                .RegisterMessageType<PetProfileMessage>().SetMessageHandler<PetProfileMessage>(OnPetProfileMessageClient)
                .RegisterMessageType<PetNestMessage>().SetMessageHandler<PetNestMessage>(OnPetNestMessageClient);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.serverAPI = api;
            api.Network.RegisterChannel("petainetwork")
                .RegisterMessageType<PetCommandMessage>().SetMessageHandler<PetCommandMessage>(OnPetCommandMessage)
                .RegisterMessageType<PetProfileMessage>().SetMessageHandler<PetProfileMessage>(OnPetProfileMessageServer)
                .RegisterMessageType<PetNestMessage>().SetMessageHandler<PetNestMessage>(OnPetNestMessageServer);
        }

        public override void Dispose()
        {
            base.Dispose();

            MultiplyPatch.Unpatch(harmony);
        }

        private void OnPetCommandMessage(IServerPlayer fromPlayer, PetCommandMessage networkMessage)
        {
            EntityPlayer player = serverAPI.World.PlayerByUid(networkMessage.playerUID)?.Entity;
            EntityAgent target = serverAPI.World.GetEntityById(networkMessage.targetEntityUID) as EntityAgent;
            EnumCommandType commandType;
            if (Enum.TryParse<EnumCommandType>(networkMessage.commandType, out commandType))
            {
                Command command = new Command(commandType, networkMessage.commandName);
                if (command.type == EnumCommandType.SIMPLE && command.commandName == "dropgear")
                {
                    (target as EntityPet)?.DropInventoryOnGround();
                    return;
                }

                if (player != null)
                {
                    player.GetBehavior<EntityBehaviorGiveCommand>().activeCommand = command;
                }
                target?.GetBehavior<EntityBehaviorReceiveCommand>()?.setCommand(command, player);
            }
        }

        private void OnPetProfileMessageServer(IServerPlayer fromPlayer, PetProfileMessage networkMessage)
        {
            EntityAgent target = serverAPI.World.GetEntityById(networkMessage.targetEntityUID) as EntityAgent;
            target.GetBehavior<EntityBehaviorNameTag>()?.SetName(networkMessage.petName);
            if (target?.HasBehavior<EntityBehaviorTameable>() == true)
            {
                var tameable = target.GetBehavior<EntityBehaviorTameable>();
                tameable.multiplyAllowed = networkMessage.multiplyAllowed;

                if (networkMessage.abandon)
                {
                    tameable.ownerId = null;
                    tameable.domesticationLevel = DomesticationLevel.WILD;
                    tameable.domesticationProgress = 0f;
                }

                serverAPI.ModLoader.GetModSystem<PetManager>().UpdatePet(target);
            }
        }

        private void OnPetProfileMessageClient(PetProfileMessage networkMessage)
        {
            if (clientAPI != null)
            {
                EntityAgent entity = clientAPI.World.GetEntityById(networkMessage.oldEntityUID) as EntityAgent;
                if (entity != null) clientAPI.ShowChatMessage(Lang.Get("petai:message-finished-taming", entity.GetName()));
                new PetProfileGUI(clientAPI, networkMessage.targetEntityUID).TryOpen();
            }
        }

        private void OnPetNestMessageServer(IServerPlayer fromPlayer, PetNestMessage networkMessage)
        {
            serverAPI.ModLoader.GetModSystem<PetManager>().SetPetNest(networkMessage.selectedPet, networkMessage.selectedNest);
            var nest = serverAPI.World.BlockAccessor.GetBlockEntity(networkMessage.selectedNest) as BlockEntityPetNest;
            if (nest != null) { nest.petId = networkMessage.selectedPet; }
        }

        private void OnPetNestMessageClient(PetNestMessage networkMessage)
        {
            if (networkMessage.availablePets == null)
            {
                networkMessage.availablePets = new List<PetDataSmall>();
            }
            new PetNestSelect(clientAPI, networkMessage.availablePets, networkMessage.selectedNest).TryOpen();
        }
    }
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class PetCommandMessage
    {
        public string playerUID;
        public string commandName;
        public string commandType;
        public long targetEntityUID;
    }
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class PetProfileMessage
    {
        public string petName;
        public bool multiplyAllowed;
        public bool abandon;
        public long targetEntityUID;
        public long oldEntityUID;
    }
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class PetNestMessage
    {
        public List<PetDataSmall> availablePets;
        public long selectedPet;
        public BlockPos selectedNest;
    }
    public class PetConfig
    {
        public static PetConfig Current { get; set; }

        public Difficulty difficulty { get; set; }
        public HashSet<string> respawningPets { get; set; }
        public List<PetResurrector> petResurrectors { get; set; }
        public int maxPetsPerPlayer { get; set; }
        public bool limitPetsPerPlayer { get; set; }
        public double petRespawnCooldown = 24;

        public static PetConfig getDefault()
        {
            var config = new PetConfig();

            config.respawningPets = new HashSet<string>(new string[] { "tame-wolf-male", "tame-wolf-female", "tame-wolf-pup" });
            config.limitPetsPerPlayer = false;
            config.maxPetsPerPlayer = 5;

            var difficulty = new Difficulty();
            difficulty.tamingMultiplier = 1;
            difficulty.obedienceMultiplier = 1;
            difficulty.disobedienceMultiplier = 1;
            difficulty.growingMultiplier = 1;
            difficulty.tamingMultiplierIncreasePerGen = 0.05f;
            difficulty.obedienceMultiplierIncreasePerGen = 0.05f;
            difficulty.disobedienceMultiplierDecreasePerGen = 0.05f;
            config.difficulty = difficulty;

            var resurrector = new PetResurrector();
            resurrector.itemCode = "gear-temporal";
            resurrector.healingValue = 15;
            config.petResurrectors = new List<PetResurrector>(new PetResurrector[] { resurrector });

            return config;
        }
    }
    public class Difficulty
    {
        public float tamingMultiplier;
        public float obedienceMultiplier;
        public float disobedienceMultiplier;
        public float tamingMultiplierIncreasePerGen;
        public float obedienceMultiplierIncreasePerGen;
        public float disobedienceMultiplierDecreasePerGen;
        public float growingMultiplier;
    }

    public class PetResurrector
    {
        public string itemCode;
        public float healingValue;
    }
}
