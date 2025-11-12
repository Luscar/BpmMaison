namespace BpmEngine.Core.Models;

public class ProcessInstance
{
    public string Id { get; set; } = string.Empty;
    public string ProcessDefinitionId { get; set; } = string.Empty;
    public int ProcessVersion { get; set; }
    public ProcessStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CurrentStepId { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? ParentProcessInstanceId { get; set; }
}

public class StepInstance
{
    public string Id { get; set; } = string.Empty;
    public string ProcessInstanceId { get; set; } = string.Empty;
    public string StepDefinitionId { get; set; } = string.Empty;
    public StepType StepType { get; set; }
    public StepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object> InputData { get; set; } = new();
    public Dictionary<string, object> OutputData { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public string? WaitingForSignal { get; set; }
}

public class TaskInstance
{
    public string Id { get; set; } = string.Empty;
    public string ProcessInstanceId { get; set; } = string.Empty;
    public string StepInstanceId { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string AssignedRole { get; set; } = string.Empty;
    public string? AssignedUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object> TaskData { get; set; } = new();
    public Dictionary<string, object>? CompletionData { get; set; }
    public bool IsCompleted { get; set; }
}
