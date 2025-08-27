using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskTrick : AiTaskIdle
    {
        public AiTaskTrick(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
        {
            commandName = taskConfig["command"].AsString();
        }

        public override int Slot => base.Slot;

        public override float Priority => base.Priority;

        public override float PriorityForCancel => base.PriorityForCancel;

        public string commandName;
        
        public override bool ShouldExecute()
        {
            return entity.GetBehavior<EntityBehaviorReceiveCommand>()?.simpleCommand == commandName; ;
        }

        public override void StartExecute() {
            entity.GetBehavior<EntityBehaviorReceiveCommand>()?.setCommand(new Command(EnumCommandType.SIMPLE, null), null);
            base.StartExecute();
        }
    }
}