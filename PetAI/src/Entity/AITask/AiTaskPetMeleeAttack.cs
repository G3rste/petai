using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskPetMeleeAttack : AiTaskMeleeAttack
    {
        readonly bool isCommandable = false;

        private EntityBehaviorGiveCommand _behaviorGiveCommand;
        private long lastOwnerLookup;
        private EntityBehaviorGiveCommand BehaviorGiveCommand
        {
            get
            {
                if (_behaviorGiveCommand == null && lastOwnerLookup + 5000 < entity.World.ElapsedMilliseconds)
                {
                    lastOwnerLookup = entity.World.ElapsedMilliseconds;
                    _behaviorGiveCommand = entity.GetBehavior<EntityBehaviorTameable>()?.CachedOwner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>();
                }
                return _behaviorGiveCommand;
            }
        }

        public AiTaskPetMeleeAttack(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
        {
            isCommandable = taskConfig["isCommandable"].AsBool(false);
            damage *= PetConfig.Current.Difficulty.petDamageMultiplier;
        }

        public override bool IsTargetableEntity(Entity e, float range)
        {
            if (e==null || !e.Alive)
            {
                return false;
            }
            var aggressionLevel = entity.GetBehavior<EntityBehaviorReceiveCommand>()?.AggressionLevel;
            if (aggressionLevel == EnumAggressionLevel.PASSIVE) { return false; }
            var tameable = entity.GetBehavior<EntityBehaviorTameable>();
            if (e is EntityPlayer player)
            {
                if (tameable != null && player.PlayerUID == tameable.OwnerId && tameable.Obedience > 0.5f)
                {
                    return false;
                }
                if (!PetConfig.Current.PvpOn && tameable?.DomesticationLevel != DomesticationLevel.WILD && player.PlayerUID != tameable?.OwnerId)
                {
                    return false;
                }
            }

            if (isCommandable && (aggressionLevel == EnumAggressionLevel.PROTECTIVE || aggressionLevel == EnumAggressionLevel.AGGRESSIVE))
            {
                if (BehaviorGiveCommand?.Attacker == e || BehaviorGiveCommand?.Victim == e)
                {
                    return CanSense(e, range);
                }
            }
            if (attackedByEntity == e)
            {
                return CanSense(e, range);
            }

            if (aggressionLevel == EnumAggressionLevel.PROTECTIVE || aggressionLevel == EnumAggressionLevel.NEUTRAL) { return false; }

            return base.IsTargetableEntity(e, range);
        }

        public override bool ShouldExecute()
        {
            long elapsedMs = entity.World.ElapsedMilliseconds;
            if (elapsedMs - lastCheckOrAttackMs < attackDurationMs || cooldownUntilMs > elapsedMs)
            {
                return false;
            }

            if (!PreconditionsSatisfied()) return false;

            Vec3d pos = entity.Pos.XYZ.Add(0, entity.SelectionBox.Y2 / 2, 0).Ahead(entity.SelectionBox.XSize / 2, 0, entity.Pos.Yaw);

            int generation = GetOwnGeneration();
            bool fullyTamed = generation >= tamingGenerations;

            float fearReductionFactor = Math.Max(0f, (tamingGenerations - generation) / tamingGenerations);
            if (WhenInEmotionStates != null) fearReductionFactor = 1;

            if (fearReductionFactor <= 0) return false;

            if (entity.World.ElapsedMilliseconds - attackedByEntityMs > 30000)
            {
                attackedByEntity = null;
            }
            if (ShouldRetaliateForRange(15) && hasDirectContact(attackedByEntity, minDist, minVerDist))
            {
                targetEntity = attackedByEntity;
            }
            else
            {
                targetEntity = entity.World.GetNearestEntity(pos, attackRange * fearReductionFactor, attackRange * fearReductionFactor, (e) =>
                {
                    return IsTargetableEntity(e, 15) && hasDirectContact(e, minDist, minVerDist);
                });
            }

            lastCheckOrAttackMs = entity.World.ElapsedMilliseconds;
            damageInflicted = false;

            return targetEntity != null;
        }
    }
}
