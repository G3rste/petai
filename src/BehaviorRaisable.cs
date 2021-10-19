using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace WolfTaming
{
    public class EntityBehaviorRaisable : EntityBehavior
    {
        public EntityBehaviorRaisable(Entity entity) : base(entity)
        {
        }

        public override void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
        {
            base.DidAttack(source, targetEntity, ref handled);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            return base.GetDrops(world, pos, byPlayer, ref handling);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void GetInfoText(StringBuilder infotext)
        {
            base.GetInfoText(infotext);
        }

        public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
        {
            return base.GetInteractionHelp(world, es, player, ref handled);
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
        }

        public override void Notify(string key, object data)
        {
            base.Notify(key, data);
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);
        }

        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            base.OnEntityDespawn(despawn);
        }

        public override void OnEntityLoaded()
        {
            base.OnEntityLoaded();
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, float damage)
        {
            base.OnEntityReceiveDamage(damageSource, damage);
        }

        public override void OnEntityReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10, float nutritionGainMultiplier = 1)
        {
            base.OnEntityReceiveSaturation(saturation, foodCat, saturationLossDelay, nutritionGainMultiplier);
        }

        public override void OnEntityRevive()
        {
            base.OnEntityRevive();
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
        }

        public override void OnFallToGround(Vec3d lastTerrainContact, double withYMotion)
        {
            base.OnFallToGround(lastTerrainContact, withYMotion);
        }

        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
            AiTaskIdle task = new AiTaskIdle(entity as EntityAgent);
            EntityPlayer player = byEntity as EntityPlayer;
            entity.Api.Logger.Debug("BehaviorRaisable Triggered");
            entity.WatchedAttributes.SetString("simplecommand", "sit");
        }

        public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
        {
            base.OnReceivedClientPacket(player, packetid, data, ref handled);
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
        {
            base.OnReceivedServerPacket(packetid, data, ref handled);
        }

        public override void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
        {
            base.OnReceivedServerPos(isTeleport, ref handled);
        }

        public override void OnStateChanged(EnumEntityState beforeState, ref EnumHandling handling)
        {
            base.OnStateChanged(beforeState, ref handling);
        }

        public override string PropertyName()
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}