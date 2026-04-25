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

        public AiTaskPetSeekEntity(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
        {
            isCommandable = taskConfig["isCommandable"].AsBool(false);
            moveSpeed *= PetConfig.Current.Difficulty.petSpeedMultiplier;
            animMeta.AnimationSpeed *= PetConfig.Current.Difficulty.petSpeedMultiplier;
        }

        public override bool ShouldExecute()
        {
            var aggressionLevel = entity.GetBehavior<EntityBehaviorReceiveCommand>()?.aggressionLevel;
            var elapsedMs = entity.World.ElapsedMilliseconds;
            if (lastCheck + 500 < elapsedMs)
            {
                NowSeekRange = getSeekRange();
                lastCheck = elapsedMs;
                if (aggressionLevel == null) { aggressionLevel = EnumAggressionLevel.AGGRESSIVE; }
                if (aggressionLevel == EnumAggressionLevel.PASSIVE) { return false; }
                if (!IsTargetableEntity(targetEntity, NowSeekRange)) { targetEntity = null; }
                if (targetEntity == null)
                {
                    if (aggressionLevel != EnumAggressionLevel.NEUTRAL && isCommandable)
                    {
                        var ownerAttackedBy = behaviorGiveCommand?.attacker;
                        if (IsTargetableEntity(ownerAttackedBy, NowSeekRange))
                        {
                            targetEntity = ownerAttackedBy;
                        }

                        var ownerAttacks = behaviorGiveCommand?.victim;
                        if (IsTargetableEntity(ownerAttacks, NowSeekRange))
                        {
                            targetEntity = ownerAttacks;
                        }
                    }
                    if (IsTargetableEntity(attackedByEntity, NowSeekRange))
                    {
                        targetEntity = attackedByEntity;
                    }
                }

                if (IsTargetableEntity(targetEntity, NowSeekRange))
                {
                    targetPos = targetEntity.Pos.XYZ;
                    return true;
                }
            }
            if (aggressionLevel == EnumAggressionLevel.AGGRESSIVE && lastSearch + 5000 < elapsedMs)
            {
                lastSearch = elapsedMs;
                targetEntity = partitionUtil.GetNearestInteractableEntity(entity.Pos.XYZ, NowSeekRange, e => IsTargetableEntity(e, NowSeekRange));

                if (IsTargetableEntity(targetEntity, NowSeekRange))
                {
                    targetPos = targetEntity.Pos.XYZ;
                    return true;
                }
            }
            return false;
        }

        public override bool IsTargetableEntity(Entity e, float range)
        {
            if (e == null) { return false; }
            var tameable = entity.GetBehavior<EntityBehaviorTameable>();
            if (e is EntityPlayer player)
            {
                if (player.PlayerUID == tameable?.ownerId && tameable != null && tameable.obedience > 0.5f)
                {
                    return false;
                }
                if (!PetConfig.Current.PvpOn && tameable?.domesticationLevel != DomesticationLevel.WILD && player.PlayerUID != tameable?.ownerId)
                {
                    return false;
                }
            }
            if (e.Pos.SquareDistanceTo(entity.Pos) > range * range) { return false; }


            return base.IsTargetableEntity(e, range);
        }
    }
}
