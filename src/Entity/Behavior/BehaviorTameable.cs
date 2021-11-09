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

        List<TamingItem> initiatorList = new List<TamingItem>();
        List<TamingItem> progressorList = new List<TamingItem>();
        AssetLocation tameEntityCode;


        long callbackId;
        public EntityBehaviorTameable(Entity entity) : base(entity)
        {
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            JsonObject[] initItems = attributes["initiator"]?.AsArray();
            if (initItems == null) initItems = new JsonObject[0];
            foreach (var item in initItems)
            {
                string name = item["code"].AsString();
                float progress = item["progress"].AsFloat(0f);
                long cooldown = item["cooldown"].AsInt(1);

                initiatorList.Add(new TamingItem(name, progress, cooldown));
            }

            JsonObject[] progItems = attributes["progressor"]?.AsArray();
            if (progItems == null) progItems = new JsonObject[0];
            foreach (var item in progItems)
            {
                string name = item["code"].AsString();
                float progress = item["progress"].AsFloat(1f);
                long cooldown = item["cooldown"].AsInt(1);

                progressorList.Add(new TamingItem(name, progress, cooldown));
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
            if (domesticationLevel == DomesticationLevel.WILD
                && itemslot?.Itemstack?.Item != null)
            {
                var tamingItem = initiatorList.Find((item) => isValidTamingItem(item, itemslot));
                if (checkTamingSuccess(tamingItem, itemslot))
                {
                    domesticationLevel = DomesticationLevel.TAMING;
                    owner = player.Player;
                    (entity.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get("wolftaming:message-startet-taming", entity.GetName(), domesticationProgress * 100));
                }
            }
            else if (domesticationLevel == DomesticationLevel.TAMING
                && itemslot?.Itemstack?.Item != null)
            {
                var tamingItem = progressorList.Find((item) => isValidTamingItem(item, itemslot));
                if (checkTamingSuccess(tamingItem, itemslot))
                {
                    (entity.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get("wolftaming:message-tended-to", entity.GetName(), domesticationProgress * 100));
                }
                if (domesticationProgress >= 1f)
                {
                    domesticationLevel = DomesticationLevel.DOMESTICATED;
                    spawnTameVariant(1f);
                }
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
            if (tamingItem != null)
            {
                if (cooldown <= entity.World.Calendar.TotalHours)
                {
                    int acceptedItems = 0;
                    var mouth = (entity as EntityAgent)?.LeftHandItemSlot as ItemSlotMouth;
                    if (mouth != null && mouth.mouthable(itemSlot))
                    {
                        acceptedItems += itemSlot.TryPutInto(entity.World, (entity as EntityAgent).LeftHandItemSlot, 1);
                    }
                    if(acceptedItems < 1)
                    {
                        itemSlot.TakeOut(1);
                    }
                    domesticationProgress += tamingItem.progress;
                    cooldown = entity.World.Calendar.TotalHours + tamingItem.cooldown;
                    return true;
                }
                else
                {
                    (entity.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get("wolftaming:message-not-ready", entity.GetName()));
                }
            }
            return false;
        }
        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            entity.World.UnregisterCallback(callbackId);
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