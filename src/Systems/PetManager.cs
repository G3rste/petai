using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace PetAI
{
    public class PetManager : ModSystem
    {

        private ConcurrentDictionary<long, PetData> petMap;
        private ICoreServerAPI sapi;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;
            api.Event.SaveGameLoaded += OnLoad;
            api.Event.GameWorldSave += OnSave;

            api.RegisterCommand("petmanager", "Admin tool to manage player pets.", "[forcerespawn|delete|list|rorateTexture]", onCmdPetManager, Privilege.controlserver);
        }

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public void UpdatePet(Entity pet, bool hasDied = false)
        {
            var tameable = pet.GetBehavior<EntityBehaviorTameable>();
            if (tameable == null || String.IsNullOrEmpty(tameable.ownerId))
            {
                Remove(pet.EntityId);
                return;
            }

            var petData = petMap.GetOrAdd(pet.EntityId, new PetData());
            petData.alive = !(bool)hasDied;
            petData.ownerId = tameable.ownerId;
            petData.petId = pet.EntityId;
            petData.petClass = sapi.World.ClassRegistry.GetEntityClassName(pet.GetType());
            petData.petName = pet.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
            petData.petType = String.Format("{0}:item-creature-{1}", pet.Code.Domain, pet.Code.Path);
            petData.requiredNestSize = tameable.size;

            if ((bool)hasDied)
            {
                petData.deadUntil = sapi.World.Calendar.TotalHours + PetConfig.Current.petRespawnCooldown;
            }
            else
            {
                petData.deadUntil = 0;
            }

            if (pet.Alive)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
                    {
                        pet.ToBytes(writer, false);
                        writer.Flush();
                        petData.deadPetBytes = ms.ToArray();
                    }
                }
            }
        }

        public long? RevivePet(long petId, BlockPos pos)
        {
            PetData data;
            petMap.TryGetValue(petId, out data);
            if (data?.deadPetBytes == null)
            {
                sapi.Logger.Warning("Could not revive entity with id={0} because it could not be found.", petId);
                return null;
            }
            using (MemoryStream ms = new MemoryStream(data.deadPetBytes))
            {
                using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
                {
                    Entity pet = sapi.World.ClassRegistry.CreateEntity(data.petClass);
                    pet.FromBytes(reader, false);
                    sapi.World.SpawnEntity(pet);
                    pet.ServerPos.SetPos(pos.ToVec3d());
                    return pet.EntityId;
                }
            }
        }

        public List<PetDataSmall> GetPetsForPlayer(string playerUID)
        {
            List<PetDataSmall> list = new List<PetDataSmall>();
            foreach (var pet in petMap.Values)
            {
                if (pet.ownerId == playerUID)
                {
                    var data = new PetDataSmall();
                    data.ownerId = pet.ownerId;
                    data.petId = pet.petId;
                    data.petName = pet.petName;
                    data.petType = pet.petType;
                    data.requiredNestSize = pet.requiredNestSize;
                    list.Add(data);
                }
            }
            return list;
        }

        public PetData GetPet(long petId)
        {
            PetData data = null;
            petMap.TryGetValue(petId, out data);
            return data;
        }

        private void OnSave()
        {
            sapi.WorldManager.SaveGame.StoreData("petmanager", SerializerUtil.Serialize(petMap));
        }

        private void OnLoad()
        {
            byte[] data = sapi.WorldManager.SaveGame.GetData("petmanager");
            petMap = data == null ? new ConcurrentDictionary<long, PetData>() : SerializerUtil.Deserialize<ConcurrentDictionary<long, PetData>>(data);
        }

        public void SetPetNest(long petId, BlockPos nestPos)
        {
            if (petMap.ContainsKey(petId))
            {
                var data = petMap[petId];
                data.nestLocation = nestPos;
            }
            else
            {
                sapi.Logger.Warning("A pet with id={0} has been tried to assign to nest at x={1}, y={2}, z={3}, but no pet with this id is known!"
                    , petId, nestPos.X, nestPos.Y, nestPos.Z);
            }
        }

        public void Remove(long petId)
        {
            if (petMap.ContainsKey(petId))
            {
                petMap.Remove(petId);
            }
        }

        public BlockPos GetNestPos(long petId)
        {
            PetData data;
            petMap.TryGetValue(petId, out data);
            return data?.nestLocation;
        }

        private void onCmdPetManager(IServerPlayer player, int groupId, CmdArgs args)
        {
            var command = args.PopWord();
            switch (command)
            {
                case "list":
                    var playerName = args.PopWord();
                    var pets = GetPetsForPlayer(sapi.PlayerData.GetPlayerDataByLastKnownName(playerName)?.PlayerUID);
                    StringBuilder builder = new StringBuilder();
                    foreach (var pet in pets)
                    {
                        string name = !String.IsNullOrEmpty(pet.petName) ? pet.petName : pet.petType;
                        builder.AppendFormat("Pet: {0}, PetId: {1}\n", name, pet.petId);
                    }
                    sapi.SendMessage(player, GlobalConstants.GeneralChatGroup, String.Format("Pets for player {0}:\n{1}", playerName, builder.ToString()), EnumChatType.CommandSuccess);
                    break;
                case "delete":
                    Remove((long)args.PopLong(0));
                    break;
                case "forcerespawn":
                    PetData data;
                    long petId = (long)args.PopLong(0);
                    petMap.TryGetValue(petId, out data);
                    if (data == null)
                    {
                        sapi.SendMessage(player, GlobalConstants.GeneralChatGroup, String.Format("There is no pet listed with id {0}", petId), EnumChatType.CommandError);
                        return;
                    }
                    if (RevivePet(petId, player.Entity.ServerPos.AsBlockPos) == null)
                    {
                        sapi.SendMessage(player, GlobalConstants.GeneralChatGroup, String.Format("Was unable to revive pet with id {0}, going to create a new one. This might will duplicate the old pet, if it has not died but is still out there somewhere.", petId), EnumChatType.CommandSuccess);

                        Entity pet = sapi.World.ClassRegistry.CreateEntity(sapi.World.GetEntityType(new AssetLocation(data.petType.Replace("item-creature-", ""))));
                        pet.ServerPos.SetPos(player.Entity.ServerPos.XYZ);
                        pet.ServerPos.Yaw = player.Entity.ServerPos.Yaw + GameMath.PI;
                        pet.Pos.SetFrom(pet.ServerPos);
                        sapi.World.SpawnEntity(pet);
                        var tameable = pet.GetBehavior<EntityBehaviorTameable>();
                        if (tameable != null)
                        {
                            tameable.domesticationLevel = DomesticationLevel.DOMESTICATED;
                            tameable.obedience = 1;
                            tameable.ownerId = data.ownerId;
                        }
                        pet.GetBehavior<EntityBehaviorNameTag>()?.SetName(data.petName);
                        UpdatePet(pet);
                        Remove(petId);
                    }
                    break;
                case "rotateTexture":
                    var textureMorpher = sapi.World.GetEntityById((long)args.PopLong(0));
                    if (textureMorpher != null)
                    {
                        int textures = 1;
                        var alternates = textureMorpher.Properties.Client.FirstTexture.Alternates;
                        if (alternates != null)
                        {
                            textures += alternates.Length;
                        }
                        int oldtexture = textureMorpher.WatchedAttributes.GetInt("textureIndex", 0);
                        textureMorpher.WatchedAttributes.SetInt("textureIndex", (oldtexture + 1) % textures);
                        textureMorpher.WatchedAttributes.MarkPathDirty("textureIndex");
                        sapi.World.SpawnEntity(PetUtil.EntityFromTree(PetUtil.EntityToTree(textureMorpher), sapi.World));
                        textureMorpher.Die(EnumDespawnReason.Removed);
                        Remove(textureMorpher.EntityId);
                    }
                    break;
            }
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class PetData
    {
        public string ownerId;
        public long petId;
        public string petType;
        public string petName;

        public string petClass;
        public bool alive;
        public EnumNestSize requiredNestSize;
        public BlockPos nestLocation;
        public double deadUntil;
        public byte[] deadPetBytes;
    }
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class PetDataSmall
    {
        public string ownerId;
        public long petId;
        public string petType;
        public string petName;
        public EnumNestSize requiredNestSize;
    }

    public enum EnumPetManagerCommand
    {
        forcerespawn, delete, list
    }
}