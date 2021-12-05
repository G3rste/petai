using Vintagestory.API.Client;
using Vintagestory.API.Config;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

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