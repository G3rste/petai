using Vintagestory.API.Config;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using System.IO;
using Vintagestory.GameContent;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace PetAI
{
    public class EntityPet : EntityAgent
    {
        protected InventoryPetGear gearInv;

        public InventorySlotBound backpackInv;
        public override IInventory GearInventory => gearInv;
        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            if (gearInv == null) gearInv = new InventoryPetGear(Code.Path, "petInv-" + EntityId, api);
            else gearInv.Api = api;
            gearInv.LateInitialize(gearInv.InventoryID, api);
            var slots = new ItemSlot[gearInv.Count];
            for (int i = 0; i < gearInv.Count; i++)
            {
                slots[i] = gearInv[i];
            }
            backpackInv = new InventorySlotBound("petBackPackInv-", api, slots);

            if (api.Side == EnumAppSide.Server)
            {
                GetBehavior<EntityBehaviorHealth>().onDamaged += (dmg, dmgSource) => applyPetArmor(dmg, dmgSource);
            }
        }


        public override void OnTesselation(ref Shape entityShape, string shapePathForLogging)
        {
            foreach (var slot in GearInventory)
            {
                addGearToShape(slot, entityShape, shapePathForLogging);
            }
            base.OnTesselation(ref entityShape, shapePathForLogging);
        }

        public override void FromBytes(BinaryReader reader, bool forClient)
        {
            base.FromBytes(reader, forClient);

            if (gearInv == null) { gearInv = new InventoryPetGear(Code.Path, "petInv-" + EntityId, null); }
            gearInv.FromTreeAttributes(getInventoryTree());
        }

        public override void ToBytes(BinaryWriter writer, bool forClient)
        {
            try
            {
                gearInv.ToTreeAttributes(getInventoryTree());
            }
            catch (NullReferenceException)
            {
                // ignore, better save whats left of it
            }

            base.ToBytes(writer, forClient);
        }

        public override string GetInfoText()
        {
            var tameable = GetBehavior<EntityBehaviorTameable>();
            var owner = tameable?.cachedOwner;
            if (owner == null) return base.GetInfoText();

            return String.Concat(base.GetInfoText(),
                    "\n",
                    Lang.Get("petai:gui-pet-owner", owner?.PlayerName),
                    "\n",
                    tameable.domesticationLevel == DomesticationLevel.DOMESTICATED ? Lang.Get("petai:gui-pet-obedience", Math.Round(tameable.obedience * 100, 2)) : Lang.Get("petai:gui-pet-domesticationProgress", Math.Round(tameable.domesticationProgress * 100, 2)),
                    "\n",
                    Lang.Get("petai:gui-pet-nestsize", Lang.Get("petai:gui-pet-nestsize-" + tameable.size.ToString().ToLower())));
        }

        public void DropInventoryOnGround()
        {
            for (int i = gearInv.Count - 1; i >= 0; i--)
            {
                if (gearInv[i].Empty) { continue; }

                Api.World.SpawnItemEntity(gearInv[i].TakeOutWhole(), ServerPos.XYZ);
                gearInv.MarkSlotDirty(i);
            }
        }

        private ITreeAttribute getInventoryTree()
        {
            if (!WatchedAttributes.HasAttribute("petinventory"))
            {
                ITreeAttribute tree = new TreeAttribute();
                gearInv.ToTreeAttributes(tree);
                WatchedAttributes.SetAttribute("petinventory", tree);
            }
            return WatchedAttributes.GetTreeAttribute("petinventory");
        }
        private float applyPetArmor(float dmg, DamageSource dmgSource)
        {
            if (dmgSource.SourceEntity != null && dmgSource.Type != EnumDamageType.Heal)
            {
                foreach (var slot in GearInventory)
                {
                    if (!slot.Empty)
                    {
                        dmg *= 1 - (slot.Itemstack.Item as ItemPetAccessory).damageReduction;
                    }
                }
            }
            return dmg;
        }

        public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
        {
            if (PetConfig.Current.PvpOff
                && GetBehavior<EntityBehaviorTameable>()?.domesticationLevel != DomesticationLevel.WILD
                && damageSource.CauseEntity is EntityPlayer player
                && player.PlayerUID != GetBehavior<EntityBehaviorTameable>()?.ownerId
                || damageSource.Source == EnumDamageSource.Fall
                && PetConfig.Current.FalldamageOff)
            {
                return false;
            }
            return base.ShouldReceiveDamage(damageSource, damage);
        }

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