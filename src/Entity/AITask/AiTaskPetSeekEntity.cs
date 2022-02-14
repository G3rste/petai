using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskPetSeekEntity : AiTaskSeekEntity
    {
        bool isCommandable = false;

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
            if (targetEntity != null && !targetEntity.Alive) { targetEntity = null; }
            if (targetEntity != null
                    && targetEntity.Alive
                    && targetEntity.IsInteractable
                    && entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos) >= MinDistanceToTarget())
                {
                    targetPos = targetEntity.ServerPos.XYZ;
                    return true;
                }

            if (base.ShouldExecute()) { return true; }
            var aggressionLevel = entity.GetBehavior<EntityBehaviorReceiveCommand>()?.aggressionLevel;
            if ((aggressionLevel == EnumAggressionLevel.PROTECTIVE || aggressionLevel == EnumAggressionLevel.AGGRESSIVE) && targetEntity == null && isCommandable)
            {
                var ownerAttackedBy = entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>()?.attacker;
                if (ownerAttackedBy != null && entity.ServerPos.SquareDistanceTo(ownerAttackedBy.ServerPos) < seekingRange * seekingRange * 2)
                {
                    targetEntity = ownerAttackedBy;
                    targetPos = targetEntity.ServerPos.XYZ;
                    return true;
                }

                var ownerAttacks = entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>()?.victim;
                if (ownerAttacks != null && entity.ServerPos.SquareDistanceTo(ownerAttacks.ServerPos) < seekingRange * seekingRange * 2)
                {
                    targetEntity = ownerAttacks;
                    targetPos = targetEntity.ServerPos.XYZ;
                    return true;
                }
            }
            return false;
        }

        public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false)
        {
            var aggressionLevel = entity.GetBehavior<EntityBehaviorReceiveCommand>()?.aggressionLevel;
            if (aggressionLevel == EnumAggressionLevel.PASSIVE) { return false; }
            if (e == entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity && entity.HasBehavior<EntityBehaviorTameable>() && entity.GetBehavior<EntityBehaviorTameable>().obedience > 0.5f) { return false; }

            if (isCommandable && (aggressionLevel == EnumAggressionLevel.PROTECTIVE || aggressionLevel == EnumAggressionLevel.AGGRESSIVE))
            {
                var ownerAttackedBy = entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>()?.attacker;
                if (ownerAttackedBy == e) { return true; }

                var ownerAttacks = entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>()?.victim;
                if (ownerAttacks == e) { return true; }
            }
            if (attackedByEntity == e) { return true; }

            if (aggressionLevel == EnumAggressionLevel.PROTECTIVE || aggressionLevel == EnumAggressionLevel.NEUTRAL) { return false; }

            return base.IsTargetableEntity(e, range, ignoreEntityCode);
        }
    }
}