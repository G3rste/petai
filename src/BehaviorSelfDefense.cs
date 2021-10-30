using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;

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
            if(attacker == null || attacker.ServerPos.SquareDistanceTo(entity.ServerPos.XYZ) > 25 && damage > 0f){
                attacker = damageSource.SourceEntity;
            }
        }
        public override void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
        {
            base.DidAttack(source, targetEntity, ref handled);
        }

        public override string PropertyName()
        {
            return "selfdefense";
        }
    }
}