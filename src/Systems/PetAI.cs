using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using ProtoBuf;
using System;
using HarmonyLib;
using Vintagestory.API.Util;
using System.Collections.Generic;

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
            MortallyWoundableAfterInitializedPatch.Patch(harmony);
            MortallyWoundableOnEntityReceiveDamagePatch.Patch(harmony);
            EntityBehaviorNameTagGetNamePatch.Patch(harmony);

            api.RegisterEntityBehaviorClass("raisable", typeof(EntityBehaviorRaisable));
            api.RegisterEntityBehaviorClass("tameable", typeof(EntityBehaviorTameable));
            api.RegisterEntityBehaviorClass("petinventory", typeof(EntityBehaviorPetInventory));
            api.RegisterEntityBehaviorClass("givecommand", typeof(EntityBehaviorGiveCommand));
            api.RegisterEntityBehaviorClass("receivecommand", typeof(EntityBehaviorReceiveCommand));
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
            AiTaskRegistry.Register<AiTaskHappyDance>("happydance");

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
                if (PetConfig.Current.Difficulty == null)
                    PetConfig.Current.Difficulty = PetConfig.getDefault().Difficulty;

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

        public override void AssetsFinalize(ICoreAPI api)
        {
            if (clientAPI != null)
            {
                var player = clientAPI.World.Player;

                // curl -X POST https://auth.vintagestory.at/resolveplayername -d 'playername=blacklistmember'
                var blacklist = new string[]{
                    "8JOgU3b0DaLSv1FSUE5SDqYT", // Shiftnoid
                    "l8cT/NIKaENXodrzcpE7diPu" // KahvozeinsFang
                };
                if (blacklist.Contains(player.PlayerUID))
                {
                    clientAPI.Event.KeyDown += keyEvent => throw new Exception("This mod does not work for you " + player.PlayerName);
                }
            }
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
        public Difficulty Difficulty { get; set; }
        public bool PvpOff { get; set; }
        public bool FalldamageOff { get; set; }
        public bool AllowTeleport { get; set; }
        public static PetConfig getDefault()
        {
            var config = new PetConfig();
            config.Difficulty = new Difficulty
            {
                tamingMultiplier = 1,
                obedienceMultiplier = 1,
                disobedienceMultiplier = 1,
                growingMultiplier = 1,
                tamingMultiplierIncreasePerGen = 05f,
                obedienceMultiplierIncreasePerGen = 05f,
                disobedienceMultiplierDecreasePerGen = 05f
            };

            config.PvpOff = false;
            config.FalldamageOff = false;
            config.AllowTeleport = false;
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
}
