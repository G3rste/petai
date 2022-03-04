using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskPetMeleeAttack : AiTaskMeleeAttack
    {
        bool isCommandable = false;

        public AiTaskPetMeleeAttack(EntityAgent entity) : base(entity)
        {
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);

            this.isCommandable = taskConfig["isCommandable"].AsBool(false);
        }

        public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false)
        {
            var aggressionLevel = entity.GetBehavior<EntityBehaviorReceiveCommand>()?.aggressionLevel;
            if (aggressionLevel == EnumAggressionLevel.PASSIVE) { return false; }
            var tameable = entity.GetBehavior<EntityBehaviorTameable>();
            if (e == tameable?.owner?.Entity && tameable.obedience > 0.5f) { return false; }

            if (isCommandable && (aggressionLevel == EnumAggressionLevel.PROTECTIVE || aggressionLevel == EnumAggressionLevel.AGGRESSIVE))
            {
                var commandBehavior = tameable?.owner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>();
                if (commandBehavior?.attacker == e || commandBehavior?.victim == e)
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