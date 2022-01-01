using System;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace PetAI
{
    public class ItemPetWhistle : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAnimation;
            byEntity.AnimManager?.StartAnimation("eat");
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            // For some reason setting the handling to prevent default does not seem to work
            byEntity.AnimManager?.StopAnimation("PlaceBlock");
            return secondsUsed < 3;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            byEntity.AnimManager?.StopAnimation("eat");
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            byEntity.AnimManager?.StopAnimation("eat");
            return true;
        }
    }
}