namespace BpmEngine.Tests;

public class ProcessInstanceTests
{
    [Fact]
    public void ProcessInstance_DefaultValues_AreInitializedCorrectly()
    {
        // Act
        var instance = new ProcessInstance();

        // Assert
        Assert.Equal(string.Empty, instance.Id);
        Assert.Equal(string.Empty, instance.ProcessDefinitionId);
        Assert.Equal(0, instance.ProcessVersion);
        Assert.Equal(ProcessStatus.NotStarted, instance.Status);
        Assert.Equal(default(DateTime), instance.StartedAt);
        Assert.Null(instance.CompletedAt);
        Assert.Null(instance.CurrentStepId);
        Assert.NotNull(instance.Variables);
        Assert.Empty(instance.Variables);
        Assert.Null(instance.ErrorMessage);
        Assert.Null(instance.ParentProcessInstanceId);
    }

    [Fact]
    public void ProcessInstance_CanSetProperties()
    {
        // Arrange
        var instance = new ProcessInstance();
        var startTime = DateTime.UtcNow;

        // Act
        instance.Id = "inst-001";
        instance.ProcessDefinitionId = "proc-001";
        instance.ProcessVersion = 1;
        instance.Status = ProcessStatus.Running;
        instance.StartedAt = startTime;
        instance.CurrentStepId = "step-001";

        // Assert
        Assert.Equal("inst-001", instance.Id);
        Assert.Equal("proc-001", instance.ProcessDefinitionId);
        Assert.Equal(1, instance.ProcessVersion);
        Assert.Equal(ProcessStatus.Running, instance.Status);
        Assert.Equal(startTime, instance.StartedAt);
        Assert.Equal("step-001", instance.CurrentStepId);
    }

    [Fact]
    public void ProcessInstance_CanAddVariables()
    {
        // Arrange
        var instance = new ProcessInstance();

        // Act
        instance.Variables["orderId"] = "12345";
        instance.Variables["amount"] = 1500;
        instance.Variables["approved"] = true;

        // Assert
        Assert.Equal(3, instance.Variables.Count);
        Assert.Equal("12345", instance.Variables["orderId"]);
        Assert.Equal(1500, instance.Variables["amount"]);
        Assert.Equal(true, instance.Variables["approved"]);
    }

    [Fact]
    public void ProcessInstance_CanSetCompletionTime()
    {
        // Arrange
        var instance = new ProcessInstance
        {
            Status = ProcessStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        // Act
        var completedTime = DateTime.UtcNow;
        instance.CompletedAt = completedTime;
        instance.Status = ProcessStatus.Completed;

        // Assert
        Assert.NotNull(instance.CompletedAt);
        Assert.Equal(completedTime, instance.CompletedAt);
        Assert.Equal(ProcessStatus.Completed, instance.Status);
    }

    [Fact]
    public void ProcessInstance_CanSetErrorMessage()
    {
        // Arrange
        var instance = new ProcessInstance
        {
            Status = ProcessStatus.Running
        };

        // Act
        instance.Status = ProcessStatus.Failed;
        instance.ErrorMessage = "Failed to process order";

        // Assert
        Assert.Equal(ProcessStatus.Failed, instance.Status);
        Assert.Equal("Failed to process order", instance.ErrorMessage);
    }

    [Fact]
    public void ProcessInstance_CanSetParentProcessInstanceId()
    {
        // Arrange
        var instance = new ProcessInstance();

        // Act
        instance.ParentProcessInstanceId = "parent-inst-001";

        // Assert
        Assert.Equal("parent-inst-001", instance.ParentProcessInstanceId);
    }
}

public class StepInstanceTests
{
    [Fact]
    public void StepInstance_DefaultValues_AreInitializedCorrectly()
    {
        // Act
        var stepInstance = new StepInstance();

        // Assert
        Assert.Equal(string.Empty, stepInstance.Id);
        Assert.Equal(string.Empty, stepInstance.ProcessInstanceId);
        Assert.Equal(string.Empty, stepInstance.StepDefinitionId);
        Assert.Equal(default(StepType), stepInstance.StepType);
        Assert.Equal(StepStatus.NotStarted, stepInstance.Status);
        Assert.Equal(default(DateTime), stepInstance.StartedAt);
        Assert.Null(stepInstance.CompletedAt);
        Assert.NotNull(stepInstance.InputData);
        Assert.NotNull(stepInstance.OutputData);
        Assert.Empty(stepInstance.InputData);
        Assert.Empty(stepInstance.OutputData);
        Assert.Null(stepInstance.ErrorMessage);
        Assert.Null(stepInstance.ScheduledFor);
        Assert.Null(stepInstance.WaitingForSignal);
    }

    [Fact]
    public void StepInstance_CanSetProperties()
    {
        // Arrange
        var stepInstance = new StepInstance();
        var startTime = DateTime.UtcNow;

        // Act
        stepInstance.Id = "step-inst-001";
        stepInstance.ProcessInstanceId = "proc-inst-001";
        stepInstance.StepDefinitionId = "step-def-001";
        stepInstance.StepType = StepType.Business;
        stepInstance.Status = StepStatus.Running;
        stepInstance.StartedAt = startTime;

        // Assert
        Assert.Equal("step-inst-001", stepInstance.Id);
        Assert.Equal("proc-inst-001", stepInstance.ProcessInstanceId);
        Assert.Equal("step-def-001", stepInstance.StepDefinitionId);
        Assert.Equal(StepType.Business, stepInstance.StepType);
        Assert.Equal(StepStatus.Running, stepInstance.Status);
        Assert.Equal(startTime, stepInstance.StartedAt);
    }

    [Fact]
    public void StepInstance_CanSetInputAndOutputData()
    {
        // Arrange
        var stepInstance = new StepInstance();

        // Act
        stepInstance.InputData["orderId"] = "12345";
        stepInstance.OutputData["result"] = "success";

        // Assert
        Assert.Single(stepInstance.InputData);
        Assert.Single(stepInstance.OutputData);
        Assert.Equal("12345", stepInstance.InputData["orderId"]);
        Assert.Equal("success", stepInstance.OutputData["result"]);
    }

    [Fact]
    public void StepInstance_CanSetScheduledFor()
    {
        // Arrange
        var stepInstance = new StepInstance
        {
            Status = StepStatus.WaitingForSchedule
        };

        // Act
        var scheduledTime = DateTime.UtcNow.AddHours(1);
        stepInstance.ScheduledFor = scheduledTime;

        // Assert
        Assert.NotNull(stepInstance.ScheduledFor);
        Assert.Equal(scheduledTime, stepInstance.ScheduledFor);
    }

    [Fact]
    public void StepInstance_CanSetWaitingForSignal()
    {
        // Arrange
        var stepInstance = new StepInstance
        {
            Status = StepStatus.WaitingForSignal
        };

        // Act
        stepInstance.WaitingForSignal = "PaymentReceived";

        // Assert
        Assert.Equal("PaymentReceived", stepInstance.WaitingForSignal);
    }
}

public class TaskInstanceTests
{
    [Fact]
    public void TaskInstance_DefaultValues_AreInitializedCorrectly()
    {
        // Act
        var taskInstance = new TaskInstance();

        // Assert
        Assert.Equal(string.Empty, taskInstance.Id);
        Assert.Equal(string.Empty, taskInstance.ProcessInstanceId);
        Assert.Equal(string.Empty, taskInstance.StepInstanceId);
        Assert.Equal(string.Empty, taskInstance.TaskType);
        Assert.Equal(string.Empty, taskInstance.AssignedRole);
        Assert.Null(taskInstance.AssignedUserId);
        Assert.Equal(default(DateTime), taskInstance.CreatedAt);
        Assert.Null(taskInstance.CompletedAt);
        Assert.NotNull(taskInstance.TaskData);
        Assert.Empty(taskInstance.TaskData);
        Assert.Null(taskInstance.CompletionData);
        Assert.False(taskInstance.IsCompleted);
    }

    [Fact]
    public void TaskInstance_CanSetProperties()
    {
        // Arrange
        var taskInstance = new TaskInstance();
        var createdTime = DateTime.UtcNow;

        // Act
        taskInstance.Id = "task-001";
        taskInstance.ProcessInstanceId = "proc-inst-001";
        taskInstance.StepInstanceId = "step-inst-001";
        taskInstance.TaskType = "approval";
        taskInstance.AssignedRole = "manager";
        taskInstance.CreatedAt = createdTime;

        // Assert
        Assert.Equal("task-001", taskInstance.Id);
        Assert.Equal("proc-inst-001", taskInstance.ProcessInstanceId);
        Assert.Equal("step-inst-001", taskInstance.StepInstanceId);
        Assert.Equal("approval", taskInstance.TaskType);
        Assert.Equal("manager", taskInstance.AssignedRole);
        Assert.Equal(createdTime, taskInstance.CreatedAt);
    }

    [Fact]
    public void TaskInstance_CanAssignToUser()
    {
        // Arrange
        var taskInstance = new TaskInstance
        {
            AssignedRole = "manager"
        };

        // Act
        taskInstance.AssignedUserId = "user-123";

        // Assert
        Assert.Equal("user-123", taskInstance.AssignedUserId);
    }

    [Fact]
    public void TaskInstance_CanCompleteTask()
    {
        // Arrange
        var taskInstance = new TaskInstance
        {
            Id = "task-001",
            IsCompleted = false
        };

        var completionData = new Dictionary<string, object>
        {
            ["approved"] = true,
            ["comments"] = "Looks good"
        };

        // Act
        taskInstance.IsCompleted = true;
        taskInstance.CompletedAt = DateTime.UtcNow;
        taskInstance.CompletionData = completionData;

        // Assert
        Assert.True(taskInstance.IsCompleted);
        Assert.NotNull(taskInstance.CompletedAt);
        Assert.NotNull(taskInstance.CompletionData);
        Assert.Equal(2, taskInstance.CompletionData.Count);
    }

    [Fact]
    public void TaskInstance_CanSetTaskData()
    {
        // Arrange
        var taskInstance = new TaskInstance();

        // Act
        taskInstance.TaskData["orderId"] = "12345";
        taskInstance.TaskData["amount"] = 1500;
        taskInstance.TaskData["description"] = "Please approve this order";

        // Assert
        Assert.Equal(3, taskInstance.TaskData.Count);
        Assert.Equal("12345", taskInstance.TaskData["orderId"]);
        Assert.Equal(1500, taskInstance.TaskData["amount"]);
    }
}
