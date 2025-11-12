using BpmEngine.Core;
using BpmEngine.Core.Models;
using BpmEngine.Handlers;
using BpmEngine.Repository;

namespace BpmEngine.Engine;

public class ProcessEngine
{
    private readonly IProcessDefinitionRepository _processDefinitionRepository;
    private readonly IProcessInstanceRepository _processInstanceRepository;
    private readonly IStepInstanceRepository _stepInstanceRepository;
    private readonly Dictionary<StepType, IStepHandler> _stepHandlers;

    public ProcessEngine(
        IProcessDefinitionRepository processDefinitionRepository,
        IProcessInstanceRepository processInstanceRepository,
        IStepInstanceRepository stepInstanceRepository,
        IEnumerable<IStepHandler> stepHandlers)
    {
        _processDefinitionRepository = processDefinitionRepository;
        _processInstanceRepository = processInstanceRepository;
        _stepInstanceRepository = stepInstanceRepository;
        _stepHandlers = stepHandlers.ToDictionary(h => h.SupportedType);
    }

    public async Task<string> StartProcessAsync(
        string processDefinitionId,
        Dictionary<string, object>? initialVariables = null,
        int? version = null)
    {
        var definition = await _processDefinitionRepository.GetByIdAsync(processDefinitionId, version);
        
        if (definition == null)
            throw new InvalidOperationException($"Définition de processus introuvable: {processDefinitionId}");

        var instance = new ProcessInstance
        {
            Id = Guid.NewGuid().ToString(),
            ProcessDefinitionId = definition.Id,
            ProcessVersion = definition.Version,
            Status = ProcessStatus.Running,
            StartedAt = DateTime.UtcNow,
            CurrentStepId = definition.StartStepId,
            Variables = initialVariables ?? new Dictionary<string, object>()
        };

        await _processInstanceRepository.CreateAsync(instance);
        await ExecuteProcessAsync(instance.Id);

        return instance.Id;
    }

    public async Task ExecuteProcessAsync(string processInstanceId)
    {
        var instance = await _processInstanceRepository.GetByIdAsync(processInstanceId);
        
        if (instance == null)
            throw new InvalidOperationException($"Instance de processus introuvable: {processInstanceId}");

        if (instance.Status != ProcessStatus.Running && instance.Status != ProcessStatus.Waiting)
            return;

        var definition = await _processDefinitionRepository.GetByIdAsync(
            instance.ProcessDefinitionId,
            instance.ProcessVersion);

        if (definition == null)
            throw new InvalidOperationException($"Définition de processus introuvable: {instance.ProcessDefinitionId}");

        instance.Status = ProcessStatus.Running;
        await _processInstanceRepository.UpdateAsync(instance);

        while (instance.CurrentStepId != null)
        {
            var stepDefinition = definition.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
            
            if (stepDefinition == null)
            {
                instance.Status = ProcessStatus.Failed;
                instance.ErrorMessage = $"Étape introuvable: {instance.CurrentStepId}";
                instance.CompletedAt = DateTime.UtcNow;
                await _processInstanceRepository.UpdateAsync(instance);
                return;
            }

            var existingStepInstance = (await _stepInstanceRepository.GetByProcessInstanceIdAsync(instance.Id))
                .FirstOrDefault(s => s.StepDefinitionId == stepDefinition.Id && s.Status != StepStatus.Completed);

            var stepInstance = existingStepInstance ?? new StepInstance
            {
                Id = Guid.NewGuid().ToString(),
                ProcessInstanceId = instance.Id,
                StepDefinitionId = stepDefinition.Id,
                StepType = stepDefinition.Type,
                Status = StepStatus.NotStarted,
                StartedAt = DateTime.UtcNow,
                InputData = new Dictionary<string, object>(instance.Variables)
            };

            if (existingStepInstance == null)
            {
                await _stepInstanceRepository.CreateAsync(stepInstance);
            }

            stepInstance.Status = StepStatus.Running;
            await _stepInstanceRepository.UpdateAsync(stepInstance);

            if (!_stepHandlers.TryGetValue(stepDefinition.Type, out var handler))
            {
                instance.Status = ProcessStatus.Failed;
                instance.ErrorMessage = $"Handler introuvable pour le type: {stepDefinition.Type}";
                instance.CompletedAt = DateTime.UtcNow;
                await _processInstanceRepository.UpdateAsync(instance);
                return;
            }

            var executionResult = await handler.ExecuteAsync(stepDefinition, instance, stepInstance);

            if (executionResult.RequiresWait)
            {
                if (executionResult.ResumeAt.HasValue)
                {
                    stepInstance.Status = StepStatus.WaitingForSchedule;
                    stepInstance.ScheduledFor = executionResult.ResumeAt;
                }
                else if (!string.IsNullOrEmpty(executionResult.WaitingForSignal))
                {
                    stepInstance.Status = StepStatus.WaitingForSignal;
                    stepInstance.WaitingForSignal = executionResult.WaitingForSignal;
                }
                else
                {
                    stepInstance.Status = StepStatus.WaitingForTask;
                }

                await _stepInstanceRepository.UpdateAsync(stepInstance);
                
                instance.Status = ProcessStatus.Waiting;
                await _processInstanceRepository.UpdateAsync(instance);
                return;
            }

            if (!executionResult.IsCompleted)
            {
                stepInstance.Status = StepStatus.Failed;
                stepInstance.ErrorMessage = executionResult.ErrorMessage;
                stepInstance.CompletedAt = DateTime.UtcNow;
                await _stepInstanceRepository.UpdateAsync(stepInstance);

                instance.Status = ProcessStatus.Failed;
                instance.ErrorMessage = executionResult.ErrorMessage;
                instance.CompletedAt = DateTime.UtcNow;
                await _processInstanceRepository.UpdateAsync(instance);
                return;
            }

            stepInstance.Status = StepStatus.Completed;
            stepInstance.OutputData = executionResult.OutputData;
            stepInstance.CompletedAt = DateTime.UtcNow;
            await _stepInstanceRepository.UpdateAsync(stepInstance);

            foreach (var output in executionResult.OutputData)
            {
                instance.Variables[output.Key] = output.Value;
            }

            instance.CurrentStepId = executionResult.NextStepId;
            await _processInstanceRepository.UpdateAsync(instance);
        }

        instance.Status = ProcessStatus.Completed;
        instance.CompletedAt = DateTime.UtcNow;
        await _processInstanceRepository.UpdateAsync(instance);
    }

    public async Task CompleteTaskAsync(string taskId, Dictionary<string, object> completionData)
    {
        var tasks = await _stepInstanceRepository.GetByProcessInstanceIdAsync(taskId);
        // Cette méthode devrait être complétée avec la logique appropriée pour trouver la tâche
        // et reprendre le processus
    }

    public async Task SendSignalAsync(string signalName, string? processInstanceId = null)
    {
        List<StepInstance> waitingSteps;

        if (processInstanceId != null)
        {
            var allSteps = await _stepInstanceRepository.GetByProcessInstanceIdAsync(processInstanceId);
            waitingSteps = allSteps
                .Where(s => s.Status == StepStatus.WaitingForSignal && s.WaitingForSignal == signalName)
                .ToList();
        }
        else
        {
            waitingSteps = await _stepInstanceRepository.GetWaitingForSignalAsync(signalName);
        }

        foreach (var step in waitingSteps)
        {
            step.Status = StepStatus.Completed;
            step.CompletedAt = DateTime.UtcNow;
            await _stepInstanceRepository.UpdateAsync(step);

            await ExecuteProcessAsync(step.ProcessInstanceId);
        }
    }

    public async Task ProcessScheduledStepsAsync()
    {
        var scheduledSteps = await _stepInstanceRepository.GetScheduledStepsAsync();
        var now = DateTime.UtcNow;

        foreach (var step in scheduledSteps.Where(s => s.ScheduledFor <= now))
        {
            await ExecuteProcessAsync(step.ProcessInstanceId);
        }
    }
}
