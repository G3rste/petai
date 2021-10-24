using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace WolfTaming
{
    public class EntityBehaviorReceiveCommand : EntityBehavior
    {
        public string shortTermCommand { get; private set; }

        public string longTermCommand
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
        public void setCommand(Command command)
        {
            if (command.type == CommandType.LONG_TERM)
            {
                longTermCommand = command.commandName;
            }
            if (command.type == CommandType.SHORT_TERM)
            {
                shortTermCommand = command.commandName;
            }
        }

        public override string PropertyName()
        {
            return "receivecommand";
        }
    }
}