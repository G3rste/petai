using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskAvoidFire : AiTaskBase
    {
        private Vec3d target;
        private readonly float moveSpeed;
        private readonly float minDistance;
        private float lastCheck = 0;

        public AiTaskAvoidFire(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
        {
            moveSpeed = taskConfig["movespeed"]?.AsFloat(0.01f) ?? 0.01f;
            minDistance = taskConfig["mindistance"]?.AsFloat(5f) ?? 5f;
        }

        public override bool ShouldExecute()
        {
            var currentTime = entity.World.ElapsedMilliseconds;
            if (lastCheck + 2000 > currentTime)
            {
                return false;
            }
            lastCheck = currentTime;

            int[][] offsets = [[-1, -1], [-1, 0], [-1, 1], [0, -1], [0, 0], [0, 1], [1, -1], [1, 0], [1, 1]];
            var entityPos = entity.Pos.AsBlockPos;
            var blockAccessor = entity.World.BlockAccessor;
            var nearbyFires = offsets.Select(offset => blockAccessor.GetChunk(entityPos.X / GlobalConstants.ChunkSize + offset[0], entityPos.Y / GlobalConstants.ChunkSize, entityPos.Z / GlobalConstants.ChunkSize + offset[1]))
                .SelectMany(chunk => chunk?.BlockEntities ?? [])
                .Where(entry => entry.Value != null && entry.Key != null)
                .Where(entry => entry.Value.Block.HasBehavior<BlockBehaviorHeatSource>())
                .Where(entry => entry.Key.DistanceSqToNearerEdge(entityPos.X, entityPos.Y, entityPos.Z) < minDistance * minDistance)
                .Select(entry => entry.Key)
                .ToList();

            var nearestFire = nearbyFires.MinBy(firePos => firePos.DistanceSqToNearerEdge(entityPos.X, entityPos.Y, entityPos.Z));

            if (nearestFire != null)
            {
                var direction = entityPos.AddCopy(-nearestFire.X, -nearestFire.Y, -nearestFire.Z).AsVec3i;
                target = entityPos.Add(direction).Add(direction).ToVec3d();
                return true;
            }

            return false;
        }

        public override void StartExecute()
        {
            base.StartExecute();
            pathTraverser.WalkTowards(target, moveSpeed, 1f, () => { }, () => { });
        }

        public override bool ContinueExecute(float dt)
        {
            return pathTraverser.Active;
        }
    }
}