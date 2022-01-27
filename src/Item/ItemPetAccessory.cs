using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace PetAI
{
    public class ItemPetAccessory : Item
    {
        public MeshRef inventoryMesh { get; private set; }

        public string inventoryShapePath => Attributes["inventoryShape"].AsString();
        public PetAccessoryType type
        {
            get
            {
                PetAccessoryType type;
                if (Enum.TryParse<PetAccessoryType>(Attributes["cothingType"].AsString(), out type))
                {
                    return type;
                }
                else
                {
                    return PetAccessoryType.NECK;
                }
            }
        }
        public int backpackSlots => Attributes["backpackslots"].AsInt(0);
        public bool canBeWornBy(string pet) => Attributes["validPets"].AsArray<string>(new string[0]).Contains(pet);

        public float damageReduction => Attributes["damageReduction"].AsFloat(0);

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            if (!String.IsNullOrEmpty(inventoryShapePath))
            {
                if (inventoryMesh == null)
                {
                    Shape shape = capi.Assets.Get(new AssetLocation(inventoryShapePath)).ToObject<Shape>();
                    MeshData mesh;
                    capi.Tesselator.TesselateShape(this, shape, out mesh);
                    inventoryMesh = capi.Render.UploadMesh(mesh);
                }
                renderinfo.ModelRef = inventoryMesh;
            }
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }
    }
    public enum PetAccessoryType
    {
        NECK, HEAD, BACK, BODY, TAIL, PAWS
    }
}