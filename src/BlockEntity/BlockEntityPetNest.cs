using System.Text;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace PetAI
{
    public class BlockEntityPetNest : BlockEntity, IPointOfInterest
    {
        public long? petId { get; set; }

        public string ownerId { get; set; }
        public string storedPetName { get; private set; }
        public string storedPetType { get; private set; }
        public PetData cachedPet { get; private set; }

        public EntityPlayer cachedOwner { get; private set; }

        public Vec3d Position => Pos.ToVec3d();

        public Vec3d MiddlePostion => Pos.ToVec3d().AddCopy(MiddelOffset);

        public Vec3d MiddelOffset => new Vec3d(Block.Attributes["x"].AsDouble(), Block.Attributes["y"].AsDouble(), Block.Attributes["z"].AsDouble());

        public string Type => "petnest";

        private PetManager petManager;

        private long listenerId;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            ICoreServerAPI sapi = api as ICoreServerAPI;
            if (sapi != null)
            {
                sapi.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
                listenerId = sapi.World.RegisterGameTickListener(RefreshPetStats, 60000);
                petManager = sapi.ModLoader.GetModSystem<PetManager>();
            }
        }

        private void RefreshPetStats(float obj)
        {
            if (petId == null) { return; }
            cachedPet = petManager.GetPet((long)petId);
            if (cachedPet?.nestLocation?.Equals(Pos) == false) { cachedPet = null; }
            storedPetName = cachedPet?.petName;
            storedPetType = cachedPet?.petType;

            if (cachedPet != null && !cachedPet.alive && cachedPet.deadUntil < Api.World.Calendar.TotalHours)
            {
                long? newPetId = petManager.RevivePet((long)petId, Pos);
                petManager.Remove((long)petId);
                petId = newPetId;
            }
            MarkDirty();
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);

            (Api as ICoreServerAPI)?.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
            (Api as ICoreServerAPI)?.World.UnregisterGameTickListener(listenerId);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            (Api as ICoreServerAPI)?.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
            (Api as ICoreServerAPI)?.World.UnregisterGameTickListener(listenerId);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (petId != null)
            {
                tree.SetLong("petId", (long)petId);
                tree.SetString("petName", storedPetName);
                tree.SetString("petType", storedPetType);
                tree.SetString("petOnwer", ownerId);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            if (tree.HasAttribute("petId"))
            {
                petId = tree.GetLong("petId");
                storedPetName = tree.GetString("petName");
                storedPetType = tree.GetString("petType");
                ownerId = tree.GetString("petOnwer");
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (String.IsNullOrEmpty(storedPetType)) { return; }
            string petDisplay = String.IsNullOrEmpty(storedPetName) ? Lang.Get(storedPetType) : storedPetName;
            if (!String.IsNullOrEmpty(petDisplay))
            {
                dsc.Append(Lang.Get("petai:blockdesc-petnest-homeof", petDisplay));
            }
        }
    }
}