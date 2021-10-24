using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace WolfTaming
{
    public class AiTaskTrick : AiTaskIdle
    {
        public AiTaskTrick(EntityAgent entity) : base(entity)
        {
        }

        public override int Slot => base.Slot;

        public override float Priority => base.Priority;

        public override float PriorityForCancel => base.PriorityForCancel;

        public string commandName;

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);
            commandName = taskConfig["command"].AsString();
        }
        public override bool ShouldExecute()
        {
            return entity.GetBehavior<EntityBehaviorReceiveCommand>()?.simpleCommand == commandName;;
        }

        public override void StartExecute() {
            entity.GetBehavior<EntityBehaviorReceiveCommand>()?.setCommand(new Command(CommandType.SIMPLE, null), null);
            base.StartExecute();
        }
    }
}