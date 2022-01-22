using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PetAI
{
    public class AiTaskBeAMount : AiTaskBase
    {
        new EntityMount entity;
        private float moveSpeed;

        public AiTaskBeAMount(EntityAgent entity) : base(entity)
        {
            this.entity = entity as EntityMount;
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);

            if (taskConfig["movespeed"] != null)
            {
                moveSpeed = taskConfig["movespeed"].AsFloat(0.06f);
            }
        }

        public override bool ShouldExecute()
        {
            return entity?.rider != null;
        }

        public override bool ContinueExecute(float dt)
        {
            /*if (entity.rider != null && entity.rider is EntityAgent)
            {
                float desiredYaw = entity.rider.ServerPos.Yaw + 1.5708f;

                float yawDist = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);
                entity.ServerPos.Yaw += GameMath.Clamp(yawDist, -1440 * dt, 1440 * dt);
                entity.ServerPos.Yaw = entity.ServerPos.Yaw % GameMath.TWOPI;

                handleInput(dt);
            }*/
            return entity?.rider != null && entity.rider is EntityAgent;
        }

        public override void FinishExecute(bool cancelled)
        {
            if (entity.rider != null)
            {
                entity.TryUnmount();
            }
            base.FinishExecute(cancelled);
        }

        private void handleInput(float dt)
        {
            if (entity.direction == EnumMountMovementDirection.Forwards)
            {
                float factor = entity.isSprinting ? 0.06f : 0.02f;
                double cosYaw = Math.Cos(entity.ServerPos.Yaw);
                double sinYaw = Math.Sin(entity.ServerPos.Yaw);
                entity.Controls.WalkVector.Set(sinYaw, 0, cosYaw);
                entity.Controls.WalkVector.Mul(factor * GlobalConstants.OverallSpeedMultiplier);
            }
            else if (entity.direction == EnumMountMovementDirection.Backwards)
            {
                double cosYaw = Math.Cos(entity.ServerPos.Yaw);
                double sinYaw = Math.Sin(entity.ServerPos.Yaw);
                entity.Controls.WalkVector.Set(-sinYaw, 0, -cosYaw);
                entity.Controls.WalkVector.Mul(0.01 * GlobalConstants.OverallSpeedMultiplier);
            }
            else
            {
                entity.Controls.WalkVector.Set(0, 0, 0);
            }
        }
    }
}