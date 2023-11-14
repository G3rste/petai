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

            Shape gearShape = null;
            AssetLocation shapePath = null;
            CompositeShape compGearShape = null;
            if (stack.Collectible is IWearableShapeSupplier iwss)
            {
                gearShape = iwss.GetShape(stack, this);
            }

            if (gearShape == null)
            {
                compGearShape = !attrObj["attachShape"].Exists ? (stack.Class == EnumItemClass.Item ? stack.Item.Shape : stack.Block.Shape) : attrObj["attachShape"].AsObject<CompositeShape>(null, stack.Collectible.Code.Domain);
                shapePath = shapePath = compGearShape.Base.CopyWithPath("shapes/" + compGearShape.Base.Path + ".json");
                gearShape = Shape.TryGet(Api, shapePath);
                if (gearShape == null)
                {
                    Api.World.Logger.Warning("Entity armor shape {0} defined in {1} {2} not found or errored, was supposed to be at {3}. Armor piece will be invisible.", compGearShape.Base, stack.Class, stack.Collectible.Code, shapePath);
                    return null;
                }
            }


            bool added = false;
            foreach (var val in gearShape.Elements)
            {
                ShapeElement elem;

                if (val.StepParentName != null)
                {
                    elem = entityShape.GetElementByName(val.StepParentName, StringComparison.InvariantCultureIgnoreCase);
                    if (elem == null)
                    {
                        Api.World.Logger.Warning("Entity gear shape {0} defined in {1} {2} requires step parent element with name {3}, but no such element was found in shape {4}. Will not be visible.", compGearShape.Base, slot.Itemstack.Class, slot.Itemstack.Collectible.Code, val.StepParentName, shapePathForLogging);
                        continue;
                    }
                }
                else
                {
                    Api.World.Logger.Warning("Entity gear shape element {0} in shape {1} defined in {2} {3} did not define a step parent element. Will not be visible.", val.Name, compGearShape.Base, slot.Itemstack.Class, slot.Itemstack.Collectible.Code);
                    continue;
                }

                if (elem.Children == null)
                {
                    elem.Children = new ShapeElement[] { val };
                }
                else
                {
                    elem.Children = (ShapeElement[])elem.Children.Append(val);
                }

                val.ParentElement = elem;

                val.WalkRecursive((el) =>
                {
                    el.DamageEffect = damageEffect;

                    foreach (var face in el.FacesResolved)
                    {
                        if (face != null) face.Texture = stack.Collectible.Code + "-" + face.Texture;
                    }
                });

                added = true;
            }

            if (gearShape.Animations != null)
            {
                foreach (var gearAnim in gearShape.Animations)
                {
                    var entityAnim = entityShape.Animations.FirstOrDefault(anim => anim.Code == gearAnim.Code);
                    if (entityAnim != null)
                    {
                        for (int gi = 0; gi < gearAnim.KeyFrames.Length; gi++)
                        {
                            var gearKeyFrame = gearAnim.KeyFrames[gi];
                            var entityKeyFrame = getOrCreateKeyFrame(entityAnim, gearKeyFrame.Frame);

                            foreach (var val in gearKeyFrame.Elements)
                            {
                                entityKeyFrame.Elements[val.Key] = val.Value;
                            }
                        }
                    }
                }
            }

            if (added && gearShape.Textures != null)
            {
                Dictionary<string, AssetLocation> newdict = new Dictionary<string, AssetLocation>();
                foreach (var val in gearShape.Textures)
                {
                    newdict[stack.Collectible.Code + "-" + val.Key] = val.Value;
                }

                // Item overrides
                var collDict = stack.Class == EnumItemClass.Block ? stack.Block.Textures : stack.Item.Textures;
                foreach (var val in collDict)
                {
                    newdict[stack.Collectible.Code + "-" + val.Key] = val.Value.Base;
                }

                gearShape.Textures = newdict;

                foreach (var val in gearShape.Textures)
                {
                    CompositeTexture ctex = new CompositeTexture() { Base = val.Value };

                    entityShape.TextureSizes[val.Key] = new int[] { gearShape.TextureWidth, gearShape.TextureHeight };

                    AssetLocation armorTexLoc = val.Value;

                    // Weird backreference to the shaperenderer. Should be refactored.
                    var texturesByLoc = extraTextureByLocation;
                    var texturesByName = extraTexturesByTextureName;

                    BakedCompositeTexture bakedCtex;

                    ICoreClientAPI capi = Api as ICoreClientAPI;

                    if (!texturesByLoc.TryGetValue(armorTexLoc, out bakedCtex))
                    {
                        int textureSubId = 0;
                        TextureAtlasPosition texpos;

                        capi.EntityTextureAtlas.GetOrInsertTexture(armorTexLoc, out textureSubId, out texpos, () =>
                        {
                            IAsset texAsset = Api.Assets.TryGet(armorTexLoc.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                            if (texAsset != null)
                            {
                                return texAsset.ToBitmap(capi);
                            }

                            capi.World.Logger.Warning("Entity armor shape {0} defined texture {1}, no such texture found.", shapePath, armorTexLoc);
                            return null;
                        });

                        ctex.Baked = new BakedCompositeTexture() { BakedName = armorTexLoc, TextureSubId = textureSubId };

                        texturesByName[val.Key] = ctex;
                        texturesByLoc[armorTexLoc] = ctex.Baked;
                    }
                    else
                    {
                        ctex.Baked = bakedCtex;
                        texturesByName[val.Key] = ctex;
                    }
                }

                foreach (var val in gearShape.TextureSizes)
                {
                    // fixhere start
                    entityShape.TextureSizes[stack.Collectible.Code + "-" + val.Key] = val.Value;
                    // fixhere end
                }
            }
            //foreach(var val in entityShape.TextureSizes)
            //    Api.Logger.Debug("texturesize for " + val.Key + " = "+ val.Value[0] + ", " +val.Value[1]);

            return entityShape;
        }
        private AnimationKeyFrame getOrCreateKeyFrame(Animation entityAnim, int frame)
        {
            for (int ei = 0; ei < entityAnim.KeyFrames.Length; ei++)
            {
                var entityKeyFrame = entityAnim.KeyFrames[ei];
                if (entityKeyFrame.Frame == frame)
                {
                    return entityKeyFrame;
                }
            }

            for (int ei = 0; ei < entityAnim.KeyFrames.Length; ei++)
            {
                var entityKeyFrame = entityAnim.KeyFrames[ei];
                if (entityKeyFrame.Frame > frame)
                {
                    var kfm = new AnimationKeyFrame() { Frame = frame };
                    entityAnim.KeyFrames = entityAnim.KeyFrames.InsertAt(kfm, frame);
                    return kfm;
                }
            }

            var kf = new AnimationKeyFrame() { Frame = frame };
            entityAnim.KeyFrames = entityAnim.KeyFrames.InsertAt(kf, 0);
            return kf;
        }
    }
}