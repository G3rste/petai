using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace PetAI
{
    public class EntityBehaviorGiveCommand : EntityBehavior
    {
        private Entity _victim;
        private Entity _attacker;
        public Entity victim
        {
            get
            {
                if (_victim == null || !_victim.Alive || !_victim.IsInteractable || _victim.EntityId == entity.EntityId)
                {
                    _victim = null;
                    return null;
                }
                return _victim;
            }
            set => _victim = value;
        }
        public Entity attacker
        {
            get
            {
                if (_attacker == null || !_attacker.Alive || !_attacker.IsInteractable || _attacker.EntityId == entity.EntityId)
                {
                    _attacker = null;
                    return null;
                }
                return _attacker;
            }
            set => _attacker = value;
        }
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

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            base.OnEntityReceiveDamage(damageSource, ref damage);
            attacker = damageSource.GetCauseEntity();
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

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is Command)) return false;
            if (obj == this) return true;
            Command command = obj as Command;
            return this.commandName == command.commandName && this.type == command.type;
        }

        public override int GetHashCode()
        {
            return commandName.GetHashCode();
        }
    }
    public enum EnumCommandType
    {
        SIMPLE, COMPLEX, AGGRESSIONLEVEL, ATTACKORDER
    }
    public enum EnumAggressionLevel
    {
        NEUTRAL, PROTECTIVE, AGGRESSIVE, PASSIVE
    }
}