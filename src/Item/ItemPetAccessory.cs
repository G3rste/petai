using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace PetAI
{
    public class ItemPetAccessory : Item
    {
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
    }
    public enum PetAccessoryType
    {
        NECK, HEAD, BACK, BODY, TAIL, PAWS
    }
}