using Vintagestory.API.Client;
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
            AiTaskRegistry.Register<AiTaskHappyDance>("happydance");

            api.RegisterEntity("EntityPet", typeof(EntityPet));
            api.RegisterEntity("EntityMount", typeof(EntityMount));
            api.RegisterMountable("EntityMount", EntityMount.GetMountable);

            api.RegisterItemClass("ItemPetAccessory", typeof(ItemPetAccessory));
            api.RegisterItemClass("ItemPetWhistle", typeof(ItemPetWhistle));
            api.RegisterItemClass("ItemTextureRotator", typeof(ItemTextureRotator));

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
                if (PetConfig.Current.resurrectors == null)
                    PetConfig.Current.resurrectors = PetConfig.getDefault().resurrectors;

                api.StoreModConfig(PetConfig.Current, "petconfig.json");
            }
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            this.clientAPI = api;

            api.Network.RegisterChannel("petainetwork")
                .RegisterMessageType<PetCommandMessage>()
                .RegisterMessageType<PetProfileMessage>().SetMessageHandler<PetProfileMessage>(OnPetProfileMessageClient);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.serverAPI = api;
            api.Network.RegisterChannel("petainetwork")
                .RegisterMessageType<PetCommandMessage>().SetMessageHandler<PetCommandMessage>(OnPetCommandMessage)
                .RegisterMessageType<PetProfileMessage>().SetMessageHandler<PetProfileMessage>(OnPetProfileMessageServer);
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
    public class PetConfig
    {
        public static PetConfig Current { get; set; }
        public Difficulty difficulty { get; set; }
        public List<PetResurrector> resurrectors { get; set; }
        public bool pvpOff { get; set; }
        public bool falldamageOff { get; set; }
        public static PetConfig getDefault()
        {
            var config = new PetConfig();

            var difficulty = new Difficulty();
            difficulty.tamingMultiplier = 1;
            difficulty.obedienceMultiplier = 1;
            difficulty.disobedienceMultiplier = 1;
            difficulty.growingMultiplier = 1;
            difficulty.tamingMultiplierIncreasePerGen = 0.05f;
            difficulty.obedienceMultiplierIncreasePerGen = 0.05f;
            difficulty.disobedienceMultiplierDecreasePerGen = 0.05f;
            config.difficulty = difficulty;

            config.pvpOff = false;

            config.resurrectors = new List<PetResurrector>(new PetResurrector[] {
                    new PetResurrector(){name = "bandage-clean", domain ="game", healingValue = 4},
                    new PetResurrector(){name = "bandage-alcoholed", domain ="game", healingValue = 8},
                    new PetResurrector(){name = "poultice-reed-horsetail", domain ="game", healingValue = 1},
                    new PetResurrector(){name = "poultice-reed-honey-sulfur", domain ="game", healingValue = 2},
                    new PetResurrector(){name = "poultice-linen-horsetail", domain ="game", healingValue = 2},
                    new PetResurrector(){name = "poultice-linen-honey-sulfur", domain ="game", healingValue = 3}});

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
        public string name;
        public string domain;
        public float healingValue;
    }
}
