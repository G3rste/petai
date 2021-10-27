using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace WolfTaming
{
    public class EntityBehaviorRaisable : EntityBehavior
    {
        public EntityBehaviorRaisable(Entity entity) : base(entity)
        {
        }
        public override string PropertyName()
        {
            return "raisable";
        }
    }
}