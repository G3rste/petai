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

namespace WolfTaming
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