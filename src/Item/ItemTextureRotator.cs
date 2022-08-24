using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PetAI
{
    public class ItemTextureRotator : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            var textureMorpher = entitySel?.Entity;
            if (api is ICoreServerAPI sapi && textureMorpher != null && firstEvent)
            {
                int textures = 1;
                var alternates = textureMorpher.Properties.Client.FirstTexture.Alternates;
                if (alternates != null)
                {
                    textures += alternates.Length;
                }
                int oldtexture = textureMorpher.WatchedAttributes.GetInt("textureIndex", 0);
                textureMorpher.WatchedAttributes.SetInt("textureIndex", (oldtexture + 1) % textures);
                textureMorpher.WatchedAttributes.MarkPathDirty("textureIndex");
                sapi.World.SpawnEntity(PetUtil.EntityFromTree(PetUtil.EntityToTree(textureMorpher), sapi.World));
                textureMorpher.Die(EnumDespawnReason.Removed);

                var usagesleft = slot.Itemstack.Attributes.GetInt("usages", 5) - 1;
                slot.Itemstack.Attributes.SetInt("usages", usagesleft);
                if (usagesleft == 0)
                {
                    slot.TakeOut(1);
                    slot.Itemstack?.Attributes?.SetInt("usages", 5);
                }
                slot.MarkDirty();
            }
            handling = EnumHandHandling.Handled;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.Append(Lang.Get("petai:desc-texturerotator", inSlot.Itemstack.Attributes.GetInt("usages", 5)));
        }
    }
}