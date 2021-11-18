using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace WolfTaming
{
    public enum DomesticationLevel
    {
        WILD, TAMING, DOMESTICATED
    }

    public class EntityBehaviorTameable : EntityBehavior
    {
        public DomesticationLevel domesticationLevel
        {
            get
            {
                DomesticationLevel level;
                if (Enum.TryParse<DomesticationLevel>(domesticationStatus.GetString("domesticationLevel"), out level))
                {
                    return level;
                }
                else
                {
                    return DomesticationLevel.WILD;
                }
            }
            set
            {
                domesticationStatus.SetString("domesticationLevel", value.ToString());
                entity.WatchedAttributes.MarkPathDirty("domesticationLevel");
            }
        }

        public IPlayer owner
        {
            get
            {
                return entity.World.PlayerByUid(domesticationStatus.GetString("owner"));
            }
            set
            {
                domesticationStatus.SetString("owner", value.PlayerUID);
                entity.WatchedAttributes.MarkPathDirty("owner");
            }
        }

        public float domesticationProgress
        {
            get
            {
                switch (domesticationLevel)
                {
                    case DomesticationLevel.WILD: return 0f;
                    case DomesticationLevel.DOMESTICATED: return 1f;
                    case DomesticationLevel.TAMING: return domesticationStatus.GetFloat("progress", 0f);
                    default: return domesticationStatus.GetFloat("progress");
                }
            }
            set
            {
                domesticationStatus.SetFloat("progress", value);
                entity.WatchedAttributes.MarkPathDirty("progress");
            }
        }

        public float obedience
        {
            get
            {
                switch (domesticationLevel)
                {
                    case DomesticationLevel.WILD: return 0f;
                    case DomesticationLevel.DOMESTICATED: return Math.Max(domesticationStatus.GetFloat("obedience", 0f), 0f);
                    case DomesticationLevel.TAMING: return 0f;
                    default: return domesticationStatus.GetFloat("obedience");
                }
            }
            set
            {
                domesticationStatus.SetFloat("obedience", value);
                entity.WatchedAttributes.MarkPathDirty("obedience");
            }
        }

        double cooldown
        {
            get
            {
                return domesticationStatus.GetDouble("cooldown", entity.World.Calendar.TotalHours);
            }
            set
            {
                domesticationStatus.SetDouble("cooldown", value);
                entity.WatchedAttributes.MarkPathDirty("cooldown");
            }
        }

        public ITreeAttribute domesticationStatus
        {
            get
            {
                if (entity.WatchedAttributes.GetTreeAttribute("domesticationstatus") == null)
                {
                    entity.WatchedAttributes.SetAttribute("domesticationstatus", new TreeAttribute());
                }
                return entity.WatchedAttributes.GetTreeAttribute("domesticationstatus");
            }
            set
            {
                entity.WatchedAttributes.SetAttribute("domesticationstatus", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }
        List<TamingItem> treatList = new List<TamingItem>();
        AssetLocation tameEntityCode;


        long callbackId;
        public EntityBehaviorTameable(Entity entity) : base(entity)
        {
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            JsonObject[] treatItems = attributes["treat"]?.AsArray();
            if (treatItems == null) treatItems = new JsonObject[0];
            foreach (var item in treatItems)
            {
                string name = item["code"].AsString();
                float progress = item["progress"].AsFloat(1f);
                long cooldown = item["cooldown"].AsInt(1);

                treatList.Add(new TamingItem(name, progress, cooldown));
            }

            if (!String.IsNullOrEmpty(attributes["tameEntityCode"].AsString()))
            {
                tameEntityCode = AssetLocation.Create(attributes["tameEntityCode"].AsString());
            }
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
            EntityPlayer player = byEntity as EntityPlayer;

            if (player == null) return;
            if (owner != null && owner.PlayerUID != player.PlayerUID) return;
            if (mode != EnumInteractMode.Interact) return;
            if (byEntity.Controls.Sneak) return;

            if (domesticationLevel == DomesticationLevel.WILD
                && itemslot?.Itemstack?.Item != null)
            {
                if (feedEntityIfPossible(itemslot))
                {
                    domesticationLevel = DomesticationLevel.TAMING;
                    owner = player.Player;
                    (entity.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get("wolftaming:message-startet-taming", entity.GetName(), Math.Round(domesticationProgress * 100, 2)));
                }
            }
            else if (domesticationLevel == DomesticationLevel.TAMING
                && itemslot?.Itemstack?.Item != null)
            {
                if (feedEntityIfPossible(itemslot))
                {
                    (entity.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get("wolftaming:message-tended-to", entity.GetName(), Math.Round(domesticationProgress * 100, 2)));
                }
                if (domesticationProgress >= 1f)
                {
                    domesticationLevel = DomesticationLevel.DOMESTICATED;
                    spawnTameVariant(1f);
                }
            }
            else if (domesticationLevel == DomesticationLevel.DOMESTICATED)
            {
                if (!attachAccessoryIfPossible(byEntity as EntityPlayer, itemslot))
                    feedEntityIfPossible(itemslot);
            }
        }

        public override string PropertyName()
        {
            return "tameable";
        }

        void spawnTameVariant(float dt)
        {
            Entity tameEntity;
            if (tameEntityCode != null && entity.Api.Side == EnumAppSide.Server)
            {
                EntityProperties tameType = entity.World.GetEntityType(tameEntityCode);

                if (tameType == null)
                {
                    entity.World.Logger.Error("Misconfigured entity. Entity with code '{0}' is configured (via Tameable behavior) to be tamed into '{1}', but no such entity type was registered.", entity.Code, tameEntityCode);
                    return;
                }

                Cuboidf collisionBox = tameType.SpawnCollisionBox;

                // Delay spawning if we're colliding
                if (entity.World.CollisionTester.IsColliding(entity.World.BlockAccessor, collisionBox, entity.ServerPos.XYZ, false))
                {
                    callbackId = entity.World.RegisterCallback(spawnTameVariant, 1000);
                    return;
                }

                tameEntity = entity.World.ClassRegistry.CreateEntity(tameType);

                tameEntity.ServerPos.SetFrom(entity.ServerPos);
                tameEntity.Pos.SetFrom(tameEntity.ServerPos);

                entity.Die(EnumDespawnReason.Expire, null);
                entity.World.SpawnEntity(tameEntity);

                if (tameEntity.HasBehavior<EntityBehaviorTameable>())
                {
                    tameEntity.GetBehavior<EntityBehaviorTameable>().domesticationStatus = domesticationStatus;
                }
                tameEntity.GetBehavior<EntityBehaviorNameTag>()?.SetName(entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName);

                //Attempt to not change the texture during taming
                tameEntity.WatchedAttributes.SetInt("textureIndex", entity.WatchedAttributes.GetInt("textureIndex", 0));
            }
            else
            {
                tameEntity = entity;
            }

            var message = new PetNameMessage();
            message.targetEntityUID = tameEntity.EntityId;
            message.oldEntityUID = entity.EntityId;

            (entity.Api as ICoreServerAPI)?.Network.GetChannel("wolftamingnetwork").SendPacket<PetNameMessage>(message, entity.GetBehavior<EntityBehaviorTameable>()?.owner as IServerPlayer);
        }

        bool isValidTamingItem(TamingItem item, ItemSlot slot)
        {
            if (item.name.EndsWith("*"))
            {
                return slot.Itemstack.Item.Code.Path.StartsWith(item.name.Remove(item.name.Length - 1));
            }
            else
            {
                return slot.Itemstack.Item.Code.Path == item.name;
            }
        }

        bool checkTamingSuccess(TamingItem tamingItem, ItemSlot itemSlot)
        {
            if (tamingItem == null) return false;
            if (cooldown <= entity.World.Calendar.TotalHours)
            {
                int acceptedItems = 0;
                var mouth = (entity as EntityAgent)?.LeftHandItemSlot as ItemSlotMouth;
                if (mouth != null)
                {
                    acceptedItems += itemSlot.TryPutInto(entity.World, (entity as EntityAgent).LeftHandItemSlot, 1);
                }
                else
                {
                    acceptedItems = 1;
                    itemSlot.TakeOut(1);
                }
                if (acceptedItems < 1) return false;

                if (domesticationLevel == DomesticationLevel.DOMESTICATED) obedience += tamingItem.progress;
                else domesticationProgress += tamingItem.progress;

                cooldown = entity.World.Calendar.TotalHours + tamingItem.cooldown;
                return true;
            }
            else
            {
                (entity.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get("wolftaming:message-not-ready", entity.GetName()));
            }
            return false;
        }
        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            entity.World.UnregisterCallback(callbackId);
        }

        private bool attachAccessoryIfPossible(EntityPlayer byEntity, ItemSlot slot)
        {
            if (owner.PlayerUID != byEntity?.PlayerUID) return false;
            var item = slot?.Itemstack?.Item;
            var pet = entity as EntityPet;
            if (pet != null && item is ItemPetAccessory)
            {
                return slot.TryFlipWith(pet.GearInventory.GetBestSuitedSlot(slot).slot);
            }
            return false;
        }

        private bool feedEntityIfPossible(ItemSlot foodsource)
        {
            var tamingItem = treatList.Find((item) => isValidTamingItem(item, foodsource));
            return checkTamingSuccess(tamingItem, foodsource);
        }
    }

    class TamingItem
    {
        public string name { get; }
        public float progress { get; }
        public long cooldown { get; }

        public TamingItem(string name, float progress, long cooldown)
        {
            this.name = name;
            this.progress = progress;
            this.cooldown = cooldown;
        }
    }
}