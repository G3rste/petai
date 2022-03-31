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

        private long selectedPetId;

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
                SingleComposer.AddStaticText(Lang.Get("petai:gui-petnest-name"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 20, 200, 20));
                SingleComposer.AddDropDown(availablePets.ConvertAll<string>(data => data.petId.ToString()).ToArray(),
                    availablePets.ConvertAll<string>(data => !String.IsNullOrEmpty(data.petName) ? data.petName : Lang.Get(data.petType)).ToArray(),
                    0,
                    (code, selected) => selectedPetId = Convert.ToInt64(code),
                    ElementBounds.Fixed(200, 20, 200, 30),
                    CairoFont.WhiteSmallishText(),
                    "petInput");
            }else{                SingleComposer.AddStaticText(Lang.Get("petai:gui-petnest-nopets"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 20, 200, 20));
}
            SingleComposer.AddButton(Lang.Get("petai:gui-profile-ok"), () => onClick(selectedNest), ElementBounds.Fixed(0, 70, 90, 40))
                .AddButton(Lang.Get("petai:gui-profile-cancel"), () => TryClose(), ElementBounds.Fixed(150, 70, 90, 40))
                .EndChildElements()
                .Compose();
        }

        private bool onClick(BlockPos selectedNest)
        {
            var message = new PetNestMessage();
            message.selectedPet = selectedPetId;
            message.selectedNest = selectedNest;

            capi.Network.GetChannel("petainetwork").SendPacket<PetNestMessage>(message);

            TryClose();
            return true;
        }
    }
}