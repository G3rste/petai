using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PetAI
{
    public class EntityBehaviorPetInventory : EntityBehaviorRideableAccessories
    {
        public EntityBehaviorPetInventory(Entity entity) : base(entity)
        {
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (byEntity is EntityPlayer player && player.PlayerUID == entity.GetBehavior<EntityBehaviorTameable>()?.ownerId)
            {
                base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
            }
            handled = EnumHandling.PassThrough;
        }

        public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
        {
            if (player.PlayerUID == entity.GetBehavior<EntityBehaviorTameable>()?.ownerId)
            {
                return base.GetInteractionHelp(world, es, player, ref handled);
            }
            return new WorldInteraction[0];
        }
        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            if (damageSource.SourceEntity != null && damageSource.Type != EnumDamageType.Heal)
            {
                foreach (var slot in Inventory)
                {
                    if (!slot.Empty)
                    {
                        damage *= 1 - slot.Itemstack.Item.Attributes["damageReduction"].AsFloat(0);
                    }
                }
            }
        }
    }
}