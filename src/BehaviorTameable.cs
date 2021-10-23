using System.Text;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API;
using Vintagestory.Essentials;
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
                if (Enum.TryParse<DomesticationLevel>(entity.WatchedAttributes.GetString("domesticationLevel"), out level))
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
                entity.WatchedAttributes.SetString("domesticationLevel", value.ToString());
            }
        }

        public IPlayer owner
        {
            get
            {
                return entity.World.PlayerByUid(entity.WatchedAttributes.GetString("owner"));
            }
            set
            {
                entity.WatchedAttributes.SetString("owner", value.PlayerUID);
            }
        }
        public EntityBehaviorTameable(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "tameable";
        }
    }
}