using Vintagestory.API.Client;
using Vintagestory.API.Common;

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
                    notifyNearbyPets(byEntity);
                }
            }
        }

        private void notifyNearbyPets(EntityAgent byEntity)
        {
            var giveBehavior = byEntity.GetBehavior<EntityBehaviorGiveCommand>();
            if (giveBehavior == null) return;

            var command = giveBehavior.activeCommand;
            var victim = giveBehavior.victim;
            
            var petArray = byEntity.World.GetEntitiesAround(byEntity.ServerPos.XYZ, 15, 5, entity => entity.HasBehavior<EntityBehaviorReceiveCommand>());

            foreach (var pet in petArray)
            {
                pet.GetBehavior<EntityBehaviorReceiveCommand>().setCommand(command, byEntity as EntityPlayer);
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