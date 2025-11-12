using BpmEngine.Core;
using BpmEngine.Core.Models;
using BpmEngine.Repository;
using BpmEngine.Services;

namespace BpmEngine.Handlers;

public class InteractiveStepHandler : IStepHandler
{
    private readonly ITaskService _taskService;
    private readonly ITaskRepository _taskRepository;

    public StepType SupportedType => StepType.Interactive;

    public InteractiveStepHandler(
        ITaskService taskService,
        ITaskRepository taskRepository)
    {
        _taskService = taskService;
        _taskRepository = taskRepository;
    }

    public async Task<StepExecutionResult> ExecuteAsync(
        StepDefinition stepDefinition,
        ProcessInstance processInstance,
        StepInstance stepInstance)
    {
        var interactiveStep = (InteractiveStepDefinition)stepDefinition;
        var result = new StepExecutionResult();

        if (stepInstance.Status == StepStatus.NotStarted || stepInstance.Status == StepStatus.Running)
        {
            var taskData = MergeTaskData(interactiveStep.TaskData, processInstance.Variables);

            var taskId = await _taskService.CreateTaskAsync(
                processInstance.Id,
                stepInstance.Id,
                interactiveStep.TaskType,
                interactiveStep.DefaultRole,
                taskData);

            var task = new TaskInstance
            {
                Id = taskId,
                ProcessInstanceId = processInstance.Id,
                StepInstanceId = stepInstance.Id,
                TaskType = interactiveStep.TaskType,
                AssignedRole = interactiveStep.DefaultRole,
                CreatedAt = DateTime.UtcNow,
                TaskData = taskData,
                IsCompleted = false
            };

            await _taskRepository.CreateAsync(task);

            result.RequiresWait = true;
            result.IsCompleted = false;
        }
        else if (stepInstance.Status == StepStatus.WaitingForTask)
        {
            var tasks = await _taskRepository.GetByProcessInstanceIdAsync(processInstance.Id);
            var task = tasks.FirstOrDefault(t => t.StepInstanceId == stepInstance.Id);

            if (task?.IsCompleted == true)
            {
                result.IsCompleted = true;
                result.OutputData = task.CompletionData ?? new Dictionary<string, object>();
                result.NextStepId = interactiveStep.NextStepId;
            }
            else
            {
                result.RequiresWait = true;
                result.IsCompleted = false;
            }
        }

        return result;
    }

    private Dictionary<string, object> MergeTaskData(
        Dictionary<string, object>? taskData,
        Dictionary<string, object> variables)
    {
        var merged = new Dictionary<string, object>(variables);
        
        if (taskData != null)
        {
            foreach (var data in taskData)
            {
                merged[data.Key] = data.Value;
            }
        }

        return merged;
    }
}
