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
            var slotbounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, 70 + pad,pet.GearInventory.Count , 1).FixedGrow(2 * pad, 2 * pad);

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            SingleComposer = capi.Gui.CreateCompo("PetGearDialog-", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("petai:gui-gear-title"), () => TryClose())
                .BeginChildElements(bgBounds)
                    .AddItemSlotGrid(pet.GearInventory, DoSendPacket, pet.GearInventory.Count, slotbounds, "petGear")
                .EndChildElements()
                .Compose();
        }

        private void DoSendPacket(object p)
        {
            capi.Network.SendEntityPacket(pet.EntityId, p);
        }
    }
}