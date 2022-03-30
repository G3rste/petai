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

        private BlockPos entityNestPos
        {
            get { return entity.WatchedAttributes.GetBlockPos("entityNest"); }
            set
            {
                if (value != null) { entity.WatchedAttributes.SetBlockPos("entityNest", value); }
            }
        }

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
            if (entityNestPos == null)
            {
                entityNestPos = entity.Api.ModLoader.GetModSystem<POIRegistry>().GetNearestPoi(entity.ServerPos.XYZ, range, isValidNest)?.Position?.AsBlockPos;
            }
            else if (nest == null)
            {
                nest = entity.Api.ModLoader.GetModSystem<POIRegistry>().GetNearestPoi(entityNestPos.ToVec3d(), 1, poi => poi is BlockEntityPetNest) as BlockEntityPetNest;
                nest.petId = entity.EntityId;
            }
            return nest != null && entity.ServerPos.SquareDistanceTo(nest.Pos.ToVec3d()) > 2;
        }

        public override void StartExecute()
        {
            base.StartExecute();

            pathTraverser.NavigateTo(nest.MiddlePostion, moveSpeed, () => { }, () => stuck = true, false);

            stuck = false;
        }

        public override bool ContinueExecute(float dt)
        {
            return !stuck && pathTraverser.Active;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        private bool isValidNest(IPointOfInterest poi)
        {
            var nest = poi as BlockEntityPetNest;
            return nest != null && nest.petId == null;
        }

        bool nestBlockReached()
        {
            return entity.ServerPos.SquareDistanceTo(entityNestPos.ToVec3d()) < 2;
        }
    }
}