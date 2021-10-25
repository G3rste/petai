using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace WolfTaming
{
    public class TaskSelectionGui : GuiDialog
    {
        public override string ToggleKeyCombinationCode => null;

        private EntityAgent targetEntity;

        private EntityPlayer player;
        public TaskSelectionGui(ICoreClientAPI capi, EntityPlayer player, EntityAgent targetEntity = null) : base(capi)
        {
            this.targetEntity = targetEntity;
            this.player = player;

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            SingleComposer = capi.Gui.CreateCompo("CommandDialog-", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Choose next PetCommand!", () => TryClose())
                .BeginChildElements(bgBounds)
                    .AddButton("Sit", () => onCommandClick(new Command(CommandType.SIMPLE, "sit")), ElementBounds.Fixed(0, 20, 90, 40))
                    .AddButton("Lay", () => onCommandClick(new Command(CommandType.SIMPLE, "lay")), ElementBounds.Fixed(100, 20, 90, 40))
                    .AddButton("Speak", () => onCommandClick(new Command(CommandType.SIMPLE, "speak")), ElementBounds.Fixed(200, 20, 90, 40))
                    .AddButton("Follow", () => onCommandClick(new Command(CommandType.COMPLEX, "followmaster")), ElementBounds.Fixed(0, 70, 90, 40))
                    .AddButton("Stay", () => onCommandClick(new Command(CommandType.COMPLEX, "stay")), ElementBounds.Fixed(100, 70, 90, 40))
                .EndChildElements()
                .Compose();
        }
        private bool onCommandClick(Command command)
        {
            var message = new PetCommandMessage();
            message.commandName = command.commandName;
            message.commandType = command.type.ToString();
            message.playerUID = player.PlayerUID;
            message.targetEntityUID = targetEntity.EntityId;

            capi.Network.GetChannel("wolftamingnetwork").SendPacket<PetCommandMessage>(message);

            TryClose();
            return true;
        }
    }
}