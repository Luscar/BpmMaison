namespace BpmEngine.Tests;

public class ProcessDefinitionTests
{
    [Fact]
    public void ProcessDefinition_DefaultValues_AreInitializedCorrectly()
    {
        // Act
        var definition = new ProcessDefinition();

        // Assert
        Assert.Equal(string.Empty, definition.Id);
        Assert.Equal(string.Empty, definition.Name);
        Assert.Equal(string.Empty, definition.Description);
        Assert.Equal(0, definition.Version);
        Assert.Equal(string.Empty, definition.StartStepId);
        Assert.NotNull(definition.Steps);
        Assert.Empty(definition.Steps);
    }

    [Fact]
    public void ProcessDefinition_CanSetProperties()
    {
        // Arrange
        var definition = new ProcessDefinition();

        // Act
        definition.Id = "proc-001";
        definition.Name = "Test Process";
        definition.Description = "A test process";
        definition.Version = 1;
        definition.StartStepId = "step-001";

        // Assert
        Assert.Equal("proc-001", definition.Id);
        Assert.Equal("Test Process", definition.Name);
        Assert.Equal("A test process", definition.Description);
        Assert.Equal(1, definition.Version);
        Assert.Equal("step-001", definition.StartStepId);
    }

    [Fact]
    public void ProcessDefinition_CanAddSteps()
    {
        // Arrange
        var definition = new ProcessDefinition
        {
            Id = "proc-001",
            Name = "Test Process"
        };

        var businessStep = new BusinessStepDefinition
        {
            Id = "step-001",
            Name = "Business Step",
            Type = StepType.Business,
            CommandName = "ProcessOrder"
        };

        // Act
        definition.Steps.Add(businessStep);

        // Assert
        Assert.Single(definition.Steps);
        Assert.Equal("step-001", definition.Steps[0].Id);
    }
}

public class BusinessStepDefinitionTests
{
    [Fact]
    public void BusinessStepDefinition_DefaultValues_AreInitializedCorrectly()
    {
        // Act
        var step = new BusinessStepDefinition();

        // Assert
        Assert.Equal(string.Empty, step.Id);
        Assert.Equal(string.Empty, step.Name);
        Assert.Equal(string.Empty, step.CommandName);
        Assert.Null(step.Parameters);
    }

    [Fact]
    public void BusinessStepDefinition_CanSetProperties()
    {
        // Arrange
        var step = new BusinessStepDefinition();

        // Act
        step.Id = "step-001";
        step.Name = "Process Order";
        step.Type = StepType.Business;
        step.CommandName = "ProcessOrderCommand";
        step.NextStepId = "step-002";
        step.Parameters = new Dictionary<string, object>
        {
            ["orderId"] = "12345"
        };

        // Assert
        Assert.Equal("step-001", step.Id);
        Assert.Equal("Process Order", step.Name);
        Assert.Equal(StepType.Business, step.Type);
        Assert.Equal("ProcessOrderCommand", step.CommandName);
        Assert.Equal("step-002", step.NextStepId);
        Assert.NotNull(step.Parameters);
        Assert.Single(step.Parameters);
    }
}

public class InteractiveStepDefinitionTests
{
    [Fact]
    public void InteractiveStepDefinition_DefaultValues_AreInitializedCorrectly()
    {
        // Act
        var step = new InteractiveStepDefinition();

        // Assert
        Assert.Equal(string.Empty, step.Id);
        Assert.Equal(string.Empty, step.TaskType);
        Assert.Equal(string.Empty, step.DefaultRole);
        Assert.Null(step.TaskData);
    }

    [Fact]
    public void InteractiveStepDefinition_CanSetProperties()
    {
        // Arrange
        var step = new InteractiveStepDefinition();

        // Act
        step.Id = "step-001";
        step.Name = "Approve Order";
        step.Type = StepType.Interactive;
        step.TaskType = "approval";
        step.DefaultRole = "manager";
        step.TaskData = new Dictionary<string, object>
        {
            ["description"] = "Please approve this order"
        };

        // Assert
        Assert.Equal("step-001", step.Id);
        Assert.Equal("Approve Order", step.Name);
        Assert.Equal(StepType.Interactive, step.Type);
        Assert.Equal("approval", step.TaskType);
        Assert.Equal("manager", step.DefaultRole);
        Assert.NotNull(step.TaskData);
    }
}

public class DecisionStepDefinitionTests
{
    [Fact]
    public void DecisionStepDefinition_DefaultValues_AreInitializedCorrectly()
    {
        // Act
        var step = new DecisionStepDefinition();

        // Assert
        Assert.Equal(string.Empty, step.Id);
        Assert.Equal(string.Empty, step.QueryName);
        Assert.Null(step.Parameters);
        Assert.NotNull(step.Routes);
        Assert.Empty(step.Routes);
    }

    [Fact]
    public void DecisionStepDefinition_CanAddRoutes()
    {
        // Arrange
        var step = new DecisionStepDefinition
        {
            Id = "step-001",
            Name = "Check Amount",
            Type = StepType.Decision,
            QueryName = "GetOrderAmount"
        };

        var route1 = new DecisionRoute
        {
            TargetStepId = "step-002",
            Condition = "amount > 1000",
            Priority = 1
        };

        var route2 = new DecisionRoute
        {
            TargetStepId = "step-003",
            Condition = "amount <= 1000",
            Priority = 2
        };

        // Act
        step.Routes.Add(route1);
        step.Routes.Add(route2);

        // Assert
        Assert.Equal(2, step.Routes.Count);
        Assert.Equal("step-002", step.Routes[0].TargetStepId);
        Assert.Equal("step-003", step.Routes[1].TargetStepId);
    }
}

public class ScheduledStepDefinitionTests
{
    [Fact]
    public void ScheduledStepDefinition_DefaultValues_AreNull()
    {
        // Act
        var step = new ScheduledStepDefinition();

        // Assert
        Assert.Null(step.ScheduleExpression);
        Assert.Null(step.DelayMinutes);
        Assert.Null(step.DelayHours);
        Assert.Null(step.DelayDays);
    }

    [Fact]
    public void ScheduledStepDefinition_CanSetDelays()
    {
        // Arrange
        var step = new ScheduledStepDefinition();

        // Act
        step.Id = "step-001";
        step.Name = "Delayed Step";
        step.Type = StepType.Scheduled;
        step.DelayMinutes = 30;

        // Assert
        Assert.Equal(30, step.DelayMinutes);
        Assert.Null(step.DelayHours);
        Assert.Null(step.DelayDays);
    }
}

public class SignalStepDefinitionTests
{
    [Fact]
    public void SignalStepDefinition_DefaultValues_AreInitializedCorrectly()
    {
        // Act
        var step = new SignalStepDefinition();

        // Assert
        Assert.Equal(string.Empty, step.SignalName);
        Assert.Null(step.TimeoutMinutes);
    }

    [Fact]
    public void SignalStepDefinition_CanSetProperties()
    {
        // Arrange
        var step = new SignalStepDefinition();

        // Act
        step.Id = "step-001";
        step.Name = "Wait for Signal";
        step.Type = StepType.Signal;
        step.SignalName = "PaymentReceived";
        step.TimeoutMinutes = 60;

        // Assert
        Assert.Equal("PaymentReceived", step.SignalName);
        Assert.Equal(60, step.TimeoutMinutes);
    }
}

public class SubProcessStepDefinitionTests
{
    [Fact]
    public void SubProcessStepDefinition_DefaultValues_AreInitializedCorrectly()
    {
        // Act
        var step = new SubProcessStepDefinition();

        // Assert
        Assert.Equal(string.Empty, step.SubProcessId);
        Assert.Null(step.SubProcessVersion);
        Assert.Null(step.InputMapping);
        Assert.Null(step.OutputMapping);
    }

    [Fact]
    public void SubProcessStepDefinition_CanSetMappings()
    {
        // Arrange
        var step = new SubProcessStepDefinition();

        // Act
        step.Id = "step-001";
        step.Name = "Call Subprocess";
        step.Type = StepType.SubProcess;
        step.SubProcessId = "subprocess-001";
        step.SubProcessVersion = 2;
        step.InputMapping = new Dictionary<string, object>
        {
            ["orderId"] = "12345"
        };
        step.OutputMapping = new Dictionary<string, object>
        {
            ["result"] = "status"
        };

        // Assert
        Assert.Equal("subprocess-001", step.SubProcessId);
        Assert.Equal(2, step.SubProcessVersion);
        Assert.NotNull(step.InputMapping);
        Assert.NotNull(step.OutputMapping);
    }
}
