using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PetAI
{
    public class ItemPetWhistle : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity?.Controls?.Sneak == true)
            {
                if (byEntity?.Api?.Side == EnumAppSide.Client && byEntity is EntityPlayer)
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
            if(command == null) return;

            var petArray = byEntity.World.GetEntitiesAround(byEntity.ServerPos.XYZ, 15, 5, entity => entity.HasBehavior<EntityBehaviorReceiveCommand>());

            foreach (var pet in petArray)
            {
                pet.GetBehavior<EntityBehaviorReceiveCommand>().setCommand(command, byEntity as EntityPlayer);
            }

            var player = byEntity as EntityPlayer;
            if (player != null && command.commandName == "settarget")
            {
                EntitySelection entitySel = null;
                BlockSelection blockSel = null;
                Vec3d pos = player.Pos.XYZ.Add(player.LocalEyePos);
                player.World.RayTraceForSelection(pos, player.SidedPos.Pitch, player.SidedPos.Yaw, 50, ref blockSel, ref entitySel);
                giveBehavior.victim = entitySel?.Entity;
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            // For some reason setting the handling to prevent default does not seem to work
            byEntity.AnimManager?.StopAnimation("PlaceBlock");
            return secondsUsed < 3;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            byEntity.AnimManager?.StopAnimation("eat");
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            byEntity.AnimManager?.StopAnimation("eat");
            return true;
        }
    }
}