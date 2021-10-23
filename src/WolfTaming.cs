using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace WolfTaming
{
    public class WolfTaming : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterEntityBehaviorClass("raisable", typeof(EntityBehaviorRaisable));
            api.RegisterEntityBehaviorClass("taskaiextended", typeof(EntityBehaviorTaskAIExtension));
            api.RegisterEntityBehaviorClass("tameable", typeof(EntityBehaviorTameable));
            AiTaskRegistry.Register<AiTaskSimpleCommand>("simplecommand");
        }
    }
}
