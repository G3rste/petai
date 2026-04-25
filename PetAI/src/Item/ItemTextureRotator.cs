using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PetAI
{
    public class ItemTextureRotator : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            var oldEntity = entitySel?.Entity;
            if (api is ICoreServerAPI sapi && oldEntity != null && firstEvent)
            {
                int oldtexture = oldEntity.WatchedAttributes.GetInt("textureIndex", 0);
                oldEntity.WatchedAttributes.SetInt("textureIndex", (oldtexture + 1) % (oldEntity.Properties.Client.TexturesAlternatesCount + 1));
                oldEntity.WatchedAttributes.MarkPathDirty("textureIndex");
                sapi.World.SpawnEntity(PetUtil.EntityFromTree(PetUtil.EntityToTree(oldEntity), sapi.World));
                oldEntity.Die(EnumDespawnReason.Removed);
            }
            handling = EnumHandHandling.Handled;
        }
    }
}