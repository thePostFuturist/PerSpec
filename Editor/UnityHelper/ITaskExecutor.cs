using System;
using System.Collections.Generic;

namespace PerSpec.UnityHelper.Editor
{
    public interface ITaskExecutor
    {
        BaseTaskExecutor.ExecutorType Type { get; }
        bool Execute(Task task);
    }
}
