using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace WolfTaming
{
    public class EntityBehaviorGiveCommand : EntityBehavior
    {
        public Command activeCommand
        {
            get
            {
                ITreeAttribute command = entity.WatchedAttributes.GetTreeAttribute("activeCommand");
                String commandName = command?.GetString("commandName");
                CommandType type;

                if (Enum.TryParse<CommandType>(command?.GetString("type"), out type) && commandName != null)
                {
                    return new Command(type, commandName);
                }
                return null;
            }
            set
            {
                ITreeAttribute command = new TreeAttribute();
                command.SetString("commandName", value.commandName);
                command.SetString("type", value.type.ToString());
                entity.WatchedAttributes.SetAttribute("activeCommand", command);
            }
        }
        public EntityBehaviorGiveCommand(Entity entity) : base(entity) { }

        public override string PropertyName()
        {
            return "givecommand";
        }
    }

    public class Command
    {
        public CommandType type { get; }
        public string commandName { get; }

        public Command(CommandType type, string commandName)
        {
            this.commandName = commandName;
            this.type = type;
        }
    }
    public enum CommandType
    {
        SHORT_TERM, LONG_TERM
    }
}