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
            minduration = taskConfig["minduration"]?.AsInt(1000) ?? 1000;
            maxduration = taskConfig["maxduration"]?.AsInt(4000) ?? 4000;
        }
        public override bool ShouldExecute()
        {
            tameable ??= entity.GetBehavior<EntityBehaviorTameable>();
            targetEntity = tameable?.CachedOwner?.Entity;
            return cooldownUntilMs < entity.World.ElapsedMilliseconds
                && targetEntity != null
                && targetEntity.Pos.SquareDistanceTo(entity.Pos) <= 25
                && HasNiceThing(targetEntity);
        }
        public override void StartExecute()
        {
            base.StartExecute();

            durationUntilMs = entity.World.ElapsedMilliseconds + minduration + entity.World.Rand.Next(maxduration - minduration);
        }
        public override bool ContinueExecute(float dt)
        {
            Vec3f targetVec = new(
                (float)(targetEntity.Pos.X - entity.Pos.X),
                (float)(targetEntity.Pos.Y - entity.Pos.Y),
                (float)(targetEntity.Pos.Z - entity.Pos.Z)
            );

            float desiredYaw = (float)Math.Atan2(targetVec.X, targetVec.Z);

            float yawDist = GameMath.AngleRadDistance(entity.Pos.Yaw, desiredYaw);
            entity.Pos.Yaw += GameMath.Clamp(yawDist, -250 * dt, 250 * dt);
            entity.Pos.Yaw = entity.Pos.Yaw % GameMath.TWOPI;

            return durationUntilMs > entity.World.ElapsedMilliseconds
                && HasNiceThing(targetEntity)
                && base.ContinueExecute(dt);
        }

        private bool HasNiceThing(EntityPlayer owner)
        {
            var code = owner?.RightHandItemSlot?.Itemstack?.Item?.Code.Path;
            return tameable?.treatList?.Find(item => item.Name == code) != null || code == "dogtoy";
        }
    }
}
