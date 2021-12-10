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
            slots = new ItemSlotPetAccessory[5] {
                new ItemSlotPetAccessory(PetAccessoryType.NECK, owningEntity, this),
                new ItemSlotPetAccessory(PetAccessoryType.HEAD, owningEntity, this),
                new ItemSlotPetAccessory(PetAccessoryType.BACK, owningEntity, this),
                new ItemSlotPetAccessory(PetAccessoryType.PAWS, owningEntity, this),
                new ItemSlotPetAccessory(PetAccessoryType.TAIL, owningEntity, this)
            };
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

        public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, List<ItemSlot> skipSlots = null)
        {
            var accessory = sourceSlot?.Itemstack?.Item as ItemPetAccessory;
            if (accessory != null)
            {
                var weightedSlot = new WeightedSlot();
                weightedSlot.weight = 1;
                switch (accessory.type)
                {
                    case PetAccessoryType.NECK: weightedSlot.slot = slots[0]; break;
                    case PetAccessoryType.HEAD: weightedSlot.slot = slots[1]; break;
                    case PetAccessoryType.BACK: weightedSlot.slot = slots[2]; break;
                    case PetAccessoryType.PAWS: weightedSlot.slot = slots[3]; break;
                    case PetAccessoryType.TAIL: weightedSlot.slot = slots[4]; break;
                    default: weightedSlot.slot = slots[0]; break;
                }
                return weightedSlot;
            }
            return base.GetBestSuitedSlot(sourceSlot, skipSlots);
        }
    }
}