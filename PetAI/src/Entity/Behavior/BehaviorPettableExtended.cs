using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PetAI
{
    public class EntityBehaviorPettableExtended : EntityBehaviorPettable
    {
        private long lastObedienceIncrease = 0;
        public EntityBehaviorPettableExtended(Entity entity) : base(entity)
        {
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
            if (lastObedienceIncrease + 1000 < byEntity.World.ElapsedMilliseconds && entity.HasBehavior<EntityBehaviorTameable>())
            {
                lastObedienceIncrease = byEntity.World.ElapsedMilliseconds;
                entity.GetBehavior<EntityBehaviorTameable>().obedience += 0.004f;
            }
        }

        public override string PropertyName()
        {
            return "pettableextended";
        }
    }
}