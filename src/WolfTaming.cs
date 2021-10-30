using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using ProtoBuf;
using System;

namespace WolfTaming
{
    public class WolfTaming : ModSystem
    {

        ICoreServerAPI serverAPI;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterEntityBehaviorClass("raisable", typeof(EntityBehaviorRaisable));
            api.RegisterEntityBehaviorClass("taskaiextended", typeof(EntityBehaviorTaskAIExtension));
            api.RegisterEntityBehaviorClass("tameable", typeof(EntityBehaviorTameable));
            api.RegisterEntityBehaviorClass("givecommand", typeof(EntityBehaviorGiveCommand));
            api.RegisterEntityBehaviorClass("receivecommand", typeof(EntityBehaviorReceiveCommand));
            api.RegisterEntityBehaviorClass("selfdefence", typeof(EntityBehaviorSelfDefense));

            AiTaskRegistry.Register<AiTaskTrick>("simplecommand");
            AiTaskRegistry.Register<AiTaskFollowMaster>("followmaster");
            AiTaskRegistry.Register<AiTaskStay>("stay");
            AiTaskRegistry.Register<AiTaskMeleeAttackExtension>("meleeattackext");
            AiTaskRegistry.Register<AiTaskSeekEntityExtension>("seekentityext");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            api.Network.RegisterChannel("wolftamingnetwork")
                .RegisterMessageType<PetCommandMessage>()
                .RegisterMessageType<PetNameMessage>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.serverAPI = api;
            api.Network.RegisterChannel("wolftamingnetwork")
                .RegisterMessageType<PetCommandMessage>().SetMessageHandler<PetCommandMessage>(OnPetCommandMessage)
                .RegisterMessageType<PetNameMessage>().SetMessageHandler<PetNameMessage>(OnPetNameMessage);
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

        private void OnPetNameMessage(IServerPlayer fromPlayer, PetNameMessage networkMessage)
        {
            EntityAgent target = serverAPI.World.GetEntityById(networkMessage.targetEntityUID) as EntityAgent;
            target.GetBehavior<EntityBehaviorNameTag>()?.SetName(networkMessage.petName);
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
    }
}
