using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskPetSeekEntity : AiTaskSeekEntity
    {
        readonly bool isCommandable = false;

        long lastCheck;

        long lastSearch;

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

        public AiTaskPetSeekEntity(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
        {
            isCommandable = taskConfig["isCommandable"].AsBool(false);
            moveSpeed *= PetConfig.Current.Difficulty.petSpeedMultiplier;
            animMeta.AnimationSpeed *= PetConfig.Current.Difficulty.petSpeedMultiplier;
        }

        public override bool ShouldExecute()
        {
            var aggressionLevel = entity.GetBehavior<EntityBehaviorReceiveCommand>()?.AggressionLevel;
            var elapsedMs = entity.World.ElapsedMilliseconds;
            if (lastCheck + 500 < elapsedMs)
            {
                NowSeekRange = getSeekRange();
                lastCheck = elapsedMs;
                aggressionLevel ??= EnumAggressionLevel.AGGRESSIVE;
                if (aggressionLevel == EnumAggressionLevel.PASSIVE) { return false; }
                if (!CanSense(targetEntity, NowSeekRange)) { targetEntity = null; }
                if (targetEntity == null)
                {
                    if (aggressionLevel != EnumAggressionLevel.NEUTRAL && isCommandable)
                    {
                        var ownerAttackedBy = BehaviorGiveCommand?.Attacker;
                        if (ownerAttackedBy?.Alive == true && CanSense(ownerAttackedBy, NowSeekRange))
                        {
                            targetEntity = ownerAttackedBy;
                        }

                        var ownerAttacks = BehaviorGiveCommand?.Victim;
                        if (ownerAttacks?.Alive == true && CanSense(ownerAttacks, NowSeekRange))
                        {
                            targetEntity = ownerAttacks;
                        }
                    }
                    if (attackedByEntity?.Alive == true && CanSense(attackedByEntity, NowSeekRange))
                    {
                        targetEntity = attackedByEntity;
                    }
                }

                if (CanSense(targetEntity, NowSeekRange))
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
                if (player.PlayerUID == tameable?.OwnerId && tameable != null && tameable.Obedience > 0.5f)
                {
                    return false;
                }
                if (!PetConfig.Current.PvpOn && tameable?.DomesticationLevel != DomesticationLevel.WILD && player.PlayerUID != tameable?.OwnerId)
                {
                    return false;
                }
            }
            if (e.Pos.SquareDistanceTo(entity.Pos) > range * range) { return false; }


            return base.IsTargetableEntity(e, range);
        }
    }
}
