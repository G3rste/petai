using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PetAI
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

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            if (entity.Api.Side == EnumAppSide.Server)
            {
                entity.World.RegisterGameTickListener(checkPetRespawn, 120000);
            }
        }

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

        public void savePet(Entity pet)
        {
            entity.Attributes.GetOrAddTreeAttribute("playerpets")[String.Format("playerpet-{0}", pet.EntityId)] = PetUtil.EntityToTree(pet);
        }

        private void checkPetRespawn(float dt)
        {
            foreach (var attr in entity.Attributes.GetOrAddTreeAttribute("playerpets"))
            {
                Entity pet = PetUtil.EntityFromTree(attr.Value as ITreeAttribute, entity.World);
                if (pet != null)
                {
                    pet.ServerPos.SetPos(entity.ServerPos);
                    pet.Pos.SetPos(entity.Pos);
                    entity.World.SpawnEntity(pet);
                    
                    if (pet.HasBehavior<EntityBehaviorHealth>())
                    {
                        pet.GetBehavior<EntityBehaviorHealth>().Health = pet.GetBehavior<EntityBehaviorHealth>().MaxHealth;
                    }

                    entity.Attributes.GetOrAddTreeAttribute("playerpets").RemoveAttribute(attr.Key);

                    SimpleParticleProperties smoke = new SimpleParticleProperties(
                            100, 150,
                            ColorUtil.ToRgba(80, 100, 100, 100),
                            new Vec3d(),
                            new Vec3d(2, 1, 2),
                            new Vec3f(-0.25f, 0f, -0.25f),
                            new Vec3f(0.25f, 0f, 0.25f),
                            0.51f,
                            -0.075f,
                            0.5f,
                            3f,
                            EnumParticleModel.Quad
                        );

                    smoke.MinPos = entity.ServerPos.XYZ.AddCopy(-1.5, -0.5, -1.5);
                    entity.World.SpawnParticles(smoke);
                }
            }
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
        SIMPLE, COMPLEX, AGGRESSIONLEVEL
    }
    public enum EnumAggressionLevel
    {
        NEUTRAL, PROTECTIVE, AGGRESSIVE, PASSIVE
    }
}