using System;
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
        public int backpackSlots { get { return Attributes["backpackslots"].AsInt(0); } }
        public bool canBeWornBy(string pet)
        {
            return Attributes["validPets"].AsArray<string>(new string[0]).Contains(pet);
        }
    }
    public enum PetAccessoryType
    {
        NECK, HEAD, BACK, TAIL, PAWS
    }
}