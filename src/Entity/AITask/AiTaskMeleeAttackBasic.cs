using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace PetAI
{
    public class AiTaskMeleeAttackBasic : AiTaskBase, IWorldIntersectionSupplier
    {
        Entity targetEntity;

        long lastCheckOrAttackMs;

        float damage = 2f;
        float minDist = 1.5f;
        float minVerDist = 1f;


        bool damageInflicted = false;

        int attackDurationMs = 1500;
        int damagePlayerAtMs = 500;

        BlockSelection blockSel = new BlockSelection();
        EntitySelection entitySel = new EntitySelection();

        public EnumDamageType damageType = EnumDamageType.BluntAttack;
        public int damageTier = 0;
        float tamingGenerations = 10f;

        bool isCommandable = false;

        public Vec3i MapSize { get { return entity.World.BlockAccessor.MapSize; } }

        public AiTaskMeleeAttackBasic(EntityAgent entity) : base(entity)
        {
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);

            if (taskConfig["tamingGenerations"] != null)
            {
                tamingGenerations = taskConfig["tamingGenerations"].AsFloat(10f);
            }

            this.damage = taskConfig["damage"].AsFloat(2);
            this.attackDurationMs = taskConfig["attackDurationMs"].AsInt(1500);
            this.damagePlayerAtMs = taskConfig["damagePlayerAtMs"].AsInt(1000);

            this.minDist = taskConfig["minDist"].AsFloat(2f);
            this.minVerDist = taskConfig["minVerDist"].AsFloat(1f);

            string strdt = taskConfig["damageType"].AsString();
            if (strdt != null)
            {
                this.damageType = (EnumDamageType)Enum.Parse(typeof(EnumDamageType), strdt, true);
            }
            this.damageTier = taskConfig["damageTier"].AsInt(0);

            ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("extraInfoText");
            tree.SetString("dmgTier", Lang.Get("Damage tier: {0}", damageTier));

            this.isCommandable = taskConfig["isCommandable"].AsBool(false);
        }

        public override bool ShouldExecute()
        {
            long ellapsedMs = entity.World.ElapsedMilliseconds;
            if (ellapsedMs - lastCheckOrAttackMs < attackDurationMs || cooldownUntilMs > ellapsedMs)
            {
                return false;
            }

            Vec3d pos = entity.ServerPos.XYZ.Add(0, entity.CollisionBox.Y2 / 2, 0).Ahead(entity.CollisionBox.XSize / 2, 0, entity.ServerPos.Yaw);
            targetEntity = getEntityToAttack(entity, isCommandable);

            lastCheckOrAttackMs = entity.World.ElapsedMilliseconds;
            damageInflicted = false;

            return targetEntity != null && hasDirectContact(targetEntity);
        }


        float curTurnRadPerSec;

        public override void StartExecute()
        {
            base.StartExecute();
            curTurnRadPerSec = entity.GetBehavior<EntityBehaviorTaskAI>().PathTraverser.curTurnRadPerSec;
        }

        public override bool ContinueExecute(float dt)
        {
            EntityPos own = entity.ServerPos;
            EntityPos his = targetEntity.ServerPos;

            float desiredYaw = (float)Math.Atan2(his.X - own.X, his.Z - own.Z);
            float yawDist = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);
            entity.ServerPos.Yaw += GameMath.Clamp(yawDist, -curTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier, curTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier);
            entity.ServerPos.Yaw = entity.ServerPos.Yaw % GameMath.TWOPI;


            if (lastCheckOrAttackMs + damagePlayerAtMs > entity.World.ElapsedMilliseconds) return true;

            if (!damageInflicted)
            {
                if (!hasDirectContact(targetEntity)) return false;

                bool alive = targetEntity.Alive;

                targetEntity.ReceiveDamage(
                    new DamageSource()
                    {
                        Source = EnumDamageSource.Entity,
                        SourceEntity = entity,
                        Type = damageType,
                        DamageTier = damageTier
                    },
                    damage * GlobalConstants.CreatureDamageModifier
                );

                if (alive && !targetEntity.Alive)
                {
                    this.entity.GetBehavior<EntityBehaviorEmotionStates>()?.TryTriggerState("saturated");
                }

                damageInflicted = true;
            }

            if (lastCheckOrAttackMs + attackDurationMs > entity.World.ElapsedMilliseconds) return true;
            return false;
        }



        bool hasDirectContact(Entity targetEntity)
        {
            Cuboidd targetBox = targetEntity.CollisionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
            Vec3d pos = entity.ServerPos.XYZ.Add(0, entity.CollisionBox.Y2 / 2, 0).Ahead(entity.CollisionBox.XSize / 2, 0, entity.ServerPos.Yaw);
            double dist = targetBox.ShortestDistanceFrom(pos);
            double vertDist = Math.Abs(targetBox.ShortestVerticalDistanceFrom(pos.Y));
            if (dist >= minDist || vertDist >= minVerDist) return false;

            Vec3d rayTraceFrom = entity.ServerPos.XYZ;
            rayTraceFrom.Y += 1 / 32f;
            Vec3d rayTraceTo = targetEntity.ServerPos.XYZ;
            rayTraceTo.Y += 1 / 32f;
            bool directContact = false;

            entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref blockSel, ref entitySel);
            directContact = blockSel == null;

            if (!directContact)
            {
                rayTraceFrom.Y += entity.CollisionBox.Y2 * 7 / 16f;
                rayTraceTo.Y += targetEntity.CollisionBox.Y2 * 7 / 16f;
                entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref blockSel, ref entitySel);
                directContact = blockSel == null;
            }

            if (!directContact)
            {
                rayTraceFrom.Y += entity.CollisionBox.Y2 * 7 / 16f;
                rayTraceTo.Y += targetEntity.CollisionBox.Y2 * 7 / 16f;
                entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref blockSel, ref entitySel);
                directContact = blockSel == null;
            }

            if (!directContact) return false;

            return true;
        }


        public Block GetBlock(BlockPos pos)
        {
            return entity.World.BlockAccessor.GetBlock(pos);
        }

        public Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos)
        {
            return entity.World.BlockAccessor.GetBlock(pos).GetCollisionBoxes(entity.World.BlockAccessor, pos);
        }

        public bool IsValidPos(BlockPos pos)
        {
            return entity.World.BlockAccessor.IsValidPos(pos);
        }


        public Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
        {
            return new Entity[0];
        }

        public static Entity getEntityToAttack(Entity entity, bool isCommandable)
        {
            Entity victim = entity.GetBehavior<EntityBehaviorSelfDefense>()?.attacker;
            if (isCommandable && entity.GetBehavior<EntityBehaviorReceiveCommand>()?.aggressionLevel == EnumAggressionLevel.PROTECTIVE)
            {
                var nullableVictim = entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>()?.attacker;
                if (nullableVictim != null) victim = nullableVictim;
            }
            if (isCommandable && entity.GetBehavior<EntityBehaviorReceiveCommand>()?.aggressionLevel == EnumAggressionLevel.AGGRESSIVE)
            {
                var nullableVictim = entity.GetBehavior<EntityBehaviorTameable>()?.owner?.Entity?.GetBehavior<EntityBehaviorGiveCommand>()?.victim;
                if (nullableVictim != null) victim = nullableVictim;
            }
            if (isCommandable && entity.GetBehavior<EntityBehaviorReceiveCommand>()?.aggressionLevel == EnumAggressionLevel.PASSIVE)
            {
                victim = null;
            }
            IPlayer owner = entity.GetBehavior<EntityBehaviorTameable>()?.owner;
            if (victim == null
                || !victim.Alive
                || victim.EntityId == entity.EntityId
                || owner != null
                    && ((victim as EntityPlayer)?.PlayerUID == owner.PlayerUID
                    || victim.GetBehavior<EntityBehaviorTameable>()?.owner?.PlayerUID == owner.PlayerUID))
            {
                return null;
            }
            return victim;
        }
    }
}