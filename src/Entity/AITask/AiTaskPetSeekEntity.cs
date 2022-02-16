using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskPetSeekEntity : AiTaskSeekEntity
    {
        bool isCommandable = false;

        long lastCheck;

        public AiTaskPetSeekEntity(EntityAgent entity) : base(entity)
        {
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);

            this.isCommandable = taskConfig["isCommandable"].AsBool(false);
        }

        public override bool ShouldExecute()
        {
            var aggressionLevel = entity.GetBehavior<EntityBehaviorReceiveCommand>()?.aggressionLevel;
            if (lastCheck + 500 < entity.World.ElapsedMilliseconds)
            {
                lastCheck = entity.World.ElapsedMilliseconds;
                if (aggressionLevel == null) { aggressionLevel = EnumAggressionLevel.AGGRESSIVE; }
                if (aggressionLevel == EnumAggressionLevel.PASSIVE) { return false; }
                if (targetEntity != null && (!targetEntity.Alive || entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos) > seekingRange * seekingRange * 2)) { targetEntity = null; }
                if (targetEntity == null)
                {
                    if (aggressionLevel != EnumAggressionLevel.NEUTRAL && isCommandable)
                    {
                        var behaviorGiveCommand = entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>();
                        var ownerAttackedBy = behaviorGiveCommand?.attacker;
                        if (ownerAttackedBy != null && ownerAttackedBy.Alive)
                        {
                            targetEntity = ownerAttackedBy;
                        }

                        var ownerAttacks = behaviorGiveCommand?.victim;
                        if (ownerAttacks != null && ownerAttacks.Alive)
                        {
                            targetEntity = ownerAttacks;
                        }
                    }
                    if (attackedByEntity != null && attackedByEntity.Alive)
                    {
                        targetEntity = attackedByEntity;
                    }
                }

                if (targetEntity != null
                        && targetEntity.Alive
                        && entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos) >= MinDistanceToTarget()
                        && entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos) < seekingRange * seekingRange * 2)
                {
                    targetPos = targetEntity.ServerPos.XYZ;
                    return true;
                }
            }
            if (aggressionLevel == EnumAggressionLevel.AGGRESSIVE) { return base.ShouldExecute(); }
            return false;
        }

        public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false)
        {
            if (e == entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity && entity.HasBehavior<EntityBehaviorTameable>() && entity.GetBehavior<EntityBehaviorTameable>().obedience > 0.5f) { return false; }

            if (attackedByEntity == e) { return true; }

            return base.IsTargetableEntity(e, range, ignoreEntityCode);
        }
    }
}