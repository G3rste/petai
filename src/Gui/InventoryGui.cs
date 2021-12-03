using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace PetAI
{
    public class PetInventoryGUI : GuiDialog
    {
        public override string ToggleKeyCombinationCode => null;

        private EntityPet pet;

        string petName;

        public PetInventoryGUI(ICoreClientAPI capi, EntityPet pet) : base(capi)
        {
            this.pet = pet;

            double pad = GuiElementItemSlotGrid.unscaledSlotPadding;
            var slotbounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, 70 + pad, pet.GearInventory.Count, 1).FixedGrow(2 * pad, 2 * pad);

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            pet.backpackInv.reloadFromSlots();

            SingleComposer = capi.Gui.CreateCompo("PetGearDialog-", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("petai:gui-gear-title"), () => TryClose())
                .BeginChildElements(bgBounds)
                    //.AddItemSlotGrid(pet.GearInventory, SendGearPacket, pet.GearInventory.Count, slotbounds, "petGear")
                    .AddItemSlotGrid(pet.backpackInv, SendBackPackPacket, pet.backpackInv.Count, slotbounds.CopyOffsetedSibling(0, 300), "petBackPackInv")
                .EndChildElements()
                .Compose();
            capi.Logger.Debug("Slots: {0}", pet.backpackInv.Count);
        }

        private void SendGearPacket(object p)
        {
            capi.Network.SendEntityPacket(pet.EntityId, p);
            pet.backpackInv.reloadFromSlots();
        }

        private void SendBackPackPacket(object p)
        {
            capi.Network.SendEntityPacket(pet.EntityId, p);
            pet.backpackInv.saveAllSlots();
        }
    }
}