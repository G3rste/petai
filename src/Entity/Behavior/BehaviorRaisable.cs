using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PetAI
{
    // Now obsolete
    public class EntityBehaviorRaisable : EntityBehaviorGrow
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
