namespace BpmEngine.Core;

public enum StepType
{
    Business,
    Interactive,
    Decision,
    Scheduled,
    Signal,
    SubProcess
}

public enum ProcessStatus
{
    NotStarted,
    Running,
    Waiting,
    Completed,
    Failed,
    Cancelled
}

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
