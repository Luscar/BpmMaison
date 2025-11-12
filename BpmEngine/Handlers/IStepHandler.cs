using BpmEngine.Core.Models;

namespace BpmEngine.Handlers;

public interface IStepHandler
{
    StepType SupportedType { get; }
    Task<StepExecutionResult> ExecuteAsync(
        StepDefinition stepDefinition, 
        ProcessInstance processInstance,
        StepInstance stepInstance);
}

public class StepExecutionResult
{
    public bool IsCompleted { get; set; }
    public bool RequiresWait { get; set; }
    public string? NextStepId { get; set; }
    public Dictionary<string, object> OutputData { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime? ResumeAt { get; set; }
    public string? WaitingForSignal { get; set; }
}
