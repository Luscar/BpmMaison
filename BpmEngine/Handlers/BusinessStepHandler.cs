using BpmEngine.Core;
using BpmEngine.Core.Models;
using BpmEngine.Services;

namespace BpmEngine.Handlers;

public class BusinessStepHandler : IStepHandler
{
    private readonly IWebServiceClient _webServiceClient;

    public StepType SupportedType => StepType.Business;

    public BusinessStepHandler(IWebServiceClient webServiceClient)
    {
        _webServiceClient = webServiceClient;
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
            
            var response = await _webServiceClient.CallAsync(
                businessStep.ServiceUrl,
                businessStep.Method,
                parameters);

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
