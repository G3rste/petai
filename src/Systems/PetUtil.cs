using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System.IO;
using System.Text;
using Vintagestory.API.Datastructures;

namespace PetAI
{
    public class PetUtil
    {
        public static ITreeAttribute EntityToTree(Entity entity)
        {
            var entityTree = new TreeAttribute();
            entityTree.SetString("class", entity.Api.World.ClassRegistry.GetEntityClassName(entity.GetType()));
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
                {
                    entity.ToBytes(writer, false);
                    writer.Flush();
                    entityTree.SetBytes("pet", ms.ToArray());
                }
            }
            return entityTree;
        }

        public static Entity EntityFromTree(ITreeAttribute entityTree, IWorldAccessor world)
        {
            if (entityTree == null)
            {
                return null;
            }
            
            using (MemoryStream ms = new MemoryStream(entityTree.GetBytes("pet")))
            {
                using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
                {
                    Entity capturedEntity = world.ClassRegistry.CreateEntity(entityTree.GetString("class"));
                    capturedEntity.FromBytes(reader, false);
                    return capturedEntity;
                }
            }
        }
    }
}