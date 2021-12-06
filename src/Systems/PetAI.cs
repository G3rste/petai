using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using ProtoBuf;
using System;

namespace PetAI
{
    public class PetAI : ModSystem
    {

        ICoreServerAPI serverAPI;

        ICoreClientAPI clientAPI;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterEntityBehaviorClass("raisable", typeof(EntityBehaviorRaisable));
            api.RegisterEntityBehaviorClass("tameable", typeof(EntityBehaviorTameable));
            api.RegisterEntityBehaviorClass("givecommand", typeof(EntityBehaviorGiveCommand));
            api.RegisterEntityBehaviorClass("receivecommand", typeof(EntityBehaviorReceiveCommand));
            api.RegisterEntityBehaviorClass("selfdefence", typeof(EntityBehaviorSelfDefense));
            api.RegisterEntityBehaviorClass("backpackinventory", typeof(EntityBehaviorBackPackInventory));

            AiTaskRegistry.Register<AiTaskTrick>("simplecommand");
            AiTaskRegistry.Register<AiTaskFollowMaster>("followmaster");
            AiTaskRegistry.Register<AiTaskStay>("stay");
            AiTaskRegistry.Register<AiTaskPetMeleeAttack>("petmeleeattack");
            AiTaskRegistry.Register<AiTaskPetSeekEntity>("petseekentity");
            AiTaskRegistry.Register<AiTaskSeekNest>("seeknest");

            api.RegisterEntity("EntityPet", typeof(EntityPet));

            api.RegisterItemClass("ItemPetAccessory", typeof(ItemPetAccessory));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            this.clientAPI = api;

            api.Network.RegisterChannel("petainetwork")
                .RegisterMessageType<PetCommandMessage>()
                .RegisterMessageType<PetNameMessage>().SetMessageHandler<PetNameMessage>(OnPetNameMessageClient);

            PetConfig.Current = PetConfig.getDefault();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.serverAPI = api;
            api.Network.RegisterChannel("petainetwork")
                .RegisterMessageType<PetCommandMessage>().SetMessageHandler<PetCommandMessage>(OnPetCommandMessage)
                .RegisterMessageType<PetNameMessage>().SetMessageHandler<PetNameMessage>(OnPetNameMessageServer);

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
                api.StoreModConfig(PetConfig.Current, "petconfig.json");
            }
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

        private void OnPetNameMessageServer(IServerPlayer fromPlayer, PetNameMessage networkMessage)
        {
            EntityAgent target = serverAPI.World.GetEntityById(networkMessage.targetEntityUID) as EntityAgent;
            target.GetBehavior<EntityBehaviorNameTag>()?.SetName(networkMessage.petName);
        }

        private void OnPetNameMessageClient(PetNameMessage networkMessage)
        {
            if (clientAPI != null)
            {
                EntityAgent entity = clientAPI.World.GetEntityById(networkMessage.oldEntityUID) as EntityAgent;
                if (entity != null) clientAPI.ShowChatMessage(Lang.Get("petai:message-finished-taming", entity.GetName()));
                new PetNameGUI(clientAPI, networkMessage.targetEntityUID).TryOpen();
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
    public class PetNameMessage
    {
        public string petName;
        public long targetEntityUID;
        public long oldEntityUID;
    }
    public class PetConfig
    {
        public static PetConfig Current { get; set; }

        public Difficulty difficulty { get; set; }

        public static PetConfig getDefault()
        {
            var config = new PetConfig();
            config.difficulty = new Difficulty();
            return config;
        }
    }
    public class Difficulty
    {
        public float tamingMultiplier = 1;
        public float obedienceMultiplier = 1;
        public float disobedienceMultiplier = 1;
        public float growingMultiplier = 1;
    }
}
