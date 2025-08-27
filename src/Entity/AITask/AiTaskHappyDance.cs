using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PetAI
{

    public class AiTaskHappyDance : AiTaskBase
    {
        protected EntityPlayer targetEntity;
        protected EntityBehaviorTameable tameable;

        protected int minduration;
        protected int maxduration;
        protected long durationUntilMs;
        public AiTaskHappyDance(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
        {
            minduration = (int)taskConfig["minduration"]?.AsInt(1000);
            maxduration = (int)taskConfig["maxduration"]?.AsInt(4000);
        }
        public override bool ShouldExecute()
        {
            if (tameable == null)
            {
                tameable = entity.GetBehavior<EntityBehaviorTameable>();
            }
            targetEntity = tameable?.cachedOwner?.Entity;
            return cooldownUntilMs < entity.World.ElapsedMilliseconds
                && targetEntity != null
                && targetEntity.ServerPos.SquareDistanceTo(entity.ServerPos) <= 25
                && hasNiceThing(targetEntity);
        }
        public override void StartExecute()
        {
            base.StartExecute();

            durationUntilMs = entity.World.ElapsedMilliseconds + minduration + entity.World.Rand.Next(maxduration - minduration);
        }
        public override bool ContinueExecute(float dt)
        {
            Vec3f targetVec = new Vec3f(
                (float)(targetEntity.ServerPos.X - entity.ServerPos.X),
                (float)(targetEntity.ServerPos.Y - entity.ServerPos.Y),
                (float)(targetEntity.ServerPos.Z - entity.ServerPos.Z)
            );

            float desiredYaw = (float)Math.Atan2(targetVec.X, targetVec.Z);

            float yawDist = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);
            entity.ServerPos.Yaw += GameMath.Clamp(yawDist, -250 * dt, 250 * dt);
            entity.ServerPos.Yaw = entity.ServerPos.Yaw % GameMath.TWOPI;

            return durationUntilMs > entity.World.ElapsedMilliseconds
                && hasNiceThing(targetEntity)
                && base.ContinueExecute(dt);
        }

        private bool hasNiceThing(EntityPlayer owner)
        {
            var code = owner?.RightHandItemSlot?.Itemstack?.Item?.Code.Path;
            return tameable?.treatList?.Find(item => item.name == code) != null || code == "dogtoy";
        }
    }
}