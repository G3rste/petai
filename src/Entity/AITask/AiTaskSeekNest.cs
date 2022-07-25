using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskSeekNest : AiTaskBase
    {
        private BlockEntityPetNest nest { get; set; }

        private List<DayTimeFrame> duringDayTimeFrames = new List<DayTimeFrame>();

        int range = 15;

        long lastCheck;

        bool stuck = false;

        float moveSpeed = 0.02f;
        public AiTaskSeekNest(EntityAgent entity) : base(entity)
        {
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);
            if (taskConfig["duringDayTimeFrames"] != null)
            {
                duringDayTimeFrames.AddRange(taskConfig["duringDayTimeFrames"].AsObject<DayTimeFrame[]>(new DayTimeFrame[0]));
            }
            range = taskConfig["horRange"].AsInt(15);
            moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);
        }

        public override bool ShouldExecute()
        {
            if (lastCheck + 10000 > entity.World.ElapsedMilliseconds) return false;
            lastCheck = entity.World.ElapsedMilliseconds;
            if (duringDayTimeFrames.Count > 0)
            {
                double hourOfDay = entity.World.Calendar.HourOfDay / entity.World.Calendar.HoursPerDay * 24f + (entity.World.Rand.NextDouble() * 0.3f - 0.15f);
                if (!duringDayTimeFrames.Exists(frame => frame.Matches(hourOfDay))) return false;
            }
            if (nest == null || entity.ServerPos.SquareDistanceTo(nest.Position) > 50)
            {
                nest = entity.Api.ModLoader.GetModSystem<POIRegistry>().GetNearestPoi(entity.ServerPos.XYZ, range, isValidNonOccupiedNest) as BlockEntityPetNest;
            }
            return nest != null && entity.ServerPos.SquareDistanceTo(nest.Pos.ToVec3d()) > 2;
        }

        private bool isValidNonOccupiedNest(IPointOfInterest poi)
        {
            if (poi is BlockEntityPetNest nest)
            {
                if ((nest.Block as BlockPetNest).nestSize < entity.GetBehavior<EntityBehaviorTameable>().size) { return false; }
                if (entity.World.GetEntitiesAround(nest.Position, 3, 3, occupier => occupier.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.GetTask<AiTaskSeekNest>()?.nest == nest).Length == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override void StartExecute()
        {
            base.StartExecute();

            stuck = false;

            pathTraverser.NavigateTo_Async(nest.MiddlePostion, moveSpeed, 1f, () => { }, () => stuck = true);
        }

        public override bool ContinueExecute(float dt)
        {
            return !stuck && pathTraverser.Active;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}