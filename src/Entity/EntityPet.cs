using Vintagestory.API.Client;
using Vintagestory.API.Config;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
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

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            List<ItemStack> list = new List<ItemStack>(base.GetDrops(world, pos, byPlayer));
            foreach (ItemSlot slot in GearInventory)
            {
                list.Add(slot.Itemstack);
            }
            return list.ToArray();
        }

        public override string GetInfoText()
        {
            if (!HasBehavior<EntityBehaviorTameable>()) return base.GetInfoText();

            return String.Concat(base.GetInfoText(),
                    "\n",
                    Lang.Get("petai:gui-pet-obedience", Math.Round(GetBehavior<EntityBehaviorTameable>().obedience * 100), 2));
        }

        public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
            if (packetid < 1000)
            {
                //earInv.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                backpackInv.reloadFromSlots();
                backpackInv.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                Api.Logger.Debug("Itemslot 1 contains: {0}", backpackInv[0].Itemstack?.Item?.Code.Path);
                backpackInv.saveAllSlots();
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
    }
}