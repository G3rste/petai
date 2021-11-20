using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace PetAI
{
    public class AiTaskFollowMaster : AiTaskBase
    {

        Entity targetEntity;
        float moveSpeed = 0.03f;
        float range = 20f;
        float maxDistance = 5f;
        bool stuck = false;

        string commandName = "followmaster";
        public AiTaskFollowMaster(EntityAgent entity) : base(entity)
        {
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);

            if (taskConfig["movespeed"] != null)
            {
                moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
            }

            if (taskConfig["searchrange"] != null)
            {
                range = taskConfig["searchrange"].AsFloat(20f);
            }

            if (taskConfig["maxdistance"] != null)
            {
                maxDistance = taskConfig["maxdistance"].AsFloat(5f);
            }

            if (taskConfig["command"] != null)
            {
                commandName = taskConfig["command"].AsString("followmaster");
            }
        }

        public override bool ShouldExecute()
        {
            if (targetEntity == null || !targetEntity.Alive || targetEntity.ShouldDespawn)
            {
                targetEntity = entity?.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity;
                return false;
            }
            return entity.GetBehavior<EntityBehaviorReceiveCommand>().complexCommand == commandName
                && targetEntity.ServerPos.SquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z) > maxDistance * maxDistance;
        }
        public override void StartExecute()
        {
            base.StartExecute();

            float size = targetEntity.CollisionBox.XSize;
            pathTraverser.NavigateTo(targetEntity.ServerPos.XYZ, moveSpeed, size + 0.2f, OnGoalReached, OnStuck, false, 1000, true);

            stuck = false;
        }

        public override bool ContinueExecute(float dt)
        {

            double x = targetEntity.ServerPos.X;
            double y = targetEntity.ServerPos.Y;
            double z = targetEntity.ServerPos.Z;

            pathTraverser.CurrentTarget.X = x;
            pathTraverser.CurrentTarget.Y = y;
            pathTraverser.CurrentTarget.Z = z;

            if (entity.ServerPos.SquareDistanceTo(x, y, z) < maxDistance * maxDistance / 4)
            {
                pathTraverser.Stop();
                return false;
            }

            return targetEntity.Alive && !stuck && pathTraverser.Active;
        }

        private void OnStuck()
        {
            stuck = true;
        }

        private void OnGoalReached()
        {
        }
    }
}