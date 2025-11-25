namespace BpmEngine.Tests;

public class StepTypeTests
{
    [Theory]
    [InlineData(StepType.Business)]
    [InlineData(StepType.Interactive)]
    [InlineData(StepType.Decision)]
    [InlineData(StepType.Scheduled)]
    [InlineData(StepType.Signal)]
    [InlineData(StepType.SubProcess)]
    public void StepType_AllValuesAreDefined(StepType stepType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(StepType), stepType));
    }

    [Fact]
    public void StepType_HasExpectedNumberOfValues()
    {
        // Act
        var values = Enum.GetValues<StepType>();

        // Assert
        Assert.Equal(6, values.Length);
    }

    [Fact]
    public void StepType_CanConvertToString()
    {
        // Arrange
        var stepType = StepType.Business;

        // Act
        var result = stepType.ToString();

        // Assert
        Assert.Equal("Business", result);
    }

    [Fact]
    public void StepType_CanParseFromString()
    {
        // Act
        var result = Enum.Parse<StepType>("Interactive");

        // Assert
        Assert.Equal(StepType.Interactive, result);
    }
}

public class ProcessStatusTests
{
    [Theory]
    [InlineData(ProcessStatus.NotStarted)]
    [InlineData(ProcessStatus.Running)]
    [InlineData(ProcessStatus.Waiting)]
    [InlineData(ProcessStatus.Completed)]
    [InlineData(ProcessStatus.Failed)]
    [InlineData(ProcessStatus.Cancelled)]
    public void ProcessStatus_AllValuesAreDefined(ProcessStatus status)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(ProcessStatus), status));
    }

    [Fact]
    public void ProcessStatus_HasExpectedNumberOfValues()
    {
        // Act
        var values = Enum.GetValues<ProcessStatus>();

        // Assert
        Assert.Equal(6, values.Length);
    }

    [Fact]
    public void ProcessStatus_CanConvertToString()
    {
        // Arrange
        var status = ProcessStatus.Running;

        // Act
        var result = status.ToString();

        // Assert
        Assert.Equal("Running", result);
    }

    [Fact]
    public void ProcessStatus_CanParseFromString()
    {
        // Act
        var result = Enum.Parse<ProcessStatus>("Completed");

        // Assert
        Assert.Equal(ProcessStatus.Completed, result);
    }

    [Fact]
    public void ProcessStatus_DefaultValueIsNotStarted()
    {
        // Act
        ProcessStatus defaultStatus = default;

        // Assert
        Assert.Equal(ProcessStatus.NotStarted, defaultStatus);
    }
}

public class StepStatusTests
{
    [Theory]
    [InlineData(StepStatus.NotStarted)]
    [InlineData(StepStatus.Running)]
    [InlineData(StepStatus.WaitingForTask)]
    [InlineData(StepStatus.WaitingForSchedule)]
    [InlineData(StepStatus.WaitingForSignal)]
    [InlineData(StepStatus.Completed)]
    [InlineData(StepStatus.Failed)]
    [InlineData(StepStatus.Skipped)]
    public void StepStatus_AllValuesAreDefined(StepStatus status)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(StepStatus), status));
    }

    [Fact]
    public void StepStatus_HasExpectedNumberOfValues()
    {
        // Act
        var values = Enum.GetValues<StepStatus>();

        // Assert
        Assert.Equal(8, values.Length);
    }

    [Fact]
    public void StepStatus_CanConvertToString()
    {
        // Arrange
        var status = StepStatus.WaitingForTask;

        // Act
        var result = status.ToString();

        // Assert
        Assert.Equal("WaitingForTask", result);
    }

    [Fact]
    public void StepStatus_CanParseFromString()
    {
        // Act
        var result = Enum.Parse<StepStatus>("WaitingForSignal");

        // Assert
        Assert.Equal(StepStatus.WaitingForSignal, result);
    }

    [Fact]
    public void StepStatus_DefaultValueIsNotStarted()
    {
        // Act
        StepStatus defaultStatus = default;

        // Assert
        Assert.Equal(StepStatus.NotStarted, defaultStatus);
    }

    [Theory]
    [InlineData(StepStatus.WaitingForTask)]
    [InlineData(StepStatus.WaitingForSchedule)]
    [InlineData(StepStatus.WaitingForSignal)]
    public void StepStatus_WaitingStatesAreDifferent(StepStatus status)
    {
        // Assert
        Assert.NotEqual(StepStatus.Running, status);
        Assert.NotEqual(StepStatus.Completed, status);
    }
}
