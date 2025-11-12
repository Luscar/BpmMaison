using BpmEngine.Core;
using BpmEngine.Core.Models;

namespace BpmEngine.Handlers;

public class ScheduledStepHandler : IStepHandler
{
    public StepType SupportedType => StepType.Scheduled;

    public Task<StepExecutionResult> ExecuteAsync(
        StepDefinition stepDefinition,
        ProcessInstance processInstance,
        StepInstance stepInstance)
    {
        var scheduledStep = (ScheduledStepDefinition)stepDefinition;
        var result = new StepExecutionResult();

        if (stepInstance.Status == StepStatus.NotStarted || stepInstance.Status == StepStatus.Running)
        {
            var resumeAt = CalculateResumeTime(scheduledStep);

            result.RequiresWait = true;
            result.IsCompleted = false;
            result.ResumeAt = resumeAt;
        }
        else if (stepInstance.Status == StepStatus.WaitingForSchedule)
        {
            if (stepInstance.ScheduledFor.HasValue && DateTime.UtcNow >= stepInstance.ScheduledFor.Value)
            {
                result.IsCompleted = true;
                result.NextStepId = scheduledStep.NextStepId;
            }
            else
            {
                result.RequiresWait = true;
                result.IsCompleted = false;
            }
        }

        return Task.FromResult(result);
    }

    private DateTime CalculateResumeTime(ScheduledStepDefinition step)
    {
        var now = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(step.ScheduleExpression))
        {
            return ParseScheduleExpression(step.ScheduleExpression);
        }

        var totalMinutes = 0;

        if (step.DelayMinutes.HasValue)
            totalMinutes += step.DelayMinutes.Value;

        if (step.DelayHours.HasValue)
            totalMinutes += step.DelayHours.Value * 60;

        if (step.DelayDays.HasValue)
            totalMinutes += step.DelayDays.Value * 24 * 60;

        return now.AddMinutes(totalMinutes);
    }

    private DateTime ParseScheduleExpression(string expression)
    {
        if (DateTime.TryParse(expression, out var specificDate))
        {
            return specificDate;
        }

        return DateTime.UtcNow.AddMinutes(1);
    }
}
