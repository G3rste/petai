using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PetAI
{
    public class AiTaskBeAMount : AiTaskBase
    {
        EntityMount mount;
        private float moveSpeed;

        public AiTaskBeAMount(EntityAgent entity) : base(entity)
        {
            this.mount = entity as EntityMount;
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
            return mount?.rider != null;
        }

        public override bool ContinueExecute(float dt)
        {
            if (mount.rider != null && mount.rider is EntityAgent)
            {
                float desiredYaw = mount.rider.SidedPos.Yaw + 1.5708f;

                float yawDist = GameMath.AngleRadDistance(mount.SidedPos.Yaw, desiredYaw);
                if (yawDist < -0.0872665f || yawDist > 0.0872665f)
                {
                    mount.SidedPos.Yaw += GameMath.Clamp(yawDist, -1440 * dt, 1440 * dt);
                    mount.SidedPos.Yaw = mount.SidedPos.Yaw % GameMath.TWOPI;
                }

                if (mount.Controls.Forward)
                {
                    float factor = mount.Controls.Sprint ? mount.mountRunningSpeed : mount.mountWalkingSpeed;
                    double cosYaw = Math.Cos(mount.SidedPos.Yaw);
                    double sinYaw = Math.Sin(mount.SidedPos.Yaw);
                    mount.Controls.WalkVector.Set(sinYaw, 0, cosYaw);
                    mount.Controls.WalkVector.Mul(factor * GlobalConstants.OverallSpeedMultiplier);
                }
                else if (mount.Controls.Backward)
                {
                    double cosYaw = Math.Cos(mount.SidedPos.Yaw);
                    double sinYaw = Math.Sin(mount.SidedPos.Yaw);
                    mount.Controls.WalkVector.Set(-sinYaw, 0, -cosYaw);
                    mount.Controls.WalkVector.Mul(0.01 * GlobalConstants.OverallSpeedMultiplier);
                }
                else
                {
                    mount.Controls.WalkVector.Set(0, 0, 0);
                }
            }
            return mount?.rider != null && mount.rider is EntityAgent;
        }

        public override void FinishExecute(bool cancelled)
        {
            if (mount.rider != null)
            {
                mount.TryUnmount();
            }
            base.FinishExecute(cancelled);
        }

        /*private void handleInput(float dt)
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
        }*/
    }
}