using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace PetAI
{
    public class InventorySlotBound : InventoryBase
    {
        private List<ItemSlot> slots = new List<ItemSlot>();
        private ItemSlot[] backPackSlots;

        public InventorySlotBound(string inventoryID, ICoreAPI api, ItemSlot[] backPackSlots) : base(inventoryID, api)
        {
            this.backPackSlots = backPackSlots;
        }

        public override ItemSlot this[int slotId] { get => slots[slotId]; set => slots[slotId] = value; }

        public override int Count => slots.Count;

        public void reloadFromSlots()
        {
            slots.Clear();
            foreach (var slot in backPackSlots)
            {
                slots.AddRange(getSlotsFromItemStack(slot.Itemstack));
            }
        }

        public void saveAllSlots()
        {
            int handledSlots = 0;
            foreach (var slot in backPackSlots)
            {
                ItemPetAccessory item = slot.Itemstack?.Item as ItemPetAccessory;
                if (item != null)
                {
                    saveSlotsInItemStack(slot.Itemstack, slots.GetRange(handledSlots, item.backpackSlots).ToArray());
                    handledSlots += item.backpackSlots;
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            throw new System.NotImplementedException();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            throw new System.NotImplementedException();
        }

        private ItemSlot[] getSlotsFromItemStack(ItemStack stack)
        {
            ItemPetAccessory item = stack?.Item as ItemPetAccessory;
            if (item == null) return new ItemSlotPetBackPack[0];

            var potentialItemSlots = new ItemSlotPetBackPack[item.backpackSlots];
            for (int i = 0; i < potentialItemSlots.Length; i++)
            {
                potentialItemSlots[i] = new ItemSlotPetBackPack(this);
            }

            if (stack.Attributes.HasAttribute("petBackPackInventory"))
            {
                return SlotsFromTreeAttributes(stack.Attributes.GetTreeAttribute("petBackPackInventory"), potentialItemSlots);
            }
            else
            {
                return potentialItemSlots;
            }
        }

        private void saveSlotsInItemStack(ItemStack stack, ItemSlot[] slots)
        {
            var tree = stack.Attributes.GetOrAddTreeAttribute("petBackPackInventory");
            SlotsToTreeAttributes(slots, tree);
        }
    }
}