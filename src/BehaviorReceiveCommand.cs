using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;

namespace WolfTaming
{
    public class EntityBehaviorReceiveCommand : EntityBehavior
    {
        public string simpleCommand { get; private set; }

        public string complexCommand
        {
            get { return entity.WatchedAttributes.GetString("activeCommand"); }
            private set
            {
                entity.WatchedAttributes.SetString("activeCommand", value);
            }
        }

        public EnumAggressionLevel aggressionLevel
        {
            get
            {
                EnumAggressionLevel level;
                if (Enum.TryParse<EnumAggressionLevel>(entity.WatchedAttributes.GetString("aggressionLevel"), out level))
                {
                    return level;
                }
                return EnumAggressionLevel.SELFDEFENSE;
            }

            private set
            {
                entity.WatchedAttributes.SetString("aggressionLevel", value.ToString());
            }
        }
        public EntityBehaviorReceiveCommand(Entity entity) : base(entity)
        {
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
            EntityPlayer player = byEntity as EntityPlayer;
            if (entity.GetBehavior<EntityBehaviorTameable>()?.domesticationLevel != DomesticationLevel.WILD
                && player != null
                && itemslot.Empty)
            {
                ICoreClientAPI capi = entity.Api as ICoreClientAPI;
                if (byEntity.Controls.Sneak && capi != null)
                {
                    new TaskSelectionGui(capi, player, entity as EntityAgent).TryOpen();
                }
                if (!byEntity.Controls.Sneak && capi == null)
                {
                    setCommand(player.GetBehavior<EntityBehaviorGiveCommand>().activeCommand, player);
                }
            }
        }
        public void setCommand(Command command, EntityPlayer byPlayer)
        {
            if(command == null) return;
            if (byPlayer == null
                || entity.GetBehavior<EntityBehaviorTameable>()?.owner == null
                || entity.GetBehavior<EntityBehaviorTameable>().owner.PlayerUID == byPlayer.PlayerUID)
            {
                if (command.type == EnumCommandType.COMPLEX)
                {
                    complexCommand = command.commandName;

                    ITreeAttribute location = new TreeAttribute();
                    location.SetDouble("x", entity.ServerPos.X);
                    location.SetDouble("y", entity.ServerPos.Y);
                    location.SetDouble("z", entity.ServerPos.Z);

                    entity.WatchedAttributes.SetAttribute("staylocation", location);
                }
                if (command.type == EnumCommandType.SIMPLE)
                {
                    simpleCommand = command.commandName;
                }
                if (command.type == EnumCommandType.AGGRESSIONLEVEL)
                {
                    EnumAggressionLevel level;
                    if (Enum.TryParse<EnumAggressionLevel>(command.commandName, out level))
                    {
                        aggressionLevel = level;
                    }
                }
            }
        }

        public override string PropertyName()
        {
            return "receivecommand";
        }
    }
}