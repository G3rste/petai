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

        long lastSearch;

        private EntityBehaviorGiveCommand _behaviorGiveCommand;
        private long lastOwnerLookup;
        private EntityBehaviorGiveCommand behaviorGiveCommand
        {
            get
            {
                if (_behaviorGiveCommand == null && lastOwnerLookup + 5000 < entity.World.ElapsedMilliseconds)
                {
                    lastOwnerLookup = entity.World.ElapsedMilliseconds;
                    _behaviorGiveCommand = entity.GetBehavior<EntityBehaviorTameable>()?.cachedOwner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>();
                }
                return _behaviorGiveCommand;
            }
        }

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
            var elapsedMs = entity.World.ElapsedMilliseconds;
            if (lastCheck + 500 < elapsedMs)
            {
                lastCheck = elapsedMs;
                if (aggressionLevel == null) { aggressionLevel = EnumAggressionLevel.AGGRESSIVE; }
                if (aggressionLevel == EnumAggressionLevel.PASSIVE) { return false; }
                if (!IsTargetableEntity(targetEntity, seekingRange, true)) { targetEntity = null; }
                if (targetEntity == null)
                {
                    if (aggressionLevel != EnumAggressionLevel.NEUTRAL && isCommandable)
                    {
                        var ownerAttackedBy = behaviorGiveCommand?.attacker;
                        if (IsTargetableEntity(ownerAttackedBy, seekingRange * 2, true))
                        {
                            targetEntity = ownerAttackedBy;
                        }

                        var ownerAttacks = behaviorGiveCommand?.victim;
                        if (IsTargetableEntity(ownerAttacks, seekingRange * 2, true))
                        {
                            targetEntity = ownerAttacks;
                        }
                    }
                    if (IsTargetableEntity(attackedByEntity, seekingRange * 2, true))
                    {
                        targetEntity = attackedByEntity;
                    }
                }

                if (IsTargetableEntity(targetEntity, seekingRange * 2, true))
                {
                    targetPos = targetEntity.ServerPos.XYZ;
                    return true;
                }
            }
            if (aggressionLevel == EnumAggressionLevel.AGGRESSIVE && lastSearch + 5000 < elapsedMs)
            {
                lastSearch = elapsedMs;
                targetEntity = partitionUtil.GetNearestEntity(entity.ServerPos.XYZ, seekingRange, e => IsTargetableEntity(e, seekingRange));

                if (IsTargetableEntity(targetEntity, seekingRange, true))
                {
                    targetPos = targetEntity.ServerPos.XYZ;
                    return true;
                }
            }
            return false;
        }

        public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false)
        {
            if (e == null) { return false; }
            if ((e as EntityPlayer)?.PlayerUID == entity.GetBehavior<EntityBehaviorTameable>()?.ownerId && entity.HasBehavior<EntityBehaviorTameable>() && entity.GetBehavior<EntityBehaviorTameable>().obedience > 0.5f) { return false; }

            return base.IsTargetableEntity(e, range, ignoreEntityCode);
        }
    }
}