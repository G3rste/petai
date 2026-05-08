using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskFollowMaster : AiTaskStayCloseToEntity
    {
        readonly string commandName = "followmaster";
        EntityBehaviorTameable tameable;
        public AiTaskFollowMaster(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
        {
            if (taskConfig["command"] != null)
            {
                commandName = taskConfig["command"].AsString("followmaster");
            }
            allowTeleport &= PetConfig.Current.AllowTeleport;
            moveSpeed *= PetConfig.Current.Difficulty.petSpeedMultiplier;
            animMeta.AnimationSpeed *= PetConfig.Current.Difficulty.petSpeedMultiplier;
        }

        public override bool ShouldExecute()
        {
            if (!PreconditionsSatisfied()) return false;
            targetEntity = tameable?.CachedOwner?.Entity;
            tameable ??= entity.GetBehavior<EntityBehaviorTameable>();
            if (targetEntity == null || !targetEntity.Alive || targetEntity.ShouldDespawn || !targetEntity.IsInteractable)
            {
                return false;
            }
            return entity.GetBehavior<EntityBehaviorReceiveCommand>().ComplexCommand == commandName
                && targetEntity.Pos.SquareDistanceTo(entity.Pos.X, entity.Pos.Y, entity.Pos.Z) > maxDistance * maxDistance;
        }

        public override void OnNoPath(Vec3d target)
        {
            if (allowTeleport && targetEntity.Pos.SquareDistanceTo(entity.Pos) > teleportAfterRange * teleportAfterRange)
            {
                tryTeleport();
            }
            else
            {
                stuck = false;
                pathTraverser.WalkTowards(targetEntity.Pos.XYZ, moveSpeed, maxDistance, OnGoalReached, OnStuck);
            }
        }
    }
}
