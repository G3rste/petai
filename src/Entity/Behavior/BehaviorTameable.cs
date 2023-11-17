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

namespace PetAI
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
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        public String ownerId
        {
            get => domesticationStatus.GetString("owner");
            set
            {
                domesticationStatus.SetString("owner", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        private IPlayer _cachedOwner;
        public IPlayer cachedOwner
        {
            get
            {
                if (_cachedOwner?.PlayerUID == ownerId)
                {
                    return _cachedOwner;
                }
                if (String.IsNullOrEmpty(ownerId))
                {
                    return null;
                }
                _cachedOwner = entity.World.PlayerByUid(ownerId);
                return _cachedOwner;
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
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        public float obedience
        {
            get
            {
                switch (domesticationLevel)
                {
                    case DomesticationLevel.WILD: return 0f;
                    case DomesticationLevel.DOMESTICATED: return Math.Min(Math.Max(domesticationStatus.GetFloat("obedience", 0f), 0f), 1f);
                    case DomesticationLevel.TAMING: return 0f;
                    default: return domesticationStatus.GetFloat("obedience");
                }
            }
            set
            {
                domesticationStatus.SetFloat("obedience", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        public bool multiplyAllowed
        {
            get
            {
                return domesticationStatus.GetBool("multiplyAllowed", true);
            }
            set
            {
                domesticationStatus.SetBool("multiplyAllowed", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
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
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
            }
        }

        double disobedienceTime
        {
            get
            {
                return domesticationStatus.GetDouble("disobedienceTime", entity.World.Calendar.TotalHours);
            }
            set
            {
                domesticationStatus.SetDouble("disobedienceTime", value);
                entity.WatchedAttributes.MarkPathDirty("domesticationstatus");
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
        private int generation => entity.WatchedAttributes.GetInt("generation", 0);
        public EnumNestSize size { get; set; }
        public List<TamingItem> treatList = new List<TamingItem>();
        AssetLocation tameEntityCode;

        float disobediencePerDay;


        long callbackId;

        long listenerId;


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
                string domain = item["domain"].AsString("game");
                float progress = item["progress"].AsFloat(1f);
                long cooldown = item["cooldown"].AsInt(1);

                treatList.Add(new TamingItem()
                {
                    name = name,
                    domain = domain,
                    progress = progress,
                    cooldown = cooldown
                });
            }

            if (!String.IsNullOrEmpty(attributes["tameEntityCode"].AsString()))
            {
                tameEntityCode = AssetLocation.Create(attributes["tameEntityCode"].AsString());
            }

            var nestSize = EnumNestSize.SMALL;
            Enum.TryParse<EnumNestSize>(attributes["size"]?.AsString()?.ToUpper(), out nestSize);
            size = nestSize;

            disobediencePerDay = attributes["disobediencePerDay"].AsFloat(0f);
            listenerId = entity.World.RegisterGameTickListener(disobey, 60000);
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
            EntityPlayer player = byEntity as EntityPlayer;

            if (player == null) return;
            if (cachedOwner != null && cachedOwner.PlayerUID != player.PlayerUID) return;
            if (mode != EnumInteractMode.Interact) return;
            if (!entity.Alive)
            {
                tryReviveWith(itemslot);
                return;
            }
            if (byEntity.Controls.Sneak) return;

            if (domesticationLevel == DomesticationLevel.WILD
                && itemslot?.Itemstack?.Collectible != null)
            {
                if (feedEntityIfPossible(itemslot, player))
                {
                    domesticationLevel = DomesticationLevel.TAMING;
                    ownerId = player.PlayerUID;
                    (player.Player as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("petai:message-startet-taming", entity.GetName(), Math.Round(domesticationProgress * 100, 2)), EnumChatType.Notification);
                }
            }
            else if (domesticationLevel == DomesticationLevel.TAMING
                && itemslot?.Itemstack?.Collectible != null)
            {
                if (feedEntityIfPossible(itemslot, player))
                {
                    (player.Player as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("petai:message-tended-to", entity.GetName(), Math.Round(domesticationProgress * 100, 2)), EnumChatType.Notification);
                }
                if (domesticationProgress >= 1f)
                {
                    domesticationLevel = DomesticationLevel.DOMESTICATED;
                    spawnTameVariant(1f);
                }
            }
            else if (domesticationLevel == DomesticationLevel.DOMESTICATED)
            {
                bool next = !attachAccessoryIfPossible(byEntity as EntityPlayer, itemslot);
                if (next)
                    next = !feedEntityIfPossible(itemslot, player);
            }

            if (itemslot?.Itemstack?.Collectible?.Code?.Path == "magicbone")
            {
                domesticationLevel = DomesticationLevel.DOMESTICATED;
                obedience = 1;
                ownerId = (byEntity as EntityPlayer)?.PlayerUID;
                spawnTameVariant(1f);
            }
        }

        public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
        {
            // We need to Handle the Clientpacket here as we cannot prevent subsequent packet handling when using the Entitymethod directly
            if (entity.Alive && packetid < 1000 && (entity is EntityPet))
            {
                var inv = (entity as EntityPet)?.backpackInv as InventorySlotBound;
                inv.reloadFromSlots();
                inv.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                inv.saveAllSlots();
                handled = EnumHandling.PreventSubsequent;
                for (int i = 0; i < inv.Count; i++)
                {
                    if (inv[i].Empty) { continue; }

                    inv.MarkSlotDirty(i);
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

            var message = new PetProfileMessage();
            message.targetEntityUID = tameEntity.EntityId;
            message.oldEntityUID = entity.EntityId;

            (entity.Api as ICoreServerAPI)?.Network.GetChannel("petainetwork").SendPacket<PetProfileMessage>(message, entity.GetBehavior<EntityBehaviorTameable>()?.cachedOwner as IServerPlayer);
        }

        bool isValidTamingItem(TamingItem item, ItemSlot slot)
        {
            if (item.name.EndsWith("*"))
            {
                return slot.Itemstack?.Collectible?.Code.Path.StartsWith(item.name.Remove(item.name.Length - 1)) == true;
            }
            else
            {
                return slot.Itemstack?.Collectible?.Code.Path == item.name;
            }
        }

        bool checkTamingSuccess(TamingItem tamingItem, ItemSlot itemSlot, EntityPlayer player)
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

                if (domesticationLevel == DomesticationLevel.DOMESTICATED) obedience += tamingItem.progress * PetConfig.Current.Difficulty.obedienceMultiplier * (float)Math.Pow(1 + PetConfig.Current.Difficulty.obedienceMultiplierIncreasePerGen, generation);
                else domesticationProgress += tamingItem.progress * PetConfig.Current.Difficulty.tamingMultiplier * (float)Math.Pow(1 + PetConfig.Current.Difficulty.tamingMultiplierIncreasePerGen, generation);

                cooldown = entity.World.Calendar.TotalHours + tamingItem.cooldown;

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

        private bool attachAccessoryIfPossible(EntityPlayer byEntity, ItemSlot slot)
        {
            if (cachedOwner == null || cachedOwner.PlayerUID != byEntity?.PlayerUID) return false;
            var item = slot?.Itemstack?.Item;
            var pet = entity as EntityPet;
            if (pet != null && item is ItemPetAccessory)
            {
                return slot.TryFlipWith(pet.GearInventory.GetBestSuitedSlot(slot)?.slot);
            }
            return false;
        }

        private bool feedEntityIfPossible(ItemSlot foodsource, EntityPlayer player)
        {
            var tamingItem = treatList.Find((item) => isValidTamingItem(item, foodsource));
            return checkTamingSuccess(tamingItem, foodsource, player);
        }

        private void disobey(float intervall)
        {
            // if players are offline for multiple days they should not loose all pet progress
            double hoursPassed = Math.Min(24, entity.World.Calendar.TotalHours - disobedienceTime);

            // I hope the PlayerEntity is null when the player is offline
            if (cachedOwner?.Entity != null)
            {
                obedience -= PetConfig.Current.Difficulty.disobedienceMultiplier * disobediencePerDay * ((float)(hoursPassed / 24)) * (float)Math.Pow(1 - PetConfig.Current.Difficulty.disobedienceMultiplierDecreasePerGen, generation);
            }
            disobedienceTime = entity.World.Calendar.TotalHours;
        }

        private void tryReviveWith(ItemSlot itemslot)
        {
            var item = PetConfig.Current.Resurrectors.Find(resurrector => resurrector.name == itemslot?.Itemstack?.Collectible?.Code?.Path);
            if (item != null && entity.GetBehavior<EntityBehaviorHarvestable>()?.IsHarvested != true)
            {
                entity.Revive();
                itemslot.TakeOut(1);
                itemslot.MarkDirty();
                if (entity.HasBehavior<EntityBehaviorHealth>())
                {
                    entity.GetBehavior<EntityBehaviorHealth>().Health = item.healingValue;
                }
            }
        }
        public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
        {
            List<ItemStack> treats = new List<ItemStack>();
            foreach (var treat in treatList)
            {
                var item = world.GetItem(new AssetLocation(treat.domain + ":" + treat.name));
                if (item != null)
                {
                    treats.Add(new ItemStack(item));
                }
                var block = world.GetBlock(new AssetLocation(treat.domain + ":" + treat.name));
                if (block != null)
                {
                    treats.Add(new ItemStack(block));
                }
            }
            List<ItemStack> resurrectors = new List<ItemStack>();
            foreach (var resurrector in PetConfig.Current.Resurrectors)
            {
                var item = world.GetItem(new AssetLocation(resurrector.domain + ":" + resurrector.name));
                if (item != null)
                {
                    resurrectors.Add(new ItemStack(item));
                }
                var block = world.GetBlock(new AssetLocation(resurrector.domain + ":" + resurrector.name));
                if (block != null)
                {
                    resurrectors.Add(new ItemStack(block));
                }
            }
            if (entity.Alive && treats.Count > 0 && (string.IsNullOrEmpty(ownerId) || player.PlayerUID == ownerId))
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "petai:interact-feed",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = treats.ToArray()
                    }
                };
            }
            else if (!entity.Alive && entity.GetBehavior<EntityBehaviorHarvestable>()?.IsHarvested != true && resurrectors.Count > 0)
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "petai:interact-revive",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = resurrectors.ToArray()
                    }
                };
            }
            else
            {
                return base.GetInteractionHelp(world, es, player, ref handled);
            }
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);
            if (!string.IsNullOrEmpty(ownerId) && entity.Api is ICoreServerAPI sapi)
            {
                sapi.SendMessage(cachedOwner,
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
                        Position = entity.ServerPos.XYZ,
                        OwningPlayerUid = ownerId,
                        Title = Lang.Get("petai:message-pet-dead", entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName),
                    },
                        cachedOwner as IServerPlayer);
                }
            }
        }
    }

    public class TamingItem
    {
        public string name { get; set; }

        public string domain { get; set; }
        public float progress { get; set; }
        public long cooldown { get; set; }
    }
}