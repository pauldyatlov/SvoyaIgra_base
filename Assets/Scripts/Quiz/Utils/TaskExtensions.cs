using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Quiz.Utils
{
    internal static class TaskExtensions
    {
        private static readonly Action<Task> _handleFinishedTask = HandleFinishedTask;

        internal static void HandleExceptions(this Task task)
        {
            task.ContinueWith(_handleFinishedTask);
        }

        private static void HandleFinishedTask(Task task)
        {
            if (task.IsFaulted)
                Debug.LogException(task.Exception);
        }
    }
}