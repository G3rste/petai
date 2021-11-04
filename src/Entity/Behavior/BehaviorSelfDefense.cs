using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace WolfTaming
{
    public class EntityBehaviorSelfDefense : EntityBehavior
    {
        public Entity attacker { get; set; }
        public EntityBehaviorSelfDefense(Entity entity) : base(entity)
        {
        }
        public override void OnEntityReceiveDamage(DamageSource damageSource, float damage)
        {
            base.OnEntityReceiveDamage(damageSource, damage);
            IPlayer player = (damageSource.SourceEntity as EntityPlayer)?.Player;
            if (player == null
                || (player.WorldData.CurrentGameMode != EnumGameMode.Creative
                    && player.WorldData.CurrentGameMode != EnumGameMode.Spectator
                    && (player as IServerPlayer).ConnectionState == EnumClientState.Playing))
            {
                if (attacker == null || !attacker.Alive || attacker.ServerPos.SquareDistanceTo(entity.ServerPos.XYZ) > 25 && damage > 0f)
                {
                    attacker = damageSource.SourceEntity;
                }
            }
        }

        public override string PropertyName()
        {
            return "selfdefense";
        }
    }
}