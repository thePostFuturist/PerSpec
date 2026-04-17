using System.Collections.Generic;

namespace PerSpec.UnityHelper.Editor
{
    public static class TaskExecutorRegistry
    {
        private static readonly Dictionary<BaseTaskExecutor.ExecutorType, ITaskExecutor> _executors = new();

        public static void Register(ITaskExecutor executor)
        {
            _executors[executor.Type] = executor;
        }

        public static bool ExecuteTask(Task task)
        {
            if (_executors.TryGetValue(task.type, out var executor))
            {
                return executor.Execute(task);
            }

            task.error = $"No executor registered for type: {task.type}";
            return false;
        }
    }
}
