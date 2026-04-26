using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using ProtoBuf;
using System;
using HarmonyLib;

namespace PetAI
{
    public class PetAI : ModSystem
    {

        readonly Harmony harmony = new("gerste.petai");
        ICoreServerAPI serverAPI;

        ICoreClientAPI clientAPI;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            MultiplyPatch.Patch(harmony);
            MortallyWoundableOnEntityReceiveDamagePatch.Patch(harmony);
            EntityBehaviorNameTagGetNamePatch.Patch(harmony);
            EntityBehaviorGrowBecomeAdultPatch.Patch(harmony);
            AiTaskStayCloseToEntityOnNoPathPatch.Patch(harmony);

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
            api.RegisterItemClass("ItemPetCarrier", typeof(ItemPetCarrier));

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
                    PetConfig.Current = new();
                }
            }
            catch
            {
                PetConfig.Current = new();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
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
            if (Enum.TryParse(networkMessage.commandType, out EnumCommandType commandType))
            {
                Command command = new(commandType, networkMessage.commandName);
                if (player != null)
                {
                    player.GetBehavior<EntityBehaviorGiveCommand>().ActiveCommand = command;
                }
                target?.GetBehavior<EntityBehaviorReceiveCommand>()?.SetCommand(command, player);
            }
        }

        private void OnPetProfileMessageServer(IServerPlayer fromPlayer, PetProfileMessage networkMessage)
        {
            EntityAgent target = serverAPI.World.GetEntityById(networkMessage.targetEntityUID) as EntityAgent;
            target.GetBehavior<EntityBehaviorNameTag>()?.SetName(networkMessage.petName);
            if (target?.HasBehavior<EntityBehaviorTameable>() == true)
            {
                var tameable = target.GetBehavior<EntityBehaviorTameable>();
                tameable.MultiplyAllowed = networkMessage.multiplyAllowed;

                if (networkMessage.abandon)
                {
                    tameable.OwnerId = null;
                    tameable.DomesticationLevel = DomesticationLevel.WILD;
                    tameable.DomesticationProgress = 0f;
                }
            }
        }

        private void OnPetProfileMessageClient(PetProfileMessage networkMessage)
        {
            if (clientAPI != null)
            {
                if (clientAPI.World.GetEntityById(networkMessage.oldEntityUID) is EntityAgent entity) clientAPI.ShowChatMessage(Lang.Get("petai:message-finished-taming", entity.GetName()));
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
        public static PetConfig Current;
        public Difficulty Difficulty = new();
        public bool PvpOn = true;
        public bool PetDamageableByOwner = false;
        public bool FalldamageOff = true;
        public bool AllowTeleport = false;
        public string[] Resurrectors = ["game:gear-temporal"];
    }
    public class Difficulty
    {
        public float petMaxHpModifier = 0;
        public float petDamageMultiplier = 1;
        public float petSpeedMultiplier = 1;
        public float tamingMultiplier = 1;
        public float obedienceMultiplier = 1;
        public float disobedienceMultiplier = 1;
        public float growingMultiplier = 1;
        public float tamingMultiplierIncreasePerGen = 0.05f;
        public float obedienceMultiplierIncreasePerGen = 0.05f;
        public float disobedienceMultiplierDecreasePerGen = 0.05f;
    }
}
