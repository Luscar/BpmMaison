using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BpmEngine.Core.Models;
using BpmEngine.Services;
using BpmEngine.Repository;

namespace BpmClient.Services
{
    /// <summary>
    /// Task service implementation
    /// Creates and manages user tasks
    /// </summary>
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;

        public TaskService(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public async Task<TaskInstance> CreateTaskAsync(
            string processInstanceId,
            string stepInstanceId,
            string taskType,
            string defaultRole,
            Dictionary<string, object> taskData)
        {
            var task = new TaskInstance
            {
                Id = Guid.NewGuid().ToString(),
                ProcessInstanceId = processInstanceId,
                StepInstanceId = stepInstanceId,
                TaskType = taskType,
                Status = TaskStatus.Pending,
                AssignedRole = defaultRole,
                TaskData = taskData ?? new Dictionary<string, object>(),
                CreatedDate = DateTime.UtcNow
            };

            await _taskRepository.SaveAsync(task);

            Console.WriteLine($"[TaskService] Created task {task.Id} of type '{taskType}' assigned to role '{defaultRole}'");

            return task;
        }

        public async Task<TaskInstance> GetTaskAsync(string taskId)
        {
            return await _taskRepository.GetByIdAsync(taskId);
        }

        public async Task<bool> CompleteTaskAsync(string taskId, Dictionary<string, object> result)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);

            if (task == null)
            {
                Console.WriteLine($"[TaskService] Task {taskId} not found");
                return false;
            }

            if (task.Status != TaskStatus.Pending && task.Status != TaskStatus.InProgress)
            {
                Console.WriteLine($"[TaskService] Task {taskId} is not in a completable state (status: {task.Status})");
                return false;
            }

            task.Status = TaskStatus.Completed;
            task.Result = result;
            task.CompletedDate = DateTime.UtcNow;

            await _taskRepository.SaveAsync(task);

            Console.WriteLine($"[TaskService] Completed task {taskId}");

            return true;
        }

        public async Task AssignTaskAsync(string taskId, string userId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);

            if (task == null)
            {
                throw new InvalidOperationException($"Task {taskId} not found");
            }

            task.AssignedUser = userId;
            task.Status = TaskStatus.InProgress;

            await _taskRepository.SaveAsync(task);

            Console.WriteLine($"[TaskService] Assigned task {taskId} to user '{userId}'");
        }
    }
}
