using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace WolfTaming
{
    public class EntityBehaviorReceiveCommand : EntityBehavior
    {
        public string simpleCommand { get; private set; }

        public string complexCommand
        {
            get { return entity.WatchedAttributes.GetString("activeCommand"); }
            private set
            {
                entity.WatchedAttributes.SetString("activeCommand", value);
            }
        }
        public EntityBehaviorReceiveCommand(Entity entity) : base(entity)
        {
        }
        public void setCommand(Command command, EntityPlayer byPlayer)
        {
            if (byPlayer == null
                || entity.GetBehavior<EntityBehaviorTameable>()?.owner == null
                || entity.GetBehavior<EntityBehaviorTameable>().owner.PlayerUID == byPlayer.PlayerUID)
            {
                if (command.type == CommandType.COMPLEX)
                {
                    complexCommand = command.commandName;
                }
                if (command.type == CommandType.SIMPLE)
                {
                    simpleCommand = command.commandName;
                }

                ITreeAttribute location = new TreeAttribute();
                location.SetDouble("x", entity.ServerPos.X);
                location.SetDouble("y", entity.ServerPos.Y);
                location.SetDouble("z", entity.ServerPos.Z);

                entity.WatchedAttributes.SetAttribute("staylocation", location);
            }
        }

        public override string PropertyName()
        {
            return "receivecommand";
        }
    }
}