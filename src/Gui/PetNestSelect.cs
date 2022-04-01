using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace PetAI
{
    public class PetNestSelect : GuiDialog
    {

        public override string ToggleKeyCombinationCode => null;

        private long? selectedPetId;

        public PetNestSelect(ICoreClientAPI capi, List<PetDataSmall> availablePets, BlockPos selectedNest) : base(capi)
        {

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            SingleComposer = capi.Gui.CreateCompo("PetNestDialog-", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("petai:gui-petnest-title"), () => TryClose())
                .BeginChildElements(bgBounds);
            if (availablePets != null && availablePets.Count > 0)
            {
                BlockEntityPetNest nest = capi.World.BlockAccessor.GetBlockEntity(selectedNest) as BlockEntityPetNest;
                int index = availablePets.FindIndex(data => data.petId == nest.petId);
                if (index == -1) { index = 0; }
                selectedPetId = availablePets[index].petId;

                SingleComposer.AddStaticText(Lang.Get("petai:gui-petnest-name"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 20, 200, 20));
                SingleComposer.AddDropDown(availablePets.ConvertAll<string>(data => data.petId.ToString()).ToArray(),
                    availablePets.ConvertAll<string>(data => !String.IsNullOrEmpty(data.petName) ? data.petName : Lang.Get(data.petType)).ToArray(),
                    index,
                    (code, selected) => selectedPetId = Convert.ToInt64(code),
                    ElementBounds.Fixed(200, 20, 200, 30),
                    CairoFont.WhiteSmallishText(),
                    "petInput");
            }
            else
            {
                SingleComposer.AddStaticText(Lang.Get("petai:gui-petnest-nopets"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 20, 200, 20));
            }
            SingleComposer.AddButton(Lang.Get("petai:gui-profile-ok"), () => onClick(selectedNest), ElementBounds.Fixed(0, 70, 90, 40))
                .AddButton(Lang.Get("petai:gui-profile-cancel"), () => TryClose(), ElementBounds.Fixed(150, 70, 90, 40))
                .EndChildElements()
                .Compose();
        }

        private bool onClick(BlockPos selectedNest)
        {
            if (selectedPetId != null)
            {
                var message = new PetNestMessage();
                message.selectedPet = (long)selectedPetId;
                message.selectedNest = selectedNest;

                capi.Network.GetChannel("petainetwork").SendPacket<PetNestMessage>(message);
            }

            TryClose();
            return true;
        }
    }
}