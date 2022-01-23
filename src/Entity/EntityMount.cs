using Vintagestory.API.Config;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using System.IO;

namespace PetAI
{
    public class EntityMount : EntityPet, IMountable, IMountableSupplier
    {
        public EntityAgent rider;
        public Vec3d MountPosition => SidedPos.XYZ.AddCopy(0, mountHeight, 0);

        public float? MountYaw => null;

        public string SuggestedAnimation => "sitflooridle";

        private AnimationMetaData walkAnimation;
        private AnimationMetaData sprintAnimation;
        private AnimationMetaData backwardAnimation;

        private float mountHeight = 1f;

        public float mountWalkingSpeed { get; private set; } = 0.02f;
        public float mountRunningSpeed { get; private set; } = 0.06f;
        public IMountableSupplier MountSupplier => this;

        public long riderEntityIdForInit { get; private set; }

        public bool IsMountedBy(Entity entity)
        {
            return rider?.EntityId == entity.EntityId;
        }

        public Vec3f GetMountOffset(Entity entity)
        {
            return new Vec3f(0, mountHeight, 0);
        }

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            rider = api.World.GetEntityById(riderEntityIdForInit) as EntityAgent;
            controls = new EntityControls();
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
            if (mode == EnumInteractMode.Interact && slot.Empty)
            {
                byEntity.TryMount(this);
            }
            else
            {
                base.OnInteract(byEntity, slot, hitPosition, mode);
            }
        }

        public void updateAnims()
        {
            if (controls.Forward)
            {
                AnimManager.StopAnimation(backwardAnimation.Code);
                AnimManager.StopAnimation(walkAnimation.Code);
                AnimManager.StopAnimation(sprintAnimation.Code);
                AnimManager.StartAnimation(controls.Sprint ? sprintAnimation : walkAnimation);
            }
            else if (controls.Backward)
            {
                AnimManager.StopAnimation(backwardAnimation.Code);
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

        public override void ToBytes(BinaryWriter writer, bool forClient)
        {
            base.ToBytes(writer, forClient);

            writer.Write(rider?.EntityId ?? (long)0);
        }

        public override void FromBytes(BinaryReader reader, bool fromServer)
        {
            base.FromBytes(reader, fromServer);
            long entityId = reader.ReadInt64();
            riderEntityIdForInit = entityId;
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