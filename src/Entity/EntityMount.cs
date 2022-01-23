using Vintagestory.API.Config;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.IO;
using Vintagestory.API.Client;

namespace PetAI
{
    public class EntityMount : EntityPet, IMountable, IMountableSupplier, IRenderer
    {
        public EntityAgent rider;
        public Vec3d MountPosition => SidedPos.XYZ.AddCopy(0, mountHeight, 0);

        public float? MountYaw => null;

        public string SuggestedAnimation => "sitflooridle";

        private AnimationMetaData walkAnimation;
        private AnimationMetaData sprintAnimation;
        private AnimationMetaData backwardAnimation;

        private float mountHeight = 1f;

        private float mountWalkingSpeed = 0.02f;
        private float mountRunningSpeed = 0.06f;
        public IMountableSupplier MountSupplier => this;

        public double RenderOrder => 0;

        public int RenderRange => 999;

        public bool IsMountedBy(Entity entity)
        {
            return rider?.EntityId == entity.EntityId;
        }

        public Vec3f GetMountOffset(Entity entity)
        {
            return new Vec3f(0, mountHeight, 0);
        }

        public void Dispose() { }

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            controls = new EntityControls();
            mountHeight = Properties.Attributes["mountHeight"].AsFloat(1f);
            mountWalkingSpeed = Properties.Attributes["mountWalkingSpeed"].AsFloat(0.02f);
            mountRunningSpeed = Properties.Attributes["mountRunningSpeed"].AsFloat(0.06f);
            walkAnimation = LoadAnimFromJson(Properties.Attributes["mountAnimations"]["walk"]);
            sprintAnimation = LoadAnimFromJson(Properties.Attributes["mountAnimations"]["sprint"]);
            backwardAnimation = LoadAnimFromJson(Properties.Attributes["mountAnimations"]["backward"]);
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (Api.Side == EnumAppSide.Server) updateMotion(dt);
        }

        public void DidMount(EntityAgent entityAgent)
        {
            if (rider != null && rider != entityAgent)
            {
                entityAgent.TryUnmount();
                return;
            }
            rider = entityAgent;
            controls.MovespeedMultiplier = 1;
            controls.OnAction += OnControls;
        }

        public void DidUnmount(EntityAgent entityAgent)
        {
            rider = null;
        }

        public void MountableToTreeAttributes(TreeAttribute tree)
        {
            tree.SetString("className", "EntityMount");
            tree.SetLong("mountId", EntityId);
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
        {
            if (mode == EnumInteractMode.Interact && slot.Empty)
            {
                byEntity.TryMount(this);
            }
            else
            {
                base.OnInteract(byEntity, slot, hitPosition, mode);
            }
        }


        public void OnRenderFrame(float dt, EnumRenderStage stage)
        {
            // Client side we update every frame for smoother turning
            updateMotion(dt);
        }

        private void updateMotion(float dt)
        {
            if (rider != null && rider is EntityAgent)
            {
                float desiredYaw = rider.SidedPos.Yaw + 1.5708f;

                float yawDist = GameMath.AngleRadDistance(SidedPos.Yaw, desiredYaw);
                SidedPos.Yaw += GameMath.Clamp(yawDist, -1440 * dt, 1440 * dt);
                SidedPos.Yaw = SidedPos.Yaw % GameMath.TWOPI;
                SidedPos.Roll = 0;

                if (controls.Forward)
                {
                    float factor = controls.Sprint ? mountRunningSpeed : mountWalkingSpeed;
                    double cosYaw = Math.Cos(SidedPos.Yaw);
                    double sinYaw = Math.Sin(SidedPos.Yaw);
                    Controls.WalkVector.Set(sinYaw, 0, cosYaw);
                    Controls.WalkVector.Mul(factor * GlobalConstants.OverallSpeedMultiplier);
                }
                else if (controls.Backward)
                {
                    double cosYaw = Math.Cos(SidedPos.Yaw);
                    double sinYaw = Math.Sin(SidedPos.Yaw);
                    Controls.WalkVector.Set(-sinYaw, 0, -cosYaw);
                    Controls.WalkVector.Mul(0.01 * GlobalConstants.OverallSpeedMultiplier);
                }
                else
                {
                    Controls.WalkVector.Set(0, 0, 0);
                }
            }
        }

        public void updateAnims()
        {
            if (controls.Forward)
            {
                AnimManager.StopAnimation(backwardAnimation.Code);
                AnimManager.StopAnimation(controls.Sprint ? walkAnimation.Code : sprintAnimation.Code);
                AnimManager.StartAnimation(controls.Sprint ? sprintAnimation : walkAnimation);
            }
            else if (controls.Backward)
            {
                AnimManager.StopAnimation(walkAnimation.Code);
                AnimManager.StopAnimation(sprintAnimation.Code);
                AnimManager.StartAnimation(backwardAnimation);
            }
            else
            {
                AnimManager.StopAnimation(backwardAnimation.Code);
                AnimManager.StopAnimation(walkAnimation.Code);
                AnimManager.StopAnimation(sprintAnimation.Code);
            }
        }

        internal static IMountable GetMountable(IWorldAccessor world, TreeAttribute tree)
        {
            if (tree.HasAttribute("mountId"))
            {
                return world.GetEntityById(tree.GetLong("mountId")) as EntityMount;
            }
            else return null;
        }

        private void OnControls(EnumEntityAction action, bool on, ref EnumHandling handled)
        {
            if (action == EnumEntityAction.Sneak && on)
            {
                rider?.TryUnmount();
                Controls.WalkVector.Mul(0);
                Controls.StopAllMovement();
            }
            updateAnims();
        }

        private AnimationMetaData LoadAnimFromJson(JsonObject json) => new AnimationMetaData
        {
            Code = json["code"].AsString(),
            Animation = json["animation"].AsString(),
            EaseInSpeed = 1f,
            EaseOutSpeed = 1f,
            AnimationSpeed = json["animationSpeed"].AsFloat()
        }.Init();
    }
}