using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace WolfTaming
{
    public class WolfTaming : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterEntityBehaviorClass("EntityBehaviorRaisable", typeof(EntityBehaviorRaisable));
            AiTaskRegistry.Register<AiTaskSimpleCommand>("simplecommand");
        }
    }
}
