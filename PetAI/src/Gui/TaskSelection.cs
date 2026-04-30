using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace PetAI
{
    public class TaskSelectionGui : GuiDialog
    {
        public override string ToggleKeyCombinationCode => null;

        readonly private EntityAgent targetEntity;

        readonly private EntityPlayer player;
        private int currentX = 0;
        private int currentY = 0;
        List<Command> availableCommands;

        public TaskSelectionGui(ICoreClientAPI capi, EntityPlayer player, EntityAgent targetEntity = null) : base(capi)
        {
            this.targetEntity = targetEntity;
            this.player = player;
            ComposeGui();
        }

        public void ComposeGui()
        {
            currentY = 20;
            currentX = 0;
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            if (targetEntity == null
                || !targetEntity.HasBehavior<EntityBehaviorTameable>()
                || targetEntity.GetBehavior<EntityBehaviorReceiveCommand>().AvailableCommands.Keys.Count == 0)
            {
                ComposeStaticDialogue(dialogBounds, bgBounds);
            }
            else
            {
                ComposeDynamicDialogue(dialogBounds, bgBounds);
            }
        }

        private void ComposeDynamicDialogue(ElementBounds dialogBounds, ElementBounds bgBounds)
        {
            availableCommands = [.. targetEntity.GetBehavior<EntityBehaviorReceiveCommand>().AvailableCommands.Keys];
            SingleComposer = capi.Gui.CreateCompo("CommandDialog-", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("petai:gui-command-title"), () => TryClose())
                .BeginChildElements(bgBounds);

            AddGuiRow(EnumCommandType.SIMPLE, "petai:gui-command-simple");
            AddGuiRow(EnumCommandType.COMPLEX, "petai:gui-command-complex");
            AddGuiRow(EnumCommandType.AGGRESSIONLEVEL, "petai:gui-command-aggressionlevel");

            SingleComposer.AddIconButton("necklace", OnToggleProfile, ElementBounds.FixedOffseted(EnumDialogArea.RightTop, 0, 20, 30, 30))
                .EndChildElements()
            .Compose();
        }

        private void AddGuiRow(EnumCommandType type, string headline)
        {
            if (availableCommands.Exists(command => command.Type == type))
            {
                SingleComposer.AddStaticText(Lang.Get(headline), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, currentY, 300, 20));
                currentY += 35;
                currentX = 0;

                foreach (var command in availableCommands.FindAll(command => command.Type == type))
                {
                    SingleComposer.AddButton(Lang.Get(string.Format("petai:gui-command-{0}", command.CommandName.ToLower())), () => OnCommandClick(command), ElementBounds.Fixed(currentX, currentY, 135, 45));
                    currentX += 150;
                }
                currentY += 50;
            }
        }

        private void ComposeStaticDialogue(ElementBounds dialogBounds, ElementBounds bgBounds)
        {
            SingleComposer = capi.Gui.CreateCompo("CommandDialog-", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("petai:gui-command-title"), () => TryClose())
                .BeginChildElements(bgBounds)
                    .AddStaticText(Lang.Get("petai:gui-command-simple"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 20, 300, 20))
                    .AddButton(Lang.Get("petai:gui-command-sit"), () => OnCommandClick(new Command(EnumCommandType.SIMPLE, "sit")), ElementBounds.Fixed(0, 50, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-lay"), () => OnCommandClick(new Command(EnumCommandType.SIMPLE, "lay")), ElementBounds.Fixed(150, 50, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-speak"), () => OnCommandClick(new Command(EnumCommandType.SIMPLE, "speak")), ElementBounds.Fixed(300, 50, 135, 45))
                    .AddStaticText(Lang.Get("petai:gui-command-complex"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 100, 300, 20))
                    .AddButton(Lang.Get("petai:gui-command-followmaster"), () => OnCommandClick(new Command(EnumCommandType.COMPLEX, "followmaster")), ElementBounds.Fixed(0, 135, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-stay"), () => OnCommandClick(new Command(EnumCommandType.COMPLEX, "stay")), ElementBounds.Fixed(150, 135, 135, 45))
                    .AddStaticText(Lang.Get("petai:gui-command-aggressionlevel"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 185, 300, 20))
                    .AddButton(Lang.Get("petai:gui-command-neutral"), () => OnCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.NEUTRAL.ToString())), ElementBounds.Fixed(0, 220, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-protective"), () => OnCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.PROTECTIVE.ToString())), ElementBounds.Fixed(150, 220, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-aggressive"), () => OnCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.AGGRESSIVE.ToString())), ElementBounds.Fixed(300, 220, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-passive"), () => OnCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.PASSIVE.ToString())), ElementBounds.Fixed(450, 220, 135, 45))
                    .AddStaticText(Lang.Get("petai:gui-command-attackorder"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 270, 300, 20))
                    .AddButton(Lang.Get("petai:gui-command-settarget"), () => OnCommandClick(new Command(EnumCommandType.ATTACKORDER, "settarget")), ElementBounds.Fixed(0, 305, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-removetarget"), () => OnCommandClick(new Command(EnumCommandType.ATTACKORDER, "removetarget")), ElementBounds.Fixed(150, 305, 135, 45))
                .EndChildElements()
                .Compose();
        }
        private bool OnCommandClick(Command command)
        {
            var message = new PetCommandMessage
            {
                commandName = command.CommandName,
                commandType = command.Type.ToString(),
                playerUID = player.PlayerUID
            };
            if (targetEntity != null)
            {
                message.targetEntityUID = targetEntity.EntityId;
            }

            if (targetEntity != null
                && targetEntity.HasBehavior<EntityBehaviorTameable>()
                && command.CommandName != "dropgear"
                && targetEntity.GetBehavior<EntityBehaviorReceiveCommand>().AvailableCommands[command] > targetEntity.GetBehavior<EntityBehaviorTameable>().Obedience)
            {
                capi.ShowChatMessage(Lang.Get("petai:gui-pet-disobey", targetEntity.GetBehavior<EntityBehaviorReceiveCommand>().AvailableCommands[command] * 100));
                return true;
            }

            TryClose();

            capi.Network.GetChannel("petainetwork").SendPacket<PetCommandMessage>(message);
            return true;
        }

        private void OnToggleProfile(bool value)
        {
            var gui = new PetProfileGUI(capi, targetEntity.EntityId);
            gui.TryClose();
            gui.TryOpen();
            TryClose();
        }
    }
}