using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PetAI
{
    public class ItemPetWhistle : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity?.Controls?.Sneak == true)
            {
                if (byEntity?.Api?.Side == EnumAppSide.Client
                    && byEntity is EntityPlayer
                    && entitySel?.Entity?.HasBehavior<EntityBehaviorTameable>() != true)
                {
                    new TaskSelectionGui(byEntity.Api as ICoreClientAPI, byEntity as EntityPlayer).TryOpen();
                }
            }
            else
            {
                handling = EnumHandHandling.PreventDefaultAnimation;
                byEntity.AnimManager?.StartAnimation("eat");
                if (byEntity.Api?.Side == EnumAppSide.Server)
                {
                    byEntity.World?.PlaySoundAt(new AssetLocation("petai:sounds/whistling.ogg"), byEntity.ServerPos.X, byEntity.ServerPos.Y, byEntity.ServerPos.Z);
                }
                notifyNearbyPets(byEntity);
            }
        }

        private void notifyNearbyPets(EntityAgent byEntity)
        {
            var giveBehavior = byEntity.GetBehavior<EntityBehaviorGiveCommand>();
            if (giveBehavior == null) return;

            var command = giveBehavior.activeCommand;
            if (command == null) return;

            var petArray = byEntity.World.GetEntitiesAround(byEntity.ServerPos.XYZ, 15, 5, entity => entity.HasBehavior<EntityBehaviorReceiveCommand>());

            var player = byEntity as EntityPlayer;
            Entity target = null;
            if (player != null && command.commandName == "settarget")
            {
                EntitySelection entitySel = null;
                BlockSelection blockSel = null;
                Vec3d pos = player.Pos.XYZ.Add(player.LocalEyePos);
                player.World.RayTraceForSelection(pos, player.SidedPos.Pitch, player.SidedPos.Yaw, 50, ref blockSel, ref entitySel);
                giveBehavior.victim = entitySel?.Entity;
                target = entitySel?.Entity;
            }
            if (command.commandName == "removetarget")
            {
                giveBehavior.victim = null;
                giveBehavior.attacker = null;

            }

            foreach (var pet in petArray)
            {
                var receiveBehavior = pet.GetBehavior<EntityBehaviorReceiveCommand>();
                receiveBehavior.setCommand(command, byEntity as EntityPlayer);

                var taskManager = pet.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager;
                var seekTask = taskManager?.GetTask<AiTaskPetSeekEntity>();
                var attackTask = taskManager?.GetTask<AiTaskPetMeleeAttack>();

                if (target != null
                    && seekTask != null
                    && attackTask != null
                    && (receiveBehavior.aggressionLevel == EnumAggressionLevel.PROTECTIVE || receiveBehavior.aggressionLevel == EnumAggressionLevel.AGGRESSIVE))
                {
                    seekTask.targetEntity = target;
                    attackTask.targetEntity = target;
                }

                if (command.commandName == "removetarget")
                {
                    taskManager?.StopTask(typeof(AiTaskPetMeleeAttack));
                    taskManager?.StopTask(typeof(AiTaskPetSeekEntity));
                    seekTask.targetEntity = null;
                    seekTask.ResetAttackedByEntity();
                    attackTask.targetEntity = null;
                    attackTask.ResetAttackedByEntity();
                }
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            // For some reason setting the handling to prevent default does not seem to work
            byEntity.AnimManager?.StopAnimation("PlaceBlock");
            return secondsUsed < 3;
        }
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {

            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "petai:interact-whistle-select",
                    HotKeyCode = "sneak",
                    MouseButton = EnumMouseButton.Right,
                },
                new WorldInteraction()
                {
                    ActionLangCode = "petai:interact-whistle-command",
                    MouseButton = EnumMouseButton.Right,
                }
            };
        }
    }
}