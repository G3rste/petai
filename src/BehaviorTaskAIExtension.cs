using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.Essentials;
using Vintagestory.GameContent;

namespace WolfTaming
{
    public class EntityBehaviorTaskAIExtension : EntityBehaviorTaskAI
    {

        List<IAiTask> wildTasks = new List<IAiTask>();
        List<IAiTask> tamingTasks = new List<IAiTask>();
        List<IAiTask> domesticatedTasks = new List<IAiTask>();
        public EntityBehaviorTaskAIExtension(Entity entity) : base(entity)
        {
        }

        public override void Initialize(EntityProperties properties, JsonObject aiconfig)
        {
            if (!(entity is EntityAgent))
            {
                entity.World.Logger.Error("Entity {0} is not of Type EntityAgent!", entity.Code);
            }

            PathTraverser = new WaypointsTraverser(entity as EntityAgent);

            parseTasks(wildTasks, "aitaskswild", aiconfig);
            parseTasks(tamingTasks, "aitaskstaming", aiconfig);
            parseTasks(domesticatedTasks, "aitasksdomesticated", aiconfig);

            fillTAskManager();
        }

        public void reloadTasks()
        {
            foreach (var task in wildTasks)
            {
                taskManager.RemoveTask(task);
            }
            foreach (var task in tamingTasks)
            {
                taskManager.RemoveTask(task);
            }
            foreach (var task in domesticatedTasks)
            {
                taskManager.RemoveTask(task);
            }
            fillTAskManager();
        }

        private void parseTasks(List<IAiTask> taskList, string domesticationLevel, JsonObject aiconfig)
        {
            JsonObject[] tasks = aiconfig[domesticationLevel]?.AsArray();
            if (tasks == null) return;

            foreach (JsonObject taskConfig in tasks)
            {
                string taskCode = taskConfig["code"]?.AsString();
                Type taskType = null;
                if (!AiTaskRegistry.TaskTypes.TryGetValue(taskCode, out taskType))
                {
                    entity.World.Logger.Error("Task with code {0} for entity {1} does not exist.", taskCode, entity.Code);
                    continue;
                }

                IAiTask task = (IAiTask)Activator.CreateInstance(taskType, (EntityAgent)entity);

                try
                {
                    task.LoadConfig(taskConfig, aiconfig);
                }
                catch (Exception e)
                {
                    entity.World.Logger.Error("Task with code {0} for entity {1} could not be loaded", taskCode, entity.Code);
                    throw e;
                }

                taskList.Add(task);
            }
        }

        private void fillTAskManager(){
            List<IAiTask> taskList;
            switch (entity.GetBehavior<EntityBehaviorTameable>().domesticationLevel)
            {
                case DomesticationLevel.WILD:
                    taskList = wildTasks;
                    break;
                case DomesticationLevel.TAMING:
                    taskList = tamingTasks;
                    break;
                case DomesticationLevel.DOMESTICATED:
                    taskList = domesticatedTasks;
                    break;
                default:
                    taskList = null;
                    break;
            }
            if (taskList == null) return;

            foreach (var task in taskList)
            {
                taskManager.AddTask(task);
            }
        }
    }
}