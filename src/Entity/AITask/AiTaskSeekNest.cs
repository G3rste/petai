using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace WolfTaming
{
    public class AiTaskSeekNest : AiTaskBase
    {

        private BlockPos entityNest
        {
            get
            {
                ITreeAttribute attribute = entity.WatchedAttributes.GetTreeAttribute("entityNest");
                if (attribute != null)
                {
                    int x = attribute.GetInt("x");
                    int y = attribute.GetInt("y");
                    int z = attribute.GetInt("z");

                    return new BlockPos(x, y, z);
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    entity.WatchedAttributes.RemoveAttribute("entityNest");
                }
                else
                {
                    ITreeAttribute attribute = new TreeAttribute();

                    attribute.SetInt("x", value.X);
                    attribute.SetInt("y", value.Y);
                    attribute.SetInt("z", value.Z);

                    entity.WatchedAttributes.SetAttribute("entityNest", attribute);
                }
            }
        }

        private List<string> nestList = new List<string>();

        private List<DayTimeFrame> duringDayTimeFrames = new List<DayTimeFrame>();

        int horRange = 15;
        int vertRange = 4;

        int maxDistance = 40;

        long lastSearch;

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
            if (taskConfig["validNests"] != null)
            {
                nestList.AddRange(taskConfig["validNests"].AsArray<string>(new string[0]));
            }
            horRange = taskConfig["horRange"].AsInt(15);
            vertRange = taskConfig["vertRange"].AsInt(4);
            maxDistance = taskConfig["maxDistance"].AsInt(40);
            moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);
        }

        public override bool ShouldExecute()
        {
            if (entityNest == null && lastSearch + 10000 > entity.World.ElapsedMilliseconds) return false;
            if (duringDayTimeFrames.Count > 0)
            {
                double hourOfDay = entity.World.Calendar.HourOfDay / entity.World.Calendar.HoursPerDay * 24f + (entity.World.Rand.NextDouble() * 0.3f - 0.15f);
                if (!duringDayTimeFrames.Exists(frame => frame.Matches(hourOfDay))) return false;
            }
            if (entityNest == null
                || entity.ServerPos.SquareDistanceTo(entityNest.X, entityNest.Y, entityNest.Z) > maxDistance * maxDistance
                || !isNestBlock(entity.World.BlockAccessor.GetBlock(entityNest), entityNest))
            {
                entityNest = null;
                tryFindEntityNest();
                lastSearch = entity.World.ElapsedMilliseconds;
                return false;
            }
            return !nestBlockReached();
        }

        public override void StartExecute()
        {
            base.StartExecute();

            pathTraverser.NavigateTo(entityNest.Copy().Up().ToVec3d().Add(0.5, 0, 0.5), moveSpeed, () => { }, () => stuck = true, false);

            stuck = false;
        }

        public override bool ContinueExecute(float dt)
        {
            if (nestBlockReached())
            {
                pathTraverser.Stop();
                return false;
            }
            return !stuck && pathTraverser.Active;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        void tryFindEntityNest()
        {
            entity.Api.World.BlockAccessor.SearchBlocks(
                entity.ServerPos.AsBlockPos.Add(-horRange, -vertRange, -horRange),
                entity.ServerPos.AsBlockPos.Add(horRange, vertRange, horRange),
                (block, pos) =>
                    {
                        if (isNestBlock(block, pos))
                        {
                            entityNest = pos.Copy();
                            return false;
                        }
                        return true;
                    });
        }

        bool isNestBlock(Block block, BlockPos pos)
        {
            return nestList.Exists(nest =>
            {
                if (nest.EndsWith("*"))
                {
                    if (block.Code.Path.StartsWith(nest.Remove(nest.Length - 1))) return true;
                    Block decor = entity.World.BlockAccessor.GetDecor(pos, BlockFacing.indexUP);
                    if (decor == null) return false;
                    return decor.Code.Path.StartsWith(nest.Remove(nest.Length - 1));
                }
                else
                {
                    if (block.Code.Path == nest) return true;
                    Block decor = entity.World.BlockAccessor.GetDecor(pos, BlockFacing.indexUP);
                    if (decor == null) return false;
                    return decor.Code.Path == nest;
                }
            });
        }

        bool nestBlockReached()
        {
            int x = (int)entity.ServerPos.X;
            int y = (int)entity.ServerPos.Y;
            int z = (int)entity.ServerPos.Z;

            return isNestBlock(entity.World.BlockAccessor.GetBlock(x, y - 1, z), new BlockPos(x, y - 1, z))
                || isNestBlock(entity.World.BlockAccessor.GetBlock(x, y, z), new BlockPos(x, y, z));
        }
    }
}