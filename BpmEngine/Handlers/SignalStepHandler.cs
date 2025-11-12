using BpmEngine.Core;
using BpmEngine.Core.Models;

namespace BpmEngine.Handlers;

public class SignalStepHandler : IStepHandler
{
    public StepType SupportedType => StepType.Signal;

    public Task<StepExecutionResult> ExecuteAsync(
        StepDefinition stepDefinition,
        ProcessInstance processInstance,
        StepInstance stepInstance)
    {
        var signalStep = (SignalStepDefinition)stepDefinition;
        var result = new StepExecutionResult();

        if (stepInstance.Status == StepStatus.NotStarted || stepInstance.Status == StepStatus.Running)
        {
            result.RequiresWait = true;
            result.IsCompleted = false;
            result.WaitingForSignal = signalStep.SignalName;
            
            if (signalStep.TimeoutMinutes.HasValue)
            {
                result.ResumeAt = DateTime.UtcNow.AddMinutes(signalStep.TimeoutMinutes.Value);
            }
        }
        else if (stepInstance.Status == StepStatus.WaitingForSignal)
        {
            if (signalStep.TimeoutMinutes.HasValue && 
                stepInstance.ScheduledFor.HasValue && 
                DateTime.UtcNow >= stepInstance.ScheduledFor.Value)
            {
                result.IsCompleted = false;
                result.ErrorMessage = $"Timeout en attente du signal: {signalStep.SignalName}";
            }
            else
            {
                result.RequiresWait = true;
                result.IsCompleted = false;
            }
        }

        return Task.FromResult(result);
    }
}
