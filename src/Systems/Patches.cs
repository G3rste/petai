using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
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
    public class PoulticePatch
    {

        public static void Patch(Harmony harmony)
        {
            harmony.Patch(methodInfo()
                , prefix: new HarmonyMethod(typeof(PoulticePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
        }

        public static void Unpatch(Harmony harmony)
        {
            harmony.Unpatch(methodInfo()
                , HarmonyPatchType.Prefix, "gerste.petai");
        }

        public static MethodInfo methodInfo()
        {
            return typeof(ItemPoultice).GetMethod("OnHeldInteractStop", BindingFlags.Instance | BindingFlags.Public);
        }
        public static bool Prefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity, EntitySelection entitySel)
        {
            if (entitySel?.Entity?.HasBehavior<EntityBehaviorTameable>() != true && entitySel?.Entity?.Properties?.Attributes?["poulticeRevive"]?.AsBool(false) != true)
            {
                return true;
            }
            if (secondsUsed > 0.7f && byEntity.World.Side == EnumAppSide.Server)
            {
                if (entitySel.Entity.Alive)
                {
                    JsonObject attr = slot.Itemstack.Collectible.Attributes;
                    float health = attr["health"].AsFloat();
                    entitySel.Entity.ReceiveDamage(new DamageSource()
                    {
                        Source = EnumDamageSource.Internal,
                        Type = health > 0 ? EnumDamageType.Heal : EnumDamageType.Poison
                    }, Math.Abs(health));

                    slot.TakeOut(1);
                    slot.MarkDirty();
                }
                else
                {
                    EnumHandling handled = EnumHandling.PassThrough;
                    entitySel.Entity.GetBehavior<EntityBehaviorTameable>()?.OnInteract(byEntity, slot, entitySel.HitPosition, EnumInteractMode.Interact, ref handled);
                    if (entitySel.Entity.Properties.Attributes["poulticeRevive"].AsBool(false))
                    {
                        entitySel.Entity.Revive();
                        slot.TakeOut(1);
                        slot.MarkDirty();
                    }
                }
            }
            Vec3d pos = entitySel.Entity.Pos.XYZ;

            SimpleParticleProperties smoke = new SimpleParticleProperties(
                    10, 15,
                    ColorUtil.ToRgba(75, 146, 175, 122),
                    new Vec3d(),
                    new Vec3d(2, 1, 2),
                    new Vec3f(-0.25f, 0f, -0.25f),
                    new Vec3f(0.25f, 0f, 0.25f),
                    0.6f,
                    -0.075f,
                    0.5f,
                    3f,
                    EnumParticleModel.Quad
                );

            smoke.MinPos = pos.AddCopy(-1.5, -0.5, -1.5);
            if (secondsUsed > 0.7f)
            {
                entitySel.Entity.World.SpawnParticles(smoke);
            }
            return false;
        }
    }

    public class WildcraftPoulticePatch
    {

        public static void Patch(Harmony harmony)
        {
            try
            {
                harmony.Patch(methodInfo()
                    , prefix: new HarmonyMethod(typeof(WildcraftPoulticePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception) { }
        }

        public static void Unpatch(Harmony harmony)
        {
            try
            {
                harmony.Unpatch(methodInfo()
                , HarmonyPatchType.Prefix, "gerste.petai");
            }
            catch (Exception) { }
        }

        public static MethodInfo methodInfo()
        {
            Type target = Type.GetType("wildcraft.WildcraftPoultice, wildcraft, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            return target?.GetMethod("OnHeldInteractStop", BindingFlags.Instance | BindingFlags.Public);
        }
        public static bool Prefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity, EntitySelection entitySel)
        {
            if (entitySel == null || entitySel.Entity == null || !entitySel.Entity.HasBehavior<EntityBehaviorTameable>())
            {
                return true;
            }
            if (secondsUsed > 0.7f && byEntity.World.Side == EnumAppSide.Server)
            {
                if (entitySel.Entity.Alive)
                {
                    JsonObject attr = slot.Itemstack.Collectible.Attributes;
                    float health = attr["health"].AsFloat();
                    entitySel.Entity.ReceiveDamage(new DamageSource()
                    {
                        Source = EnumDamageSource.Internal,
                        Type = health > 0 ? EnumDamageType.Heal : EnumDamageType.Poison
                    }, Math.Abs(health));

                    slot.TakeOut(1);
                    slot.MarkDirty();
                }
                else
                {
                    EnumHandling handled = EnumHandling.PassThrough;
                    entitySel.Entity.GetBehavior<EntityBehaviorTameable>()?.OnInteract(byEntity, slot, entitySel.HitPosition, EnumInteractMode.Interact, ref handled);
                }
            }
            Vec3d pos = entitySel.Entity.Pos.XYZ;

            SimpleParticleProperties smoke = new SimpleParticleProperties(
                    10, 15,
                    ColorUtil.ToRgba(75, 146, 175, 122),
                    new Vec3d(),
                    new Vec3d(2, 1, 2),
                    new Vec3f(-0.25f, 0f, -0.25f),
                    new Vec3f(0.25f, 0f, 0.25f),
                    0.6f,
                    -0.075f,
                    0.5f,
                    3f,
                    EnumParticleModel.Quad
                );

            smoke.MinPos = pos.AddCopy(-1.5, -0.5, -1.5);
            if (secondsUsed > 0.7f)
            {
                entitySel.Entity.World.SpawnParticles(smoke);
            }
            return false;
        }
    }

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
        public static bool Prefix(EntityBehaviorMortallyWoundable __instance, ref float damage)
        {
            if (__instance.HealthState == EnumEntityHealthState.Normal){
                return true;
            }
            damage = 0;
            return false;
        }
    }
}