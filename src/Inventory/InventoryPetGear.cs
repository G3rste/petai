using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace PetAI
{
    public class InventoryPetGear : InventoryBase
    {

        ItemSlot[] slots;
        string owningEntity;


        public override ItemSlot this[int slotId] { get => slots[slotId]; set => slots[slotId] = value; }

        public override int Count => slots.Length;
        public InventoryPetGear(string owningEntity, string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            this.owningEntity = owningEntity;
            var accessoryTypes = Enum.GetNames(typeof(PetAccessoryType));
            slots = new List<string>(accessoryTypes)
                .ConvertAll<ItemSlotPetAccessory>(
                    accessoryType => new ItemSlotPetAccessory(
                        (PetAccessoryType)Enum.Parse(typeof(PetAccessoryType), accessoryType), owningEntity, this))
                .ToArray();
        }
        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots = SlotsFromTreeAttributes(tree, slots);
            owningEntity = tree.GetString("owningEntity");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
            tree.SetString("owningEntity", owningEntity);
        }
        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            foreach (var slot in slots)
            {
                slot.Itemstack?.ResolveBlockOrItem(api.World);
            }
        }

        public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
        {
            var accessory = sourceSlot?.Itemstack?.Item as ItemPetAccessory;
            if (accessory != null)
            {
                var weightedSlot = new WeightedSlot();
                weightedSlot.weight = 1;
                weightedSlot.slot = slots[(int)accessory.type];
                return weightedSlot;
            }
            return base.GetBestSuitedSlot(sourceSlot, op, skipSlots);
        }
    }
}