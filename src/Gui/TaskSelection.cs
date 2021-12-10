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

        private EntityAgent targetEntity;

        private EntityPlayer player;
        private int currentX = 0;
        private int currentY = 0;
        List<Command> availableCommands;

        public TaskSelectionGui(ICoreClientAPI capi, EntityPlayer player, EntityAgent targetEntity = null) : base(capi)
        {
            this.targetEntity = targetEntity;
            this.player = player;
            composeGui();
        }

        public void composeGui()
        {
            currentY = 20;
            currentX = 0;
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            if (targetEntity == null
                || !targetEntity.HasBehavior<EntityBehaviorTameable>()
                || targetEntity.GetBehavior<EntityBehaviorReceiveCommand>().availableCommands.Keys.Count == 0)
            {
                composeStaticDialogue(dialogBounds, bgBounds);
            }
            else
            {
                composeDynamicDialogue(dialogBounds, bgBounds);
            }
        }

        private void composeDynamicDialogue(ElementBounds dialogBounds, ElementBounds bgBounds)
        {
            availableCommands = new List<Command>(targetEntity.GetBehavior<EntityBehaviorReceiveCommand>().availableCommands.Keys);
            SingleComposer = capi.Gui.CreateCompo("CommandDialog-", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("petai:gui-command-title"), () => TryClose())
                .BeginChildElements(bgBounds);

            tryAddPetInventory();
            addGuiRow(EnumCommandType.SIMPLE, "petai:gui-command-simple");
            addGuiRow(EnumCommandType.COMPLEX, "petai:gui-command-complex");
            addGuiRow(EnumCommandType.AGGRESSIONLEVEL, "petai:gui-command-aggressionlevel");

            SingleComposer.EndChildElements()
            .Compose();
        }

        private void tryAddPetInventory()
        {
            var pet = targetEntity as EntityPet;
            if (pet != null && !pet.GearInventory.Empty)
            {
                pet.backpackInv.reloadFromSlots();
                SingleComposer.AddStaticText(Lang.Get("petai:gui-command-backpackinv"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, currentY, 200, 20));
                currentY += 35;
                SingleComposer.AddButton(Lang.Get("petai:gui-command-dropgear"), () => onCommandClick(new Command(EnumCommandType.SIMPLE, "dropgear")), ElementBounds.Fixed(currentX, currentY, 135, 45));
                double pad = GuiElementItemSlotGrid.unscaledSlotPadding;
                var slotbounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad + 150, currentY, pet.backpackInv.Count, 1).FixedGrow(2 * pad, 2 * pad);
                SingleComposer.AddItemSlotGrid(pet.backpackInv, SendBackPackPacket, pet.backpackInv.Count, slotbounds, "petBackPackInv");
                currentY += 55;
            }
        }

        private void addGuiRow(EnumCommandType type, string headline)
        {
            if (availableCommands.Exists(command => command.type == type))
            {
                SingleComposer.AddStaticText(Lang.Get(headline), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, currentY, 200, 20));
                currentY += 35;
                currentX = 0;

                foreach (var command in availableCommands.FindAll(command => command.type == type))
                {
                    SingleComposer.AddButton(Lang.Get(string.Format("petai:gui-command-{0}", command.commandName.ToLower())), () => onCommandClick(command), ElementBounds.Fixed(currentX, currentY, 135, 45));
                    currentX += 150;
                }
                currentY += 50;
            }
        }

        private void composeStaticDialogue(ElementBounds dialogBounds, ElementBounds bgBounds)
        {
            SingleComposer = capi.Gui.CreateCompo("CommandDialog-", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("petai:gui-command-title"), () => TryClose())
                .BeginChildElements(bgBounds)
                    .AddStaticText(Lang.Get("petai:gui-command-simple"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 20, 200, 20))
                    .AddButton(Lang.Get("petai:gui-command-sit"), () => onCommandClick(new Command(EnumCommandType.SIMPLE, "sit")), ElementBounds.Fixed(0, 50, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-lay"), () => onCommandClick(new Command(EnumCommandType.SIMPLE, "lay")), ElementBounds.Fixed(150, 50, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-speak"), () => onCommandClick(new Command(EnumCommandType.SIMPLE, "speak")), ElementBounds.Fixed(300, 50, 135, 45))
                    .AddStaticText(Lang.Get("petai:gui-command-complex"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 100, 200, 20))
                    .AddButton(Lang.Get("petai:gui-command-followmaster"), () => onCommandClick(new Command(EnumCommandType.COMPLEX, "followmaster")), ElementBounds.Fixed(0, 135, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-stay"), () => onCommandClick(new Command(EnumCommandType.COMPLEX, "stay")), ElementBounds.Fixed(150, 135, 135, 45))
                    .AddStaticText(Lang.Get("petai:gui-command-aggressionlevel"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, 185, 200, 20))
                    .AddButton(Lang.Get("petai:gui-command-passive"), () => onCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.PASSIVE.ToString())), ElementBounds.Fixed(0, 220, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-neutral"), () => onCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.NEUTRAL.ToString())), ElementBounds.Fixed(0, 220, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-defensive"), () => onCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.PROTECTIVE.ToString())), ElementBounds.Fixed(150, 220, 135, 45))
                    .AddButton(Lang.Get("petai:gui-command-aggressive"), () => onCommandClick(new Command(EnumCommandType.AGGRESSIONLEVEL, EnumAggressionLevel.AGGRESSIVE.ToString())), ElementBounds.Fixed(300, 220, 135, 45))
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

            if (targetEntity != null
                && targetEntity.HasBehavior<EntityBehaviorTameable>()
                && command.commandName != "dropgear"
                && targetEntity.GetBehavior<EntityBehaviorReceiveCommand>().availableCommands[command] > targetEntity.GetBehavior<EntityBehaviorTameable>().obedience)
            {
                capi.ShowChatMessage(Lang.Get("petai:gui-pet-disobey", targetEntity.GetBehavior<EntityBehaviorReceiveCommand>().availableCommands[command] * 100));
                return true;
            }

            TryClose();

            capi.Network.GetChannel("petainetwork").SendPacket<PetCommandMessage>(message);

            if (command.commandName == "dropgear")
            {
                (targetEntity as EntityPet).DropInventoryOnGround();
            }
            return true;
        }

        private void SendBackPackPacket(object p)
        {
            capi.Network.SendEntityPacket(targetEntity.EntityId, p);
            (targetEntity as EntityPet).backpackInv.saveAllSlots();
        }
    }
}