using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskFollowMaster : AiTaskStayCloseToGuardedEntity 
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
    }
}