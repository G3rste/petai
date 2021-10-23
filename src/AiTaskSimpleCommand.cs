using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace WolfTaming
{
    public class AiTaskSimpleCommand : AiTaskIdle
    {
        public AiTaskSimpleCommand(EntityAgent entity) : base(entity)
        {
        }

        public override int Slot => base.Slot;

        public override float Priority => base.Priority;

        public override float PriorityForCancel => base.PriorityForCancel;

        public string commandName;

        public static readonly string commandKey = "simpleCommand";

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);
            commandName = taskConfig["command"].AsString();
        }
        public override bool ShouldExecute()
        {
            bool execute = entity.WatchedAttributes.GetString(commandKey) == commandName;
            if (execute) entity.WatchedAttributes.RemoveAttribute(commandKey);
            return execute;
        }
    }
}