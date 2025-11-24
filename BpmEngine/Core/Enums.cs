using System.Text.Json.Serialization;

namespace BpmEngine.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StepType
{
    Business,
    Interactive,
    Decision,
    Scheduled,
    Signal,
    SubProcess
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProcessStatus
{
    NotStarted,
    Running,
    Waiting,
    Completed,
    Failed,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StepStatus
{
    NotStarted,
    Running,
    WaitingForTask,
    WaitingForSchedule,
    WaitingForSignal,
    Completed,
    Failed,
    Skipped
}
