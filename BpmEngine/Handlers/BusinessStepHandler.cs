using BpmEngine.Core;
using BpmEngine.Core.Models;
using BpmEngine.Services;

namespace BpmEngine.Handlers;

public class BusinessStepHandler : IStepHandler
{
    private readonly ICommandHandler _commandHandler;
    private readonly IWebServiceClient? _webServiceClient; // Legacy support

    public StepType SupportedType => StepType.Business;

    public BusinessStepHandler(ICommandHandler commandHandler)
    {
        _commandHandler = commandHandler;
        _webServiceClient = null;
    }

    // Legacy constructor for backward compatibility
    [Obsolete("Use constructor with ICommandHandler instead")]
    public BusinessStepHandler(IWebServiceClient webServiceClient)
    {
        _webServiceClient = webServiceClient;
        _commandHandler = null!;
    }

    public async Task<StepExecutionResult> ExecuteAsync(
        StepDefinition stepDefinition,
        ProcessInstance processInstance,
        StepInstance stepInstance)
    {
        var businessStep = (BusinessStepDefinition)stepDefinition;
        var result = new StepExecutionResult();

        try
        {
            var parameters = MergeParameters(businessStep.Parameters, processInstance.Variables);

            Dictionary<string, object> response;

            // Use new CQRS pattern if CommandName is provided
            if (!string.IsNullOrEmpty(businessStep.CommandName))
            {
                if (_commandHandler == null)
                    throw new InvalidOperationException("ICommandHandler not configured. Please use the new constructor with ICommandHandler.");

                response = await _commandHandler.ExecuteAsync(
                    businessStep.CommandName,
                    parameters);
            }
            // Legacy support for ServiceUrl
            else if (!string.IsNullOrEmpty(businessStep.ServiceUrl))
            {
                if (_webServiceClient == null)
                    throw new InvalidOperationException("IWebServiceClient not configured. Please migrate to ICommandHandler or use the legacy constructor.");

#pragma warning disable CS0618 // Type or member is obsolete
                response = await _webServiceClient.CallAsync(
                    businessStep.ServiceUrl,
                    businessStep.Method,
                    parameters);
#pragma warning restore CS0618 // Type or member is obsolete
            }
            else
            {
                throw new InvalidOperationException("Either CommandName or ServiceUrl must be specified in BusinessStepDefinition.");
            }

            result.IsCompleted = true;
            result.OutputData = response;
            result.NextStepId = businessStep.NextStepId;
        }
        catch (Exception ex)
        {
            result.IsCompleted = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private Dictionary<string, object> MergeParameters(
        Dictionary<string, object>? stepParameters,
        Dictionary<string, object> variables)
    {
        var merged = new Dictionary<string, object>(variables);
        
        if (stepParameters != null)
        {
            foreach (var param in stepParameters)
            {
                merged[param.Key] = param.Value;
            }
        }

        return merged;
    }
}
