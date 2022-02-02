using Vintagestory.API.Common;

namespace PetAI {
    class BehaviorConsiderHumanFoodForPetsToo : CollectibleBehavior
    {
        public BehaviorConsiderHumanFoodForPetsToo(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            EntityBehaviorTameable tameable = entitySel?.Entity?.GetBehavior<EntityBehaviorTameable>();
            if(tameable != null){
                tameable.OnInteract(byEntity, slot, null, EnumInteractMode.Interact, ref handling);
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }
    }
}