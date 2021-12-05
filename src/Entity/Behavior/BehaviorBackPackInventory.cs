using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace PetAI
{
    public class EntityBehaviorBackPackInventory : EntityBehavior
    {
        public EntityBehaviorBackPackInventory(Entity entity) : base(entity)
        {
        }

        public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
        {
            if (entity.Alive && packetid < 1000 && (entity is EntityPet))
            {
                var inv = (entity as EntityPet)?.backpackInv as InventorySlotBound;
                inv.reloadFromSlots();
                inv.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                inv.saveAllSlots();
                handled = EnumHandling.PreventSubsequent;
                for (int i = 0; i < inv.Count; i++)
                {
                    if (inv[i].Empty) { continue; }

                    inv.MarkSlotDirty(i);
                }
            }
        }
        public override string PropertyName()
        {
            return "backpackinventory";
        }
    }
}