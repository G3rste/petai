using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PetAI
{
    public class BlockPetNest : Block
    {
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
}