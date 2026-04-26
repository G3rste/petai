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
                    byEntity.World?.PlaySoundAt(new AssetLocation("petai:sounds/whistling.ogg"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
                }
                NotifyNearbyPets(byEntity);
            }
        }

        private void NotifyNearbyPets(EntityAgent byEntity)
        {
            var giveBehavior = byEntity.GetBehavior<EntityBehaviorGiveCommand>();
            if (giveBehavior == null) return;

            var command = giveBehavior.ActiveCommand;
            if (command == null) return;

            var petArray = byEntity.World.GetEntitiesAround(byEntity.Pos.XYZ, 15, 5, entity => entity.HasBehavior<EntityBehaviorReceiveCommand>());

            Entity target = null;
            if (byEntity is EntityPlayer player && command.CommandName == "settarget")
            {
                EntitySelection entitySel = null;
                BlockSelection blockSel = null;
                Vec3d pos = player.Pos.XYZ.Add(player.LocalEyePos);
                player.World.RayTraceForSelection(pos, player.Pos.Pitch, player.Pos.Yaw, 50, ref blockSel, ref entitySel);
                if (entitySel?.Entity?.GetBehavior<EntityBehaviorTameable>()?.OwnerId != player.PlayerUID)
                {
                    giveBehavior.Victim = entitySel?.Entity;
                    target = entitySel?.Entity;
                }
            }
            if (command.CommandName == "removetarget")
            {
                giveBehavior.Victim = null;
                giveBehavior.Attacker = null;

            }

            foreach (var pet in petArray)
            {
                var receiveBehavior = pet.GetBehavior<EntityBehaviorReceiveCommand>();
                receiveBehavior.SetCommand(command, byEntity as EntityPlayer);

                var taskManager = pet.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager;
                var seekTask = taskManager?.GetTask<AiTaskPetSeekEntity>();
                var attackTask = taskManager?.GetTask<AiTaskPetMeleeAttack>();

                if (target != null
                    && seekTask != null
                    && attackTask != null
                    && (receiveBehavior.AggressionLevel == EnumAggressionLevel.PROTECTIVE || receiveBehavior.AggressionLevel == EnumAggressionLevel.AGGRESSIVE))
                {
                    seekTask.targetEntity = target;
                }

                if (command.CommandName == "removetarget"
                    && seekTask != null
                    && attackTask != null)
                {
                    taskManager?.StopTask(typeof(AiTaskPetMeleeAttack));
                    taskManager?.StopTask(typeof(AiTaskPetSeekEntity));
                    seekTask.targetEntity = null;
                    seekTask.ClearAttacker();
                    attackTask.targetEntity = null;
                    attackTask.ClearAttacker();
                }



                foreach (var task in taskManager.AllTasks)
                {
                    (task as AiTaskBaseTargetable)?.ClearAttacker();
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

            return
            [
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
            ];
        }
    }
}
