using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace PetAI
{
    public class PetNameGUI : GuiDialog
    {
        public override string ToggleKeyCombinationCode => null;

        private long targetEntityId;

        string petName;

        public PetNameGUI(ICoreClientAPI capi, long targetEntityId) : base(capi)
        {
            this.targetEntityId = targetEntityId;

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            SingleComposer = capi.Gui.CreateCompo("PetNameDialog-", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("petai:gui-name-title"), () => TryClose())
                .BeginChildElements(bgBounds)
                    .AddTextInput(ElementBounds.Fixed(0, 20, 200, 40), (name) => petName = name)
                    .AddButton(Lang.Get("petai:gui-name-ok"), () => onClick(), ElementBounds.Fixed(0, 70, 90, 40))
                .EndChildElements()
                .Compose();
        }
        private bool onClick()
        {
            var message = new PetNameMessage();
            message.petName = petName;
            message.targetEntityUID = targetEntityId;

            capi.Network.GetChannel("petainetwork").SendPacket<PetNameMessage>(message);

            TryClose();
            return true;
        }
    }
}