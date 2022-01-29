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

            if (isCommandable && (aggressionLevel == EnumAggressionLevel.PROTECTIVE || aggressionLevel == EnumAggressionLevel.AGGRESSIVE))
            {
                var ownerAttackedBy = entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>()?.attacker;
                if (ownerAttackedBy == e) { return true; }

                var ownerAttacks = entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>()?.victim;
                if (ownerAttacks == e) { return true; }
            }
            if (attackedByEntity == e
                && attackedByEntity.Alive
                && attackedByEntity.IsInteractable
                && attackedByEntity != entity.GetBehavior<EntityBehaviorTameable>()?.owner)
            {
                return true;
            }

            if (aggressionLevel == EnumAggressionLevel.PROTECTIVE || aggressionLevel == EnumAggressionLevel.NEUTRAL) { return false; }

            return base.IsTargetableEntity(e, range, ignoreEntityCode);
        }
    }
}