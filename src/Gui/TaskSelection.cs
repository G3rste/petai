using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

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
                .AddDialogTitleBar(Lang.Get("wolftaming:gui-command-title"), () => TryClose())
                .BeginChildElements(bgBounds)
                    .AddStaticText(Lang.Get("wolftaming:gui-command-simple"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 20, 200, 20))
                    .AddButton(Lang.Get("wolftaming:gui-command-sit"), () => onCommandClick(new Command(EnumCommandType.SIMPLE, "sit")), ElementBounds.Fixed(0, 50, 135, 45))
                    .AddButton(Lang.Get("wolftaming:gui-command-lay"), () => onCommandClick(new Command(EnumCommandType.SIMPLE, "lay")), ElementBounds.Fixed(150, 50, 135, 45))
                    .AddButton(Lang.Get("wolftaming:gui-command-speak"), () => onCommandClick(new Command(EnumCommandType.SIMPLE, "speak")), ElementBounds.Fixed(300, 50, 135, 45))
                    .AddStaticText(Lang.Get("wolftaming:gui-command-complex"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 100, 200, 20))
                    .AddButton(Lang.Get("wolftaming:gui-command-follow"), () => onCommandClick(new Command(EnumCommandType.COMPLEX, "followmaster")), ElementBounds.Fixed(0, 135, 135, 45))
                    .AddButton(Lang.Get("wolftaming:gui-command-stay"), () => onCommandClick(new Command(EnumCommandType.COMPLEX, "stay")), ElementBounds.Fixed(150, 135, 135, 45))
                    .AddStaticText(Lang.Get("wolftaming:gui-command-aggressionlevel"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 185, 200, 20))
                    .AddButton(Lang.Get("wolftaming:gui-command-neutral"), () => onCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.SELFDEFENSE.ToString())), ElementBounds.Fixed(0, 220, 135, 45))
                    .AddButton(Lang.Get("wolftaming:gui-command-protect"), () => onCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.PROTECTMASTER.ToString())), ElementBounds.Fixed(150, 220, 135, 45))
                    .AddButton(Lang.Get("wolftaming:gui-command-attack"), () => onCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.ATTACKTARGET.ToString())), ElementBounds.Fixed(300, 220, 135, 45))
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