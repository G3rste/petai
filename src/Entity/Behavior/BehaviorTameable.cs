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

        public IPlayer owner { get => entity.World.PlayerByUid(ownerId); set => ownerId = value?.PlayerUID; }

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
        List<TamingItem> treatList = new List<TamingItem>();
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
                float progress = item["progress"].AsFloat(1f);
                long cooldown = item["cooldown"].AsInt(1);

                treatList.Add(new TamingItem(name, progress, cooldown));
            }

            if (!String.IsNullOrEmpty(attributes["tameEntityCode"].AsString()))
            {
                tameEntityCode = AssetLocation.Create(attributes["tameEntityCode"].AsString());
            }

            disobediencePerDay = attributes["disobediencePerDay"].AsFloat(0f);
            listenerId = entity.World.RegisterGameTickListener(disobey, 60000);

            entity.Api.ModLoader.GetModSystem<PetManager>()?.UpdatePet(entity, !entity.Alive);
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
            EntityPlayer player = byEntity as EntityPlayer;

            if (player == null) return;
            if (owner != null && owner.PlayerUID != player.PlayerUID) return;
            if (mode != EnumInteractMode.Interact) return;
            if (!entity.Alive)
            {
                tryReviveWith(itemslot);
                return;
            }
            if (byEntity.Controls.Sneak) return;

            if (domesticationLevel == DomesticationLevel.WILD
                && itemslot?.Itemstack?.Item != null)
            {
                if (feedEntityIfPossible(itemslot))
                {
                    domesticationLevel = DomesticationLevel.TAMING;
                    owner = player.Player;
                    (entity.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get("petai:message-startet-taming", entity.GetName(), Math.Round(domesticationProgress * 100, 2)));
                }
            }
            else if (domesticationLevel == DomesticationLevel.TAMING
                && itemslot?.Itemstack?.Item != null)
            {
                if (feedEntityIfPossible(itemslot))
                {
                    (entity.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get("petai:message-tended-to", entity.GetName(), Math.Round(domesticationProgress * 100, 2)));
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
                    next = !feedEntityIfPossible(itemslot);
            }

            if (itemslot?.Itemstack?.Item?.Code?.Path == "magicbone")
            {
                domesticationLevel = DomesticationLevel.DOMESTICATED;
                obedience = 1;
                owner = (byEntity as EntityPlayer)?.Player;
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

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            if (owner != null && PetConfig.Current.respawningPets.Contains(entity.Code.Path))
            {
                entity.Revive();
                Vec3d pos = entity.Pos.XYZ;

                SimpleParticleProperties smoke = new SimpleParticleProperties(
                        100, 150,
                        ColorUtil.ToRgba(80, 100, 100, 100),
                        new Vec3d(),
                        new Vec3d(2, 1, 2),
                        new Vec3f(-0.25f, 0f, -0.25f),
                        new Vec3f(0.25f, 0f, 0.25f),
                        0.51f,
                        -0.075f,
                        0.5f,
                        3f,
                        EnumParticleModel.Quad
                    );

                smoke.MinPos = pos.AddCopy(-1.5, -0.5, -1.5);
                entity.World.SpawnParticles(smoke);
                entity.Api.ModLoader.GetModSystem<PetManager>()?.UpdatePet(entity, true);
                entity.Die(EnumDespawnReason.Removed);
            }
            else
            {
                PetData data;
                entity.Api.ModLoader.GetModSystem<PetManager>()?.petMap.TryRemove(entity.EntityId, out data);
                base.OnEntityDeath(damageSourceForDeath);
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

            (entity.Api as ICoreServerAPI)?.Network.GetChannel("petainetwork").SendPacket<PetProfileMessage>(message, entity.GetBehavior<EntityBehaviorTameable>()?.owner as IServerPlayer);
            (entity.Api as ICoreServerAPI)?.ModLoader.GetModSystem<PetManager>().UpdatePet(tameEntity);
        }

        bool isValidTamingItem(TamingItem item, ItemSlot slot)
        {
            if (item.name.EndsWith("*"))
            {
                return slot.Itemstack?.Item?.Code.Path.StartsWith(item.name.Remove(item.name.Length - 1)) == true;
            }
            else
            {
                return slot.Itemstack?.Item?.Code.Path == item.name;
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

                if (domesticationLevel == DomesticationLevel.DOMESTICATED) obedience += tamingItem.progress * PetConfig.Current.difficulty.obedienceMultiplier;
                else domesticationProgress += tamingItem.progress * PetConfig.Current.difficulty.tamingMultiplier;

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
                (entity.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get("petai:message-not-ready", entity.GetName()));
            }
            return false;
        }
        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            entity.World.UnregisterCallback(callbackId);
            entity.World.UnregisterGameTickListener(listenerId);

            if (entity.Alive 
                && (despawn.reason == EnumDespawnReason.Unload
                || despawn.reason == EnumDespawnReason.Disconnect
                || despawn.reason == EnumDespawnReason.OutOfRange))
            {
                entity.Api.ModLoader.GetModSystem<PetManager>()?.UpdatePet(entity, !entity.Alive);
            }
        }

        private bool attachAccessoryIfPossible(EntityPlayer byEntity, ItemSlot slot)
        {
            if (owner == null || owner.PlayerUID != byEntity?.PlayerUID) return false;
            var item = slot?.Itemstack?.Item;
            var pet = entity as EntityPet;
            if (pet != null && item is ItemPetAccessory)
            {
                return slot.TryFlipWith(pet.GearInventory.GetBestSuitedSlot(slot)?.slot);
            }
            return false;
        }

        private bool feedEntityIfPossible(ItemSlot foodsource)
        {
            var tamingItem = treatList.Find((item) => isValidTamingItem(item, foodsource));
            return checkTamingSuccess(tamingItem, foodsource);
        }

        private void disobey(float intervall)
        {
            double hoursPassed = entity.World.Calendar.TotalHours - disobedienceTime;

            obedience -= PetConfig.Current.difficulty.disobedienceMultiplier * disobediencePerDay * ((float)(hoursPassed / 24));
            disobedienceTime = entity.World.Calendar.TotalHours;
        }

        private void tryReviveWith(ItemSlot itemslot)
        {
            var item = PetConfig.Current.petResurrectors.Find(resurrector => resurrector.itemCode == itemslot?.Itemstack?.Item?.Code?.Path);
            if (item != null)
            {
                entity.Revive();
                itemslot.TakeOut(1);
                if (entity.HasBehavior<EntityBehaviorHealth>())
                {
                    entity.GetBehavior<EntityBehaviorHealth>().Health = item.healingValue;
                }
            }
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