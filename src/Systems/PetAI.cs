using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using ProtoBuf;
using System;
using System.Collections.Generic;
using HarmonyLib;

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
            WildcraftPoulticePatch.Patch(harmony);
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
                if (PetConfig.Current.Resurrectors == null)
                    PetConfig.Current.Resurrectors = PetConfig.getDefault().Resurrectors;

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
        public Difficulty Difficulty { get; set; }
        public List<PetResurrector> Resurrectors { get; set; }
        public bool PvpOff { get; set; }
        public bool FalldamageOff { get; set; }
        public bool AllowTeleport { get; set; }
        public static PetConfig getDefault()
        {
            var config = new PetConfig();

            var difficulty = new Difficulty
            {
                tamingMultiplier = 1,
                obedienceMultiplier = 1,
                disobedienceMultiplier = 1,
                growingMultiplier = 1,
                tamingMultiplierIncreasePerGen = 0.05f,
                obedienceMultiplierIncreasePerGen = 0.05f,
                disobedienceMultiplierDecreasePerGen = 0.05f
            };
            config.Difficulty = difficulty;

            config.PvpOff = false;
            config.FalldamageOff = false;
            config.AllowTeleport = false;

            config.Resurrectors = new List<PetResurrector>(new PetResurrector[] {
                    // Base Game
                    new PetResurrector(){name = "bandage-clean", domain ="game", healingValue = 4},
                    new PetResurrector(){name = "bandage-alcoholed", domain ="game", healingValue = 8},
                    new PetResurrector(){name = "poultice-reed-horsetail", domain ="game", healingValue = 1},
                    new PetResurrector(){name = "poultice-reed-honey-sulfur", domain ="game", healingValue = 2},
                    new PetResurrector(){name = "poultice-linen-horsetail", domain ="game", healingValue = 2},
                    new PetResurrector(){name = "poultice-linen-honey-sulfur", domain ="game", healingValue = 3},
                    // Wildcraft
                    new PetResurrector(){name = "poultice-linen-honey-sulfur-lavender", domain ="wildcraft", healingValue = 8.0},
                    new PetResurrector(){name = "poultice-reed-honey-sulfur-lavender", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-linen-horsetail-lavender", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-horsetail-lavender", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-stjohnswort-lavender", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-stjohnswort-lavender", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-poisonoak-lavender", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-poisonoak-lavender", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-inula-lavender", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-inula-lavender", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-celandine-lavender", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-celandine-lavender", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-jewelweed-lavender", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-jewelweed-lavender", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-honey-sulfur-thyme", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-honey-sulfur-thyme", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-horsetail-thyme", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-horsetail-thyme", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-stjohnswort-thyme", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-stjohnswort-thyme", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-poisonoak-thyme", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-poisonoak-thyme", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-inula-thyme", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-inula-thyme", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-celandine-thyme", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-celandine-thyme", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-jewelweed-thyme", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-jewelweed-thyme", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-honey-sulfur-marshmallow", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-honey-sulfur-marshmallow", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-horsetail-marshmallow", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-horsetail-marshmallow", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-stjohnswort-marshmallow", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-stjohnswort-marshmallow", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-poisonoak-marshmallow", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-poisonoak-marshmallow", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-inula-marshmallow", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-inula-marshmallow", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-celandine-marshmallow", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-celandine-marshmallow", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-jewelweed-marshmallow", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-jewelweed-marshmallow", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-honey-sulfur-holly", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-honey-sulfur-holly", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-horsetail-holly", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-horsetail-holly", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-stjohnswort-holly", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-stjohnswort-holly", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-poisonoak-holly", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-poisonoak-holly", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-inula-holly", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-inula-holly", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-celandine-holly", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-celandine-holly", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-jewelweed-holly", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-jewelweed-holly", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-honey-sulfur-marigold", domain ="wildcraft", healingValue = 8.0},
                    new PetResurrector(){name = "poultice-reed-honey-sulfur-marigold", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-linen-horsetail-marigold", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-horsetail-marigold", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-stjohnswort-marigold", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-stjohnswort-marigold", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-poisonoak-marigold", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-poisonoak-marigold", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-inula-marigold", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-inula-marigold", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-celandine-marigold", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-celandine-marigold", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-jewelweed-marigold", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-jewelweed-marigold", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-honey-sulfur-yarrow", domain ="wildcraft", healingValue = 8.0},
                    new PetResurrector(){name = "poultice-reed-honey-sulfur-yarrow", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-linen-horsetail-yarrow", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-horsetail-yarrow", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-stjohnswort-yarrow", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-stjohnswort-yarrow", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-poisonoak-yarrow", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-poisonoak-yarrow", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-inula-yarrow", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-inula-yarrow", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-celandine-yarrow", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-celandine-yarrow", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-jewelweed-yarrow", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-jewelweed-yarrow", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-honey-sulfur-sage", domain ="wildcraft", healingValue = 7.0},
                    new PetResurrector(){name = "poultice-reed-honey-sulfur-sage", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-linen-horsetail-sage", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-horsetail-sage", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-stjohnswort-sage", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-stjohnswort-sage", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-poisonoak-sage", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "poultice-reed-poisonoak-sage", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "poultice-linen-inula-sage", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-inula-sage", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-celandine-sage", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-celandine-sage", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "poultice-linen-jewelweed-sage", domain ="wildcraft", healingValue = 6.0},
                    new PetResurrector(){name = "poultice-reed-jewelweed-sage", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "simplepoultice-linen-stjohnswort", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "simplepoultice-reed-stjohnswort", domain ="wildcraft", healingValue = 2.0},
                    new PetResurrector(){name = "simplepoultice-linen-poisonoak", domain ="wildcraft", healingValue = 4.0},
                    new PetResurrector(){name = "simplepoultice-reed-poisonoak", domain ="wildcraft", healingValue = 2.0},
                    new PetResurrector(){name = "simplepoultice-linen-inula", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "simplepoultice-reed-inula", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "simplepoultice-linen-celandine", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "simplepoultice-reed-celandine", domain ="wildcraft", healingValue = 3.0},
                    new PetResurrector(){name = "simplepoultice-linen-jewelweed", domain ="wildcraft", healingValue = 5.0},
                    new PetResurrector(){name = "simplepoultice-reed-jewelweed", domain ="wildcraft", healingValue = 3.0}
                });
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
