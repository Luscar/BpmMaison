namespace BpmEngine.Core.Models;

public class ProcessDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; }
    public string StartStepId { get; set; } = string.Empty;
    public List<StepDefinition> Steps { get; set; } = new();
}

public abstract class StepDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public StepType Type { get; set; }
    public string? NextStepId { get; set; }
}

public class BusinessStepDefinition : StepDefinition
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string Method { get; set; } = "POST";
    public Dictionary<string, object>? Parameters { get; set; }
}

public class InteractiveStepDefinition : StepDefinition
{
    public string TaskType { get; set; } = string.Empty;
    public string DefaultRole { get; set; } = string.Empty;
    public Dictionary<string, object>? TaskData { get; set; }
}

public class DecisionStepDefinition : StepDefinition
{
    public string QueryServiceUrl { get; set; } = string.Empty;
    public string Method { get; set; } = "POST";
    public Dictionary<string, object>? Parameters { get; set; }
    public List<DecisionRoute> Routes { get; set; } = new();
}

public class DecisionRoute
{
    public string TargetStepId { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class ScheduledStepDefinition : StepDefinition
{
    public string? ScheduleExpression { get; set; }
    public int? DelayMinutes { get; set; }
    public int? DelayHours { get; set; }
    public int? DelayDays { get; set; }
}

public class SignalStepDefinition : StepDefinition
{
    public string SignalName { get; set; } = string.Empty;
    public int? TimeoutMinutes { get; set; }
}

public class SubProcessStepDefinition : StepDefinition
{
    public string SubProcessId { get; set; } = string.Empty;
    public int? SubProcessVersion { get; set; }
    public Dictionary<string, object>? InputMapping { get; set; }
    public Dictionary<string, object>? OutputMapping { get; set; }
}
