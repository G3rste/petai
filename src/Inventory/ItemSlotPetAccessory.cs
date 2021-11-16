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

namespace WolfTaming
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