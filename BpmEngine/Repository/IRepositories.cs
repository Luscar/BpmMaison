using BpmEngine.Core.Models;

namespace BpmEngine.Repository;

public interface IProcessDefinitionRepository
{
    Task<ProcessDefinition?> GetByIdAsync(string processId, int? version = null);
    Task<ProcessDefinition> SaveAsync(ProcessDefinition definition);
    Task<List<ProcessDefinition>> GetAllAsync();
    Task<List<ProcessDefinition>> GetVersionsAsync(string processId);
}

public interface IProcessInstanceRepository
{
    Task<ProcessInstance?> GetByIdAsync(string instanceId);
    Task<ProcessInstance> CreateAsync(ProcessInstance instance);
    Task<ProcessInstance> UpdateAsync(ProcessInstance instance);
    Task<List<ProcessInstance>> GetByStatusAsync(ProcessStatus status);
    Task<List<ProcessInstance>> GetByDefinitionIdAsync(string definitionId);
}

public interface IStepInstanceRepository
{
    Task<StepInstance?> GetByIdAsync(string stepInstanceId);
    Task<StepInstance> CreateAsync(StepInstance instance);
    Task<StepInstance> UpdateAsync(StepInstance instance);
    Task<List<StepInstance>> GetByProcessInstanceIdAsync(string processInstanceId);
    Task<List<StepInstance>> GetScheduledStepsAsync();
    Task<List<StepInstance>> GetWaitingForSignalAsync(string signalName);
}

public interface ITaskRepository
{
    Task<TaskInstance?> GetByIdAsync(string taskId);
    Task<TaskInstance> CreateAsync(TaskInstance task);
    Task<TaskInstance> UpdateAsync(TaskInstance task);
    Task<List<TaskInstance>> GetByProcessInstanceIdAsync(string processInstanceId);
    Task<List<TaskInstance>> GetPendingByRoleAsync(string role);
    Task<List<TaskInstance>> GetByUserIdAsync(string userId);
}
