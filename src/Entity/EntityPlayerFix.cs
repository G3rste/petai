using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using System.Linq;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace PetAI
{
    public class EntityPlayerFix : EntityPlayer
    {

        // Everything beyond this point fixes a vanilla bug responsible for not properly rendering wearables when there a textures with multiple resolutions
        // to find the fix -> search for fixhere 
        protected override Shape addGearToShape(ItemSlot slot, Shape entityShape, string shapePathForLogging)
        {
            if (slot.Empty) return entityShape;
            ItemStack stack = slot.Itemstack;
            JsonObject attrObj = stack.Collectible.Attributes;

            float damageEffect = 0;
            if (stack.ItemAttributes?["visibleDamageEffect"].AsBool() == true)
            {
                damageEffect = Math.Max(0, 1 - (float)stack.Collectible.GetRemainingDurability(stack) / stack.Collectible.GetMaxDurability(stack) * 1.1f);
            }

            string[] disableElements = attrObj?["disableElements"]?.AsArray<string>(null);
            if (disableElements != null)
            {
                foreach (var val in disableElements)
                {
                    entityShape.RemoveElementByName(val);
                }
            }

            if (attrObj?["wearableAttachment"].Exists != true) return entityShape;

            Shape gearShape=null;
            AssetLocation shapePath;
            CompositeShape compGearShape = null;
            if (stack.Collectible is IWearableShapeSupplier iwss)
            {
                gearShape = iwss.GetShape(stack, this);
            }

            if (gearShape == null) {
                compGearShape = !attrObj["attachShape"].Exists ? (stack.Class == EnumItemClass.Item ? stack.Item.Shape : stack.Block.Shape) : attrObj["attachShape"].AsObject<CompositeShape>(null, stack.Collectible.Code.Domain);
                shapePath = compGearShape.Base.CopyWithPath("shapes/" + compGearShape.Base.Path + ".json");
                gearShape = Shape.TryGet(Api, shapePath);
                if (gearShape == null)
                {
                    Api.World.Logger.Warning("Entity armor shape {0} defined in {1} {2} not found or errored, was supposed to be at {3}. Armor piece will be invisible.", compGearShape.Base, stack.Class, stack.Collectible.Code, shapePath);
                    return null;
                }
            }

            string texturePrefixCode = stack.Collectible.Code.ToShortString();

            // Item stack textures take precedence over shape textures
            if (gearShape.Textures == null) gearShape.Textures = new Dictionary<string, AssetLocation>();
            var collectibleDict = stack.Class == EnumItemClass.Block ? stack.Block.Textures : stack.Item.Textures;
            foreach (var val in collectibleDict)
            {
                gearShape.Textures[val.Key] = val.Value.Base;
            }

            var textures = Properties.Client.Textures;
            Api.Logger.Debug("Doing the thing!");
            entityShape.StepParentShape(
                gearShape, 
                texturePrefixCode, 
                compGearShape?.Base.ToString() ?? "Custom texture from ItemWearableShapeSupplier " + string.Format("defined in {0} {1}", stack.Class, stack.Collectible.Code),
                shapePathForLogging, 
                Api.World.Logger,
                (texcode, tloc) =>
                {
                    var cmpt = textures[texturePrefixCode + "-" + texcode] = new CompositeTexture(tloc);
                    cmpt.Bake(Api.Assets);
                    (Api as ICoreClientAPI).EntityTextureAtlas.GetOrInsertTexture(cmpt.Baked.TextureFilenames[0], out int textureSubid, out _);
                    cmpt.Baked.TextureSubId = textureSubid;
                },
                damageEffect
            );


            return entityShape;
        }
    }
}