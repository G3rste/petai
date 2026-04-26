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
using System.Text;
using System.Linq;
using Vintagestory.API.Util;

namespace PetAI
{
    public enum DomesticationLevel
    {
        WILD, TAMING, DOMESTICATED
    }

    public class EntityBehaviorTameable : EntityBehavior
    {
        public DomesticationLevel DomesticationLevel
        {
            get
            {
                if (Enum.TryParse<DomesticationLevel>(DomesticationStatus.GetString("domesticationLevel"), out var level))
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
                DomesticationStatus.SetString("domesticationLevel", value.ToString());
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        public string OwnerId
        {
            get => DomesticationStatus.GetString("owner");
            set
            {
                DomesticationStatus.SetString("owner", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        private IPlayer _cachedOwner;
        public IPlayer CachedOwner
        {
            get
            {
                if (_cachedOwner?.PlayerUID == OwnerId)
                {
                    return _cachedOwner;
                }
                if (String.IsNullOrEmpty(OwnerId))
                {
                    return null;
                }
                _cachedOwner = entity.World.PlayerByUid(OwnerId);
                return _cachedOwner;
            }
        }

        public float DomesticationProgress
        {
            get
            {
                return DomesticationLevel switch
                {
                    DomesticationLevel.WILD => 0f,
                    DomesticationLevel.DOMESTICATED => 1f,
                    DomesticationLevel.TAMING => DomesticationStatus.GetFloat("progress", 0f),
                    _ => DomesticationStatus.GetFloat("progress"),
                };
            }
            set
            {
                DomesticationStatus.SetFloat("progress", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        public float Obedience
        {
            get
            {
                return DomesticationLevel switch
                {
                    DomesticationLevel.WILD => 0f,
                    DomesticationLevel.DOMESTICATED => Math.Min(Math.Max(DomesticationStatus.GetFloat("obedience", 0f), 0f), 1f),
                    DomesticationLevel.TAMING => 0f,
                    _ => DomesticationStatus.GetFloat("obedience"),
                };
            }
            set
            {
                DomesticationStatus.SetFloat("obedience", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        public bool MultiplyAllowed
        {
            get
            {
                return DomesticationStatus.GetBool("multiplyAllowed", true);
            }
            set
            {
                DomesticationStatus.SetBool("multiplyAllowed", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        double Cooldown
        {
            get
            {
                return DomesticationStatus.GetDouble("cooldown", entity.World.Calendar.TotalHours);
            }
            set
            {
                DomesticationStatus.SetDouble("cooldown", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        double DisobedienceTime
        {
            get
            {
                return DomesticationStatus.GetDouble("disobedienceTime", entity.World.Calendar.TotalHours);
            }
            set
            {
                DomesticationStatus.SetDouble("disobedienceTime", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        public ITreeAttribute DomesticationStatus
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
        private int Generation => entity.WatchedAttributes.GetInt("generation", 0);
        public EnumNestSize Size { get; set; }
        public List<TamingItem> treatList = [];
        AssetLocation tameEntityCode;

        float disobediencePerDay;


        long callbackId;

        long listenerId;


        public EntityBehaviorTameable(Entity entity) : base(entity)
        {
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            JsonObject[] treatItems = attributes["treat"]?.AsArray() ?? [];
            foreach (var item in treatItems)
            {
                string name = item["code"].AsString();
                string domain = item["domain"].AsString("game");
                float progress = item["progress"].AsFloat(1f);
                long cooldown = item["cooldown"].AsInt(1);

                treatList.Add(new TamingItem()
                {
                    Name = name,
                    Domain = domain,
                    Progress = progress,
                    Cooldown = cooldown
                });
            }

            if (!string.IsNullOrEmpty(attributes["tameEntityCode"].AsString()))
            {
                tameEntityCode = AssetLocation.Create(attributes["tameEntityCode"].AsString());
            }

            var nestSize = EnumNestSize.SMALL;
            Enum.TryParse(attributes["size"]?.AsString()?.ToUpper(), out nestSize);
            Size = nestSize;

            disobediencePerDay = attributes["disobediencePerDay"].AsFloat(0f);
            entity.Api.Event.EnqueueMainThreadTask(() =>
                listenerId = entity.World.RegisterGameTickListener(Disobey, 60000), "register disobedience tick listener"
            );
        }

        public override void AfterInitialized(bool onFirstSpawn)
        {
            entity.GetBehavior<EntityBehaviorHealth>()?.SetMaxHealthModifiers("petconfig", PetConfig.Current.Difficulty.petMaxHpModifier);
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);

            if (byEntity is not EntityPlayer player) return;
            if (CachedOwner != null && CachedOwner.PlayerUID != player.PlayerUID) return;
            if (mode != EnumInteractMode.Interact) return;
            if (!entity.Alive)
            {
                TryReviveWith(itemslot);
                return;
            }
            if (byEntity.Controls.Sneak) return;

            if (DomesticationLevel == DomesticationLevel.WILD
                && itemslot?.Itemstack?.Collectible != null)
            {
                if (FeedEntityIfPossible(itemslot, player))
                {
                    DomesticationLevel = DomesticationLevel.TAMING;
                    OwnerId = player.PlayerUID;
                    (player.Player as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("petai:message-startet-taming", entity.GetName(), Math.Round(DomesticationProgress * 100, 2)), EnumChatType.Notification);
                }
            }
            else if (DomesticationLevel == DomesticationLevel.TAMING
                && itemslot?.Itemstack?.Collectible != null)
            {
                if (FeedEntityIfPossible(itemslot, player))
                {
                    (player.Player as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("petai:message-tended-to", entity.GetName(), Math.Round(DomesticationProgress * 100, 2)), EnumChatType.Notification);
                }
                if (DomesticationProgress >= 1f)
                {
                    DomesticationLevel = DomesticationLevel.DOMESTICATED;
                    SpawnTameVariant(1f);
                }
            }
            else if (DomesticationLevel == DomesticationLevel.DOMESTICATED)
            {
                FeedEntityIfPossible(itemslot, player);
            }

            if (itemslot?.Itemstack?.Collectible?.Code?.Path == "magicbone")
            {
                DomesticationLevel = DomesticationLevel.DOMESTICATED;
                Obedience = 1;
                OwnerId = (byEntity as EntityPlayer)?.PlayerUID;
                SpawnTameVariant(1f);
            }
        }
        public override string PropertyName()
        {
            return "tameable";
        }

        void SpawnTameVariant(float dt)
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
                if (entity.World.CollisionTester.IsColliding(entity.World.BlockAccessor, collisionBox, entity.Pos.XYZ, false))
                {
                    callbackId = entity.World.RegisterCallback(SpawnTameVariant, 1000);
                    return;
                }

                tameEntity = entity.World.ClassRegistry.CreateEntity(tameType);

                tameEntity.Pos.SetFrom(entity.Pos);
                tameEntity.Pos.SetFrom(tameEntity.Pos);

                entity.Die(EnumDespawnReason.Expire, null);
                entity.World.SpawnEntity(tameEntity);

                if (tameEntity.HasBehavior<EntityBehaviorTameable>())
                {
                    tameEntity.GetBehavior<EntityBehaviorTameable>().DomesticationStatus = DomesticationStatus;
                }

                //Attempt to keep the growth progress of the entity
                if (entity.WatchedAttributes.HasAttribute("grow"))
                {
                    tameEntity.WatchedAttributes.SetAttribute("grow", entity.WatchedAttributes.GetAttribute("grow"));
                }

                tameEntity.GetBehavior<EntityBehaviorNameTag>()?.SetName(entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName);

                //Attempt to not change the texture during taming
                tameEntity.WatchedAttributes.SetInt("textureIndex", entity.WatchedAttributes.GetInt("textureIndex", 0));
            }
            else
            {
                tameEntity = entity;
            }

            var message = new PetProfileMessage
            {
                targetEntityUID = tameEntity.EntityId,
                oldEntityUID = entity.EntityId
            };

            (entity.Api as ICoreServerAPI)?.Network.GetChannel("petainetwork").SendPacket<PetProfileMessage>(message, entity.GetBehavior<EntityBehaviorTameable>()?.CachedOwner as IServerPlayer);
        }

        bool IsValidTamingItem(TamingItem item, ItemSlot slot)
        {
            if (item.Name.EndsWith("*"))
            {
                return slot.Itemstack?.Collectible?.Code.Path.StartsWith(item.Name[..^1]) == true;
            }
            else
            {
                return slot.Itemstack?.Collectible?.Code.Path == item.Name;
            }
        }

        bool CheckTamingSuccess(TamingItem tamingItem, ItemSlot itemSlot, EntityPlayer player)
        {
            if (tamingItem == null) return false;
            if (Cooldown <= entity.World.Calendar.TotalHours)
            {
                int acceptedItems = 0;
                if (entity is EntityAgent { LeftHandItemSlot: ItemSlotMouth })
                {
                    acceptedItems += itemSlot.TryPutInto(entity.World, (entity as EntityAgent).LeftHandItemSlot, 1);
                }
                else
                {
                    acceptedItems = 1;
                    itemSlot.TakeOut(1);
                }
                if (acceptedItems < 1) return false;

                if (DomesticationLevel == DomesticationLevel.DOMESTICATED) Obedience += tamingItem.Progress * PetConfig.Current.Difficulty.obedienceMultiplier * (float)Math.Pow(1f + PetConfig.Current.Difficulty.obedienceMultiplierIncreasePerGen, Generation);
                else DomesticationProgress += tamingItem.Progress * PetConfig.Current.Difficulty.tamingMultiplier * (float)Math.Pow(1f + PetConfig.Current.Difficulty.tamingMultiplierIncreasePerGen, Generation);

                Cooldown = entity.World.Calendar.TotalHours + tamingItem.Cooldown;

                // if an entity does not implement the mouthinventory, it should still be able to multiply
                if (!entity.HasBehavior<EntityBehaviorMouthInventory>())
                {
                    ITreeAttribute tree = entity.WatchedAttributes.GetOrAddTreeAttribute("hunger");
                    tree.SetFloat("saturation", tree.GetFloat("saturation", 0) + 1);
                }

                entity.WatchedAttributes.MarkPathDirty("hunger");

                return true;
            }
            else
            {
                (player.Player as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("petai:message-not-ready", entity.GetName()), EnumChatType.Notification);
            }
            return false;
        }

        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            entity.World.UnregisterCallback(callbackId);
            entity.World.UnregisterGameTickListener(listenerId);
        }

        private bool FeedEntityIfPossible(ItemSlot foodsource, EntityPlayer player)
        {
            var tamingItem = treatList.Find((item) => IsValidTamingItem(item, foodsource));
            return CheckTamingSuccess(tamingItem, foodsource, player);
        }

        private void Disobey(float intervall)
        {
            // if players are offline for multiple days they should not loose all pet progress
            double hoursPassed = Math.Min(24, entity.World.Calendar.TotalHours - DisobedienceTime);

            // I hope the PlayerEntity is null when the player is offline
            if (CachedOwner?.Entity != null)
            {
                Obedience -= PetConfig.Current.Difficulty.disobedienceMultiplier * disobediencePerDay * ((float)(hoursPassed / 24)) * (float)Math.Pow(1f - PetConfig.Current.Difficulty.disobedienceMultiplierDecreasePerGen, Generation);
            }
            DisobedienceTime = entity.World.Calendar.TotalHours;
        }
        public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
        {
            ItemStack[] treats = [.. treatList
                .ConvertAll(treat => new AssetLocation(treat.Domain + ":" + treat.Name))
                .ConvertAll(treat => (CollectibleObject)world.GetItem(treat) ?? world.GetBlock(treat))
                .FindAll(treat => treat != null)
                .ConvertAll(treat => new ItemStack(treat))];
            if (entity.Alive && treats.Length > 0 && (string.IsNullOrEmpty(OwnerId) || player.PlayerUID == OwnerId))
            {
                return [
                    new WorldInteraction()
                    {
                        ActionLangCode = "petai:interact-feed",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = treats
                    }
                ];
            }
            else if (!entity.Alive && entity.GetBehavior<EntityBehaviorHarvestable>()?.IsHarvested != true && PetConfig.Current.Resurrectors?.Length > 0)
            {
                return [
                    new WorldInteraction()
                    {
                        ActionLangCode = "petai:interact-revive",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = [.. PetConfig.Current.Resurrectors.ToList()
                            .ConvertAll(resurrector => new AssetLocation(resurrector))
                            .ConvertAll(resurrector => (CollectibleObject)world.GetItem(resurrector) ?? world.GetBlock(resurrector))
                            .FindAll(resurrector => resurrector != null)
                            .ConvertAll(resurrector => new ItemStack(resurrector))]
                    }
                ];
            }
            else
            {
                return base.GetInteractionHelp(world, es, player, ref handled);
            }
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            var aggressor = damageSource.CauseEntity ?? damageSource.SourceEntity;
            if (aggressor is EntityPlayer player
                && (player.PlayerUID == OwnerId && !PetConfig.Current.PetDamageableByOwner
                    || player.PlayerUID != OwnerId && !PetConfig.Current.PvpOn && DomesticationLevel != DomesticationLevel.WILD)
                || damageSource.Source == EnumDamageSource.Fall
                && PetConfig.Current.FalldamageOff)
            {
                damage = 0;
                damageSource.CauseEntity = null;
                damageSource.SourceEntity = null;
            }
            var behaviorHealth = entity.GetBehavior<EntityBehaviorHealth>();
            if (behaviorHealth?.Health < 0)
            {
                behaviorHealth.Health=GameMath.Clamp(behaviorHealth.Health, 0, behaviorHealth.MaxHealth);
            }
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);
            if (!string.IsNullOrEmpty(OwnerId) && entity.Api is ICoreServerAPI sapi)
            {
                sapi.SendMessage(CachedOwner,
                GlobalConstants.GeneralChatGroup,
                Lang.Get("petai:message-pet-dead",
                entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName),
                EnumChatType.Notification);

                if (entity.Api.ModLoader.GetModSystem<WorldMapManager>().MapLayers.Find(ml => ml is WaypointMapLayer) is WaypointMapLayer waypointMap)
                {
                    waypointMap.AddWaypoint(new Waypoint()
                    {
                        Color = 9044739,
                        Icon = "gravestone",
                        Pinned = true,
                        Position = entity.Pos.XYZ,
                        OwningPlayerUid = OwnerId,
                        Title = Lang.Get("petai:message-pet-dead", entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName),
                    },
                        CachedOwner as IServerPlayer);
                }
            }
        }

        public override void GetInfoText(StringBuilder infotext)
        {
            if (CachedOwner == null) return;

            infotext
                .AppendLine(Lang.Get("petai:gui-pet-owner", CachedOwner?.PlayerName))
                .AppendLine(DomesticationLevel == DomesticationLevel.DOMESTICATED ? Lang.Get("petai:gui-pet-obedience", Math.Round(Obedience * 100, 2)) : Lang.Get("petai:gui-pet-domesticationProgress", Math.Round(DomesticationProgress * 100, 2)))
                .AppendLine(Lang.Get("petai:gui-pet-nestsize", Lang.Get("petai:gui-pet-nestsize-" + Size.ToString().ToLower())));
            if (entity.HasBehavior<EntityBehaviorHealth>())
            {
                var beh = entity.GetBehavior<EntityBehaviorHealth>();
                infotext.AppendLine(Lang.Get("Health: {0}/{1}", beh.Health, beh.MaxHealth));
            }
        }

        private void TryReviveWith(ItemSlot itemslot)
        {
            var isResurrector = PetConfig.Current.Resurrectors.Any(resurrector => resurrector.Split(":").Last() == itemslot?.Itemstack?.Collectible?.Code?.Path);
            if (isResurrector && entity.GetBehavior<EntityBehaviorHarvestable>()?.IsHarvested != true)
            {
                entity.Revive();
                if (entity.HasBehavior<EntityBehaviorMortallyWoundable>())
                {
                    entity.GetBehavior<EntityBehaviorMortallyWoundable>().HealthState = EnumEntityHealthState.Normal;
                }
                entity.AnimManager?.ActiveAnimationsByAnimCode.Keys.Foreach(entity.AnimManager.StopAnimation);
                itemslot.TakeOut(1);
                itemslot.MarkDirty();
            }
        }
    }

    public class TamingItem
    {
        public string Name { get; set; }

        public string Domain { get; set; }
        public float Progress { get; set; }
        public long Cooldown { get; set; }
    }
}
