using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

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
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var sapi = (world.Api as ICoreServerAPI);
            if (sapi != null)
            {
                var message = new PetNestMessage();
                message.availablePets = sapi.ModLoader.GetModSystem<PetManager>().GetPetsForPlayer(byPlayer.PlayerUID);
                message.selectedNest = blockSel.Position;
                sapi.Network.GetChannel("petainetwork").SendPacket<PetNestMessage>(message, byPlayer as IServerPlayer);
            }
            return true;
        }
    }

    public enum EnumNestSize
    {
        SMALL, MEDIUM, LARGE
    }
}