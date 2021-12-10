using Vintagestory.API.Common;

namespace PetAI
{
    public class ItemSlotPetBackPack : ItemSlotSurvival
    {
        public ItemSlotPetBackPack(InventoryBase inventory) : base(inventory)
        {
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return base.CanTakeFrom(sourceSlot, priority) && isNoPetBackPack(sourceSlot);
        }
        public override bool CanHold(ItemSlot itemstackFromSourceSlot)
        {
            return base.CanHold(itemstackFromSourceSlot) && isNoPetBackPack(itemstackFromSourceSlot);
        }

        private bool isNoPetBackPack(ItemSlot slot)
        {
            var item = slot.Itemstack?.Item as ItemPetAccessory;
            return item == null || item.backpackSlots == 0;
        }
    }
}