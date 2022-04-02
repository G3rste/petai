using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using System.Collections.Generic;

namespace PetAI
{
    public class EntityBehaviorReceiveCommand : EntityBehavior
    {
        private string _simpleCommand;

        protected TaskSelectionGui gui;
        public string simpleCommand
        {
            get
            {
                if (isEntityObedient(new Command(EnumCommandType.SIMPLE, _simpleCommand))) return _simpleCommand;
                else return null;

            }
            private set
            {
                _simpleCommand = value;
            }
        }
        public string complexCommand
        {
            get
            {
                string commandName = entity.WatchedAttributes.GetString("activeCommand");
                if (isEntityObedient(new Command(EnumCommandType.COMPLEX, commandName))) return commandName;
                else return null;
            }
            private set
            {
                entity.WatchedAttributes.SetString("activeCommand", value);
            }
        }

        public EnumAggressionLevel aggressionLevel
        {
            get
            {
                if (entity.GetBehavior<EntityBehaviorTameable>().domesticationLevel == DomesticationLevel.WILD) { return EnumAggressionLevel.AGGRESSIVE; }
                EnumAggressionLevel level;
                string commandName = entity.WatchedAttributes.GetString("aggressionLevel");
                if (Enum.TryParse<EnumAggressionLevel>(commandName, out level) && (isEntityObedient(new Command(EnumCommandType.AGGRESSIONLEVEL, commandName))))
                {
                    return level;
                }
                return EnumAggressionLevel.NEUTRAL;
            }

            private set
            {
                entity.WatchedAttributes.SetString("aggressionLevel", value.ToString());
            }
        }
        public Dictionary<Command, float> availableCommands { get; private set; } = new Dictionary<Command, float>();
        public EntityBehaviorReceiveCommand(Entity entity) : base(entity)
        {
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            JsonObject[] commands = attributes["availableCommands"]?.AsArray();
            if (commands == null) commands = new JsonObject[0];
            foreach (var item in commands)
            {
                string commandName = item["commandName"].AsString();
                EnumCommandType type;
                Enum.TryParse<EnumCommandType>(item["commandType"].AsString(), out type);
                float minObedience = item["minObedience"].AsFloat(1f);

                availableCommands.Add(new Command(type, commandName), minObedience);
            }
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
            EntityPlayer player = byEntity as EntityPlayer;
            if (entity.GetBehavior<EntityBehaviorTameable>()?.domesticationLevel != DomesticationLevel.WILD
                && player != null
                && player.PlayerUID == entity.GetBehavior<EntityBehaviorTameable>()?.owner?.PlayerUID
                && byEntity.Controls.Sneak
                && mode == EnumInteractMode.Interact)
            {
                if (entity.Api.Side == EnumAppSide.Client)
                {
                    if (gui == null) { gui = new TaskSelectionGui(entity.Api as ICoreClientAPI, player, entity as EntityAgent); }
                    else { gui.composeGui(); }
                    gui.TryOpen();
                }
            }
        }
        public void setCommand(Command command, EntityPlayer byPlayer)
        {
            if (command == null) return;
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

        private bool isEntityObedient(Command command)
        {
            if (command == null || command.commandName == null) return true;
            if (entity.HasBehavior<EntityBehaviorTameable>() && availableCommands.ContainsKey(command))
            {
                return entity.GetBehavior<EntityBehaviorTameable>().obedience >= availableCommands[command];
            }
            return true;
        }
        public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "petai:interact-command",
                    HotKeyCode = "sneak",
                    MouseButton = EnumMouseButton.Right,
                }
            };
        }
    }
}