using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskPetMeleeAttack : AiTaskMeleeAttack
    {
        bool isCommandable = false;

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

        public AiTaskPetMeleeAttack(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
        {
            this.isCommandable = taskConfig["isCommandable"].AsBool(false);
        }

        public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false)
        {
            var aggressionLevel = entity.GetBehavior<EntityBehaviorReceiveCommand>()?.aggressionLevel;
            if (aggressionLevel == EnumAggressionLevel.PASSIVE) { return false; }
            var tameable = entity.GetBehavior<EntityBehaviorTameable>();
            if (e is EntityPlayer player)
            {
                if (tameable != null && player.PlayerUID == tameable.ownerId && tameable.obedience > 0.5f)
                {
                    return false;
                }
                if (!PetConfig.Current.PvpOn && tameable?.domesticationLevel != DomesticationLevel.WILD && player.PlayerUID != tameable?.ownerId)
                {
                    return false;
                }
            }

            if (isCommandable && (aggressionLevel == EnumAggressionLevel.PROTECTIVE || aggressionLevel == EnumAggressionLevel.AGGRESSIVE))
            {
                if (behaviorGiveCommand?.attacker == e || behaviorGiveCommand?.victim == e)
                {
                    return base.IsTargetableEntity(e, range, true);
                }
            }
            if (attackedByEntity == e)
            {
                return base.IsTargetableEntity(e, range, true);
            }

            if (aggressionLevel == EnumAggressionLevel.PROTECTIVE || aggressionLevel == EnumAggressionLevel.NEUTRAL) { return false; }

            return base.IsTargetableEntity(e, range, ignoreEntityCode);
        }
    }
}