using Vintagestory.API.Common;

namespace PetAI
{
    public class ItemSlotPetAccessory : ItemSlotSurvival
    {
        PetAccessoryType type { get; }
        string owningEntity { get; }
        public ItemSlotPetAccessory(PetAccessoryType type, string owningEntity, InventoryBase inventory) : base(inventory)
        {
            this.type = type;
            this.owningEntity = owningEntity;
        }

        public ItemSlotPetAccessory(InventoryBase inventory) : base(inventory)
        {
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return base.CanTakeFrom(sourceSlot, priority) && isCorrectAccessory(sourceSlot);
        }
        public override bool CanHold(ItemSlot itemstackFromSourceSlot)
        {
            return base.CanHold(itemstackFromSourceSlot) && isCorrectAccessory(itemstackFromSourceSlot);
        }
        private bool isCorrectAccessory(ItemSlot sourceSlot)
        {
            var accessory = sourceSlot.Itemstack.Item as ItemPetAccessory;
            if (accessory != null)
            {
                return type == accessory.type && accessory.canBeWornBy(owningEntity);
            }
            return false;
        }
    }
}