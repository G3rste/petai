using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PetAI
{
    public class AiTaskStay : AiTaskBase
    {

        double? x;
        double? y;
        double? z;
        float moveSpeed = 0.01f;
        float range = 40f;
        float maxDistance = 10f;
        bool stuck = false;

        string commandName = "stay";
        public AiTaskStay(EntityAgent entity) : base(entity)
        {
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);

            if (taskConfig["movespeed"] != null)
            {
                moveSpeed = taskConfig["movespeed"].AsFloat(0.01f);
            }

            if (taskConfig["searchrange"] != null)
            {
                range = taskConfig["searchrange"].AsFloat(40f);
            }

            if (taskConfig["maxdistance"] != null)
            {
                maxDistance = taskConfig["maxdistance"].AsFloat(10f);
            }

            if (taskConfig["command"] != null)
            {
                commandName = taskConfig["command"].AsString("stay");
            }
        }

        public override bool ShouldExecute()
        {
            if (entity?.GetBehavior<EntityBehaviorReceiveCommand>()?.complexCommand != commandName)
            {
                x = null;
                y = null;
                z = null;
                return false;
            }
            if (x == null || y == null || z == null)
            {
                ITreeAttribute home = entity?.WatchedAttributes?.GetTreeAttribute("staylocation");
                x = home?.TryGetDouble("x");
                y = home?.TryGetDouble("y");
                z = home?.TryGetDouble("z");
                return false;
            }
            return entity.GetBehavior<EntityBehaviorReceiveCommand>().complexCommand == commandName &&
                entity.ServerPos.SquareDistanceTo((float)x, (float)y, (float)z) > maxDistance * maxDistance;
        }
        public override void StartExecute()
        {
            base.StartExecute();

            if (x != null && y != null && z != null)
            {
                pathTraverser.NavigateTo(new Vec3d((double)x, (double)y, (double)z), moveSpeed, 1f, OnGoalReached, OnStuck, false, 1000, true);
            }
            stuck = false;
        }

        public override bool ContinueExecute(float dt)
        {
            if (x == null || y == null || z == null)
            {
                return false;
            }

            if (entity.ServerPos.SquareDistanceTo((double)x, (double)y, (double)z) < maxDistance * maxDistance / 4)
            {
                pathTraverser.Stop();
                return false;
            }

            return !stuck && pathTraverser.Active;
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