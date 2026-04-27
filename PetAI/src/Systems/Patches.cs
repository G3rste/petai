using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace PetAI
{
    public class MultiplyPatch
    {

        public static void Patch(Harmony harmony)
        {
            harmony.Patch(MethodInfo()
                , prefix: new HarmonyMethod(typeof(MultiplyPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
        }

        public static void Unpatch(Harmony harmony)
        {
            harmony.Unpatch(MethodInfo()
                , HarmonyPatchType.Prefix, "gerste.petai");
        }

        public static MethodInfo MethodInfo()
        {
            return typeof(EntityBehaviorMultiply).GetMethod("TryGetPregnant", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        public static bool Prefix(ref bool __result, EntityBehaviorMultiply __instance)
        {
            bool? multiplyAllowed = __instance.entity.GetBehavior<EntityBehaviorTameable>()?.MultiplyAllowed;
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

    public class MortallyWoundableOnEntityReceiveDamagePatch
    {

        public static void Patch(Harmony harmony)
        {
            harmony.Patch(MethodInfo()
                , prefix: new HarmonyMethod(typeof(MortallyWoundableOnEntityReceiveDamagePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
        }

        public static void Unpatch(Harmony harmony)
        {
            harmony.Unpatch(MethodInfo()
                , HarmonyPatchType.Prefix, "gerste.petai");
        }

        public static MethodInfo MethodInfo()
        {
            return typeof(EntityBehaviorMortallyWoundable).GetMethod("OnEntityReceiveDamage", BindingFlags.Instance | BindingFlags.Public);
        }
        public static bool Prefix(EntityBehaviorMortallyWoundable __instance, DamageSource damageSource, ref float damage)
        {
            var tameable = __instance.entity.GetBehavior<EntityBehaviorTameable>();
            if (tameable != null && string.IsNullOrEmpty(tameable.OwnerId))
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

    public class EntityBehaviorNameTagGetNamePatch
    {

        public static void Patch(Harmony harmony)
        {
            harmony.Patch(MethodInfo()
                , postfix: new HarmonyMethod(typeof(EntityBehaviorNameTagGetNamePatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public)));
        }

        public static void Unpatch(Harmony harmony)
        {
            harmony.Unpatch(MethodInfo()
                , HarmonyPatchType.Postfix, "gerste.petai");
        }

        public static MethodInfo MethodInfo()
        {
            return typeof(EntityBehaviorNameTag).GetMethod("GetName", BindingFlags.Instance | BindingFlags.Public);
        }
        public static void Postfix(ref string __result)
        {
            if (string.IsNullOrWhiteSpace(__result))
            {
                __result = null;
            }
        }
    }

    public class EntityBehaviorGrowBecomeAdultPatch
    {

        public static void Patch(Harmony harmony)
        {
            harmony.Patch(MethodInfo()
                , postfix: new HarmonyMethod(typeof(EntityBehaviorGrowBecomeAdultPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public)));
        }

        public static void Unpatch(Harmony harmony)
        {
            harmony.Unpatch(MethodInfo()
                , HarmonyPatchType.Postfix, "gerste.petai");
        }

        public static MethodInfo MethodInfo()
        {
            return typeof(EntityBehaviorGrow).GetMethod("BecomeAdult", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static void Postfix(EntityBehaviorGrow __instance, Entity adult) {
            Entity entity = __instance.entity;

            if (adult.HasBehavior<EntityBehaviorTameable>())
            {
                adult.GetBehavior<EntityBehaviorTameable>().DomesticationStatus = entity.GetBehavior<EntityBehaviorTameable>().DomesticationStatus;
            }
            adult.GetBehavior<EntityBehaviorNameTag>()?.SetName(entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName);
        }
    }

    public class AiTaskStayCloseToEntityOnNoPathPatch
    {

        public static void Patch(Harmony harmony)
        {
            harmony.Patch(MethodInfo()
                , postfix: new HarmonyMethod(typeof(AiTaskStayCloseToEntityOnNoPathPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public)));
        }

        public static void Unpatch(Harmony harmony)
        {
            harmony.Unpatch(MethodInfo()
                , HarmonyPatchType.Postfix, "gerste.petai");
        }

        public static MethodInfo MethodInfo()
        {
            return typeof(AiTaskStayCloseToEntity).GetMethod("OnNoPath", BindingFlags.Instance | BindingFlags.Public, []);
        }
        public static void Postfix(AiTaskStayCloseToEntity __instance)
        {
            __instance.OnNoPath(null);
        }
    }
}
