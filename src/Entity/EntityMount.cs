using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using System.IO;
using Vintagestory.API.Client;
using System.Collections.Generic;

namespace PetAI
{
    public class EntityMount : EntityPet, IMountable
    {
        public EntityAgent rider;
        public Vec3d MountPosition => SidedPos.XYZ.AddCopy(0, mountHeight, 0);

        public float? MountYaw => null;

        public string SuggestedAnimation => riderIdleAnimation;

        private AnimationMetaData walkAnimation;
        private AnimationMetaData sprintAnimation;
        private AnimationMetaData backwardAnimation;

        private float mountHeight = 1f;

        private string riderIdleAnimation;

        public float mountWalkingSpeed { get; private set; } = 0.02f;
        public float mountRunningSpeed { get; private set; } = 0.06f;
        public IMountableSupplier MountSupplier => null;

        public bool CanControl => false;

        public Entity MountedBy => rider;

        EntityPos mountPos = new EntityPos();
        EntityPos IMountable.MountPosition
        {
            get
            {
                mountPos.SetPos(MountPosition);
                return mountPos;
            }

        }

        public EnumMountAngleMode AngleMode => EnumMountAngleMode.PushYaw;

        Vec3f IMountable.LocalEyePos => new Vec3f(0, unchecked((float)Properties.EyeHeight), 0);

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            controls = new EntityControls();
            riderIdleAnimation = Properties.Attributes["riderIdleAnimation"].AsString();
            mountHeight = Properties.Attributes["mountHeight"].AsFloat(1f);
            mountWalkingSpeed = Properties.Attributes["mountWalkingSpeed"].AsFloat(0.02f);
            mountRunningSpeed = Properties.Attributes["mountRunningSpeed"].AsFloat(0.06f);
            walkAnimation = LoadAnimFromJson(Properties.Attributes["mountAnimations"]["walk"]);
            sprintAnimation = LoadAnimFromJson(Properties.Attributes["mountAnimations"]["sprint"]);
            backwardAnimation = LoadAnimFromJson(Properties.Attributes["mountAnimations"]["backward"]);
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
            if (mode == EnumInteractMode.Interact
                && !byEntity.Controls.Sprint
                && slot.Empty
                && GetBehavior<EntityBehaviorTameable>()?.cachedOwner?.Entity == byEntity)
            {
                byEntity.TryMount(this);
            }
            else
            {
                base.OnInteract(byEntity, slot, hitPosition, mode);
            }
        }

        public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
        {
            if (rider != null && rider == damageSource?.SourceEntity)
            {
                return false;
            }
            return base.ShouldReceiveDamage(damageSource, damage);
        }

        public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
        {
            var interactions = new List<WorldInteraction>();
            if (Alive && GetBehavior<EntityBehaviorTameable>()?.ownerId == player.PlayerUID)
            {
                interactions.Add(new WorldInteraction()
                {
                    ActionLangCode = "petai:interact-mount",
                    // it crashes on an empty Itemstack, so we use an invisible one
                    Itemstacks = new ItemStack[1] { new ItemStack(World.GetBlock(new AssetLocation("air"))) },
                    MouseButton = EnumMouseButton.Right
                });
            }
            interactions.AddRange(base.GetInteractionHelp(world, es, player));

            return interactions.ToArray();
        }

        private void updateAnims(EnumEntityAction action, bool on)
        {
            switch (action)
            {
                case EnumEntityAction.Forward:
                    if (on)
                    {
                        if (controls.Sprint) { AnimManager.StartAnimation(sprintAnimation); }
                        else { AnimManager.StartAnimation(walkAnimation); }
                    }
                    else
                    {
                        AnimManager.StopAnimation(walkAnimation.Code);
                        AnimManager.StopAnimation(sprintAnimation.Code);
                    }
                    break;
                case EnumEntityAction.Backward:
                    if (on) { AnimManager.StartAnimation(backwardAnimation); }
                    else { AnimManager.StopAnimation(backwardAnimation.Code); }
                    break;
                case EnumEntityAction.Sprint:
                    if (on)
                    {
                        if (controls.Forward) { AnimManager.StartAnimation(sprintAnimation); }
                    }
                    else
                    {
                        AnimManager.StopAnimation(sprintAnimation.Code);
                    }
                    break;
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
            if (action == EnumEntityAction.Sneak && !on)
            {
                rider?.TryUnmount();
                Controls.WalkVector.Mul(0);
                Controls.StopAllMovement();
            }
            updateAnims(action, on);
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