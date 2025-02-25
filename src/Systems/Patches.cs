using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace PetAI
{
    public class MultiplyPatch
    {

        public static void Patch(Harmony harmony)
        {
            harmony.Patch(methodInfo()
                , prefix: new HarmonyMethod(typeof(MultiplyPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
        }

        public static void Unpatch(Harmony harmony)
        {
            harmony.Unpatch(methodInfo()
                , HarmonyPatchType.Prefix, "gerste.petai");
        }

        public static MethodInfo methodInfo()
        {
            return typeof(EntityBehaviorMultiply).GetMethod("TryGetPregnant", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        public static bool Prefix(ref bool __result, EntityBehaviorMultiply __instance)
        {
            bool? multiplyAllowed = __instance.entity.GetBehavior<EntityBehaviorTameable>()?.multiplyAllowed;
            if (multiplyAllowed == null || multiplyAllowed == true)
            {
                return true;
            }
            else
            {
                __result = false;
                return false;
            }
        }
    }

    // Currently an entity can only be mortally woundable if its also a mount. This Patch fixes that.
    public class MortallyWoundableAfterInitializedPatch
    {

        public static void Patch(Harmony harmony)
        {
            harmony.Patch(methodInfo()
                , prefix: new HarmonyMethod(typeof(MortallyWoundableAfterInitializedPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
        }

        public static void Unpatch(Harmony harmony)
        {
            harmony.Unpatch(methodInfo()
                , HarmonyPatchType.Prefix, "gerste.petai");
        }

        public static MethodInfo methodInfo()
        {
            return typeof(EntityBehaviorMortallyWoundable).GetMethod("AfterInitialized", BindingFlags.Instance | BindingFlags.Public);
        }
        public static bool Prefix(EntityBehaviorMortallyWoundable __instance)
        {
            if (__instance.entity.World.Side == EnumAppSide.Server)
            {
                typeof(EntityBehaviorMortallyWoundable).GetField("ebh", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, __instance.entity.GetBehavior<EntityBehaviorHealth>());

                EntityBehaviorTaskAI taskAi = __instance.entity.GetBehavior<EntityBehaviorTaskAI>();

                taskAi.TaskManager.OnShouldExecuteTask += (t) => __instance.HealthState != EnumEntityHealthState.MortallyWounded && __instance.HealthState != EnumEntityHealthState.Recovering;

                if (__instance.HealthState == EnumEntityHealthState.MortallyWounded)
                {
                    typeof(EntityBehaviorMortallyWoundable).GetMethod("setMortallyWounded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, null);
                }
            }

            if (__instance.entity.HasBehavior<EntityBehaviorSeatable>())
            {
                __instance.entity.GetBehavior<EntityBehaviorSeatable>().CanSit += typeof(EntityBehaviorMortallyWoundable).GetMethod("EntityBehaviorMortallyWoundable_CanSit", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate<CanSitDelegate>(__instance);
            }
            return false;
        }
    }

    public class MortallyWoundableOnEntityReceiveDamagePatch
    {

        public static void Patch(Harmony harmony)
        {
            harmony.Patch(methodInfo()
                , prefix: new HarmonyMethod(typeof(MortallyWoundableOnEntityReceiveDamagePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
        }

        public static void Unpatch(Harmony harmony)
        {
            harmony.Unpatch(methodInfo()
                , HarmonyPatchType.Prefix, "gerste.petai");
        }

        public static MethodInfo methodInfo()
        {
            return typeof(EntityBehaviorMortallyWoundable).GetMethod("OnEntityReceiveDamage", BindingFlags.Instance | BindingFlags.Public);
        }
        public static bool Prefix(EntityBehaviorMortallyWoundable __instance, DamageSource damageSource, ref float damage)
        {
            var tameable = __instance.entity.GetBehavior<EntityBehaviorTameable>();
            if (tameable != null && string.IsNullOrEmpty(tameable.ownerId))
            {
                return false;
            }
            if (tameable == null
                || __instance.HealthState == EnumEntityHealthState.Normal
                || damageSource.Type == EnumDamageType.Heal)
            {
                return true;
            }
            damage = 0;
            return false;
        }
    }
}