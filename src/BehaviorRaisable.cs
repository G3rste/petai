using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace WolfTaming
{
    public class EntityBehaviorRaisable : EntityBehavior
    {
        public EntityBehaviorRaisable(Entity entity) : base(entity)
        {
        }
        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
            EntityPlayer player = byEntity as EntityPlayer;
            if (player != null && itemslot.Empty)
            {
                ICoreClientAPI capi = entity.Api as ICoreClientAPI;
                if (byEntity.Controls.Sneak)
                {
                    new TaskSelectionGui(capi, player, entity as EntityAgent).TryOpen();
                }
                else
                {
                    entity.GetBehavior<EntityBehaviorReceiveCommand>()?.setCommand(player.GetBehavior<EntityBehaviorGiveCommand>().activeCommand, player);
                }
            }
        }
        public override string PropertyName()
        {
            return "raisable";
        }
    }
}