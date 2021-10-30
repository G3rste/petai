using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace WolfTaming
{
    public class EntityBehaviorGiveCommand : EntityBehavior
    {
        public Entity victim { get; set; }
        public Entity attacker { get; set; }
        public Command activeCommand
        {
            get
            {
                ITreeAttribute command = entity.WatchedAttributes.GetTreeAttribute("activeCommand");
                String commandName = command?.GetString("commandName");
                EnumCommandType type;

                if (Enum.TryParse<EnumCommandType>(command?.GetString("type"), out type) && commandName != null)
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

        public override void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
        {
            base.DidAttack(source, targetEntity, ref handled);
            victim = targetEntity;
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, float damage)
        {
            base.OnEntityReceiveDamage(damageSource, damage);
            attacker = damageSource.SourceEntity;
        }

        public override string PropertyName()
        {
            return "givecommand";
        }
    }

    public class Command
    {
        public EnumCommandType type { get; }
        public string commandName { get; }

        public Command(EnumCommandType type, string commandName)
        {
            this.commandName = commandName;
            this.type = type;
        }
    }
    public enum EnumCommandType
    {
        SIMPLE, COMPLEX, AGGRESSIONLEVEL
    }
    public enum EnumAggressionLevel
    {
        SELFDEFENSE, PROTECTMASTER, ATTACKTARGET
    }
}