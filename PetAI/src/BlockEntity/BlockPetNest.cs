using System;
using Vintagestory.API.Common;

namespace PetAI
{
    public class BlockPetNest : Block
    {
        public EnumNestSize nestSize
        {
            get
            {
                EnumNestSize size = EnumNestSize.SMALL;
                Enum.TryParse<EnumNestSize>(Variant["size"].ToUpper(), out size);
                return size;
            }
        }
    }

    public enum EnumNestSize
    {
        SMALL, MEDIUM, LARGE
    }
}