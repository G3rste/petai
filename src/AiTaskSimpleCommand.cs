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

        public override bool ContinueExecute(float dt)
        {
            return base.ContinueExecute(dt);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override void FinishExecute(bool cancelled)
        {
            base.FinishExecute(cancelled);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);
        }

        public override bool Notify(string key, object data)
        {
            return base.Notify(key, data);
        }

        public override void OnEntityDespawn(EntityDespawnReason reason)
        {
            base.OnEntityDespawn(reason);
        }

        public override void OnEntityHurt(DamageSource source, float damage)
        {
            base.OnEntityHurt(source, damage);
        }

        public override void OnEntityLoaded()
        {
            base.OnEntityLoaded();
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
        }

        public override void OnStateChanged(EnumEntityState beforeState)
        {
            base.OnStateChanged(beforeState);
        }

        public override bool ShouldExecute()
        {
            bool execute = entity.WatchedAttributes.GetString("simplecommand") == "sit";
            if (execute) entity.WatchedAttributes.RemoveAttribute("simplecommand");
            return execute;
        }

        public override void StartExecute()
        {
            base.StartExecute();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}