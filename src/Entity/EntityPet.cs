using Vintagestory.API.Config;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.IO;

namespace PetAI
{
    public class EntityPet : EntityAgent
    {
        protected InventoryBase gearInv;

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
               // GetBehavior<EntityBehaviorHealth>().onDamaged += (dmg, dmgSource) => handleDamaged(dmg, dmgSource);
            }
        }

        public override void OnTesselation(ref Shape entityShape, string shapePathForLogging)
        {
            base.OnTesselation(ref entityShape, shapePathForLogging);
            foreach (var slot in GearInventory)
            {
                addGearToShape(slot, entityShape, shapePathForLogging);
            }
        }

        public override void FromBytes(BinaryReader reader, bool forClient)
        {
            base.FromBytes(reader, forClient);

            if (gearInv == null) { gearInv = new InventoryPetGear(Code.Path, "petInv-" + EntityId, null); }
            gearInv.FromTreeAttributes(getInventoryTree());
        }

        public override void ToBytes(BinaryWriter writer, bool forClient)
        {
            gearInv.ToTreeAttributes(getInventoryTree());

            base.ToBytes(writer, forClient);
        }

        public override string GetInfoText()
        {
            if (!HasBehavior<EntityBehaviorTameable>()) return base.GetInfoText();

            return String.Concat(base.GetInfoText(),
                    "\n",
                    Lang.Get("petai:gui-pet-obedience", Math.Round(GetBehavior<EntityBehaviorTameable>().obedience * 100), 2));
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

        private float handleDamaged(float dmg, DamageSource dmgSource)
        {
            if (PetConfig.Current.petCanDie || GetBehavior<EntityBehaviorHealth>().Health > dmg || GetBehavior<EntityBehaviorTameable>().owner == null)
            {
                return dmg;
            }
            else
            {
                Vec3d pos = Pos.XYZ;
                
                SimpleParticleProperties smoke = new SimpleParticleProperties(
                        100, 150,
                        ColorUtil.ToRgba(80, 100, 100, 100),
                        new Vec3d(),
                        new Vec3d(2, 1, 2),
                        new Vec3f(-0.25f, 0f, -0.25f),
                        new Vec3f(0.25f, 0f, 0.25f),
                        0.51f,
                        -0.075f,
                        0.5f,
                        3f,
                        EnumParticleModel.Quad
                    );

                smoke.MinPos = pos.AddCopy(-1.5, -0.5, -1.5);
                World.SpawnParticles(smoke);
                GetBehavior<EntityBehaviorTameable>().owner.Entity.GetBehavior<EntityBehaviorGiveCommand>().savePet(this);
                Die(EnumDespawnReason.Removed);
                return 0f;
            }
        }
    }
}