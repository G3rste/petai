using Vintagestory.API.Common;

namespace WolfTaming
{
    public class WolfTaming : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterEntityBehaviorClass("EntityBehaviorRaisable", typeof(EntityBehaviorRaisable));
        }
    }
}
