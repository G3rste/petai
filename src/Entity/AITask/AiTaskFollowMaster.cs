using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskFollowMaster : AiTaskStayCloseToEntity
    {
        string commandName = "followmaster";
        public AiTaskFollowMaster(EntityAgent entity) : base(entity)
        {
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);

            if (taskConfig["command"] != null)
            {
                commandName = taskConfig["command"].AsString("followmaster");
            }
            allowTeleport &= PetConfig.Current.AllowTeleport;
        }

        public override bool ShouldExecute()
        {
            if (targetEntity == null || !targetEntity.Alive || targetEntity.ShouldDespawn || !targetEntity.IsInteractable)
            {
                targetEntity = entity?.GetBehavior<EntityBehaviorTameable>()?.cachedOwner?.Entity;
                return false;
            }
            return entity.GetBehavior<EntityBehaviorReceiveCommand>().complexCommand == commandName
                && targetEntity.ServerPos.SquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z) > maxDistance * maxDistance;
        }

        //overridden base method to avoid constant teleporting when stuck
        public override void StartExecute()
        {
            base.StartExecute();

            float size = targetEntity.SelectionBox.XSize;

            pathTraverser.NavigateTo_Async(targetEntity.ServerPos.XYZ, moveSpeed, size + 0.2f, OnGoalReached, () => stuck = true, null, 1000, 1);

            targetOffset.Set(entity.World.Rand.NextDouble() * 2 - 1, 0, entity.World.Rand.NextDouble() * 2 - 1);

            stuck = false;

            double x = targetEntity.ServerPos.X + targetOffset.X;
            double y = targetEntity.ServerPos.Y;
            double z = targetEntity.ServerPos.Z + targetOffset.Z;

            float dist = entity.ServerPos.SquareDistanceTo(x, y, z);

            if (allowTeleport && dist > teleportAfterRange * teleportAfterRange)
            {
                tryTeleport();
            }
        }

        public override bool ContinueExecute(float dt)
        {
            double x = targetEntity.ServerPos.X + targetOffset.X;
            double y = targetEntity.ServerPos.Y;
            double z = targetEntity.ServerPos.Z + targetOffset.Z;

            pathTraverser.CurrentTarget.X = x;
            pathTraverser.CurrentTarget.Y = y;
            pathTraverser.CurrentTarget.Z = z;

            float dist = entity.ServerPos.SquareDistanceTo(x, y, z);

            if (dist < 3 * 3)
            {
                pathTraverser.Stop();
                return false;
            }

            if (allowTeleport && dist > teleportAfterRange * teleportAfterRange)
            {
                tryTeleport();
            }

            return !stuck && pathTraverser.Active;
        }
        public override void OnNoPath(Vec3d target)
        {
            // ignore
        }
    }
}