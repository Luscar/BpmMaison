using BpmEngine.Core;
using BpmEngine.Core.Models;
using BpmEngine.Repository;

namespace BpmEngine.Handlers;

public class SubProcessStepHandler : IStepHandler
{
    private readonly IProcessDefinitionRepository _processDefinitionRepository;
    private readonly IProcessInstanceRepository _processInstanceRepository;

    public StepType SupportedType => StepType.SubProcess;

    public SubProcessStepHandler(
        IProcessDefinitionRepository processDefinitionRepository,
        IProcessInstanceRepository processInstanceRepository)
    {
        _processDefinitionRepository = processDefinitionRepository;
        _processInstanceRepository = processInstanceRepository;
    }

    public async Task<StepExecutionResult> ExecuteAsync(
        StepDefinition stepDefinition,
        ProcessInstance processInstance,
        StepInstance stepInstance)
    {
        var subProcessStep = (SubProcessStepDefinition)stepDefinition;
        var result = new StepExecutionResult();

        if (stepInstance.Status == StepStatus.NotStarted || stepInstance.Status == StepStatus.Running)
        {
            var subProcessDefinition = await _processDefinitionRepository.GetByIdAsync(
                subProcessStep.SubProcessId,
                subProcessStep.SubProcessVersion);

            if (subProcessDefinition == null)
            {
                result.IsCompleted = false;
                result.ErrorMessage = $"Sous-processus introuvable: {subProcessStep.SubProcessId}";
                return result;
            }

            var subProcessVariables = MapInputVariables(
                subProcessStep.InputMapping,
                processInstance.Variables);

            var subProcessInstance = new ProcessInstance
            {
                Id = Guid.NewGuid().ToString(),
                ProcessDefinitionId = subProcessDefinition.Id,
                ProcessVersion = subProcessDefinition.Version,
                Status = ProcessStatus.NotStarted,
                StartedAt = DateTime.UtcNow,
                Variables = subProcessVariables,
                ParentProcessInstanceId = processInstance.Id,
                CurrentStepId = subProcessDefinition.StartStepId
            };

            await _processInstanceRepository.CreateAsync(subProcessInstance);

            stepInstance.OutputData["SubProcessInstanceId"] = subProcessInstance.Id;

            result.RequiresWait = true;
            result.IsCompleted = false;
        }
        else if (stepInstance.Status == StepStatus.Running || stepInstance.Status == StepStatus.Waiting)
        {
            if (!stepInstance.OutputData.TryGetValue("SubProcessInstanceId", out var subProcessIdObj))
            {
                result.IsCompleted = false;
                result.ErrorMessage = "ID du sous-processus non trouvé";
                return result;
            }

            var subProcessId = subProcessIdObj.ToString();
            var subProcess = await _processInstanceRepository.GetByIdAsync(subProcessId!);

            if (subProcess == null)
            {
                result.IsCompleted = false;
                result.ErrorMessage = "Instance du sous-processus introuvable";
                return result;
            }

            if (subProcess.Status == ProcessStatus.Completed)
            {
                var outputData = MapOutputVariables(
                    subProcessStep.OutputMapping,
                    subProcess.Variables);

                result.IsCompleted = true;
                result.NextStepId = subProcessStep.NextStepId;
                result.OutputData = outputData;
            }
            else if (subProcess.Status == ProcessStatus.Failed)
            {
                result.IsCompleted = false;
                result.ErrorMessage = $"Le sous-processus a échoué: {subProcess.ErrorMessage}";
            }
            else
            {
                result.RequiresWait = true;
                result.IsCompleted = false;
            }
        }

        return result;
    }

    private Dictionary<string, object> MapInputVariables(
        Dictionary<string, object>? mapping,
        Dictionary<string, object> parentVariables)
    {
        if (mapping == null)
            return new Dictionary<string, object>(parentVariables);

        var result = new Dictionary<string, object>();

        foreach (var map in mapping)
        {
            if (map.Value is string sourceKey && parentVariables.ContainsKey(sourceKey))
            {
                result[map.Key] = parentVariables[sourceKey];
            }
            else
            {
                result[map.Key] = map.Value;
            }
        }

        return result;
    }

    private Dictionary<string, object> MapOutputVariables(
        Dictionary<string, object>? mapping,
        Dictionary<string, object> subProcessVariables)
    {
        if (mapping == null)
            return new Dictionary<string, object>(subProcessVariables);

        var result = new Dictionary<string, object>();

        foreach (var map in mapping)
        {
            if (map.Value is string sourceKey && subProcessVariables.ContainsKey(sourceKey))
            {
                result[map.Key] = subProcessVariables[sourceKey];
            }
        }

        return result;
    }
}
