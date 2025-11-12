using BpmEngine.Core;
using BpmEngine.Core.Models;
using BpmEngine.Services;

namespace BpmEngine.Handlers;

public class DecisionStepHandler : IStepHandler
{
    private readonly IWebServiceClient _webServiceClient;
    private readonly IConditionEvaluator _conditionEvaluator;

    public StepType SupportedType => StepType.Decision;

    public DecisionStepHandler(
        IWebServiceClient webServiceClient,
        IConditionEvaluator conditionEvaluator)
    {
        _webServiceClient = webServiceClient;
        _conditionEvaluator = conditionEvaluator;
    }

    public async Task<StepExecutionResult> ExecuteAsync(
        StepDefinition stepDefinition,
        ProcessInstance processInstance,
        StepInstance stepInstance)
    {
        var decisionStep = (DecisionStepDefinition)stepDefinition;
        var result = new StepExecutionResult();

        try
        {
            var parameters = MergeParameters(decisionStep.Parameters, processInstance.Variables);
            
            var queryResult = await _webServiceClient.CallAsync(
                decisionStep.QueryServiceUrl,
                decisionStep.Method,
                parameters);

            var context = new Dictionary<string, object>(processInstance.Variables);
            foreach (var item in queryResult)
            {
                context[item.Key] = item.Value;
            }

            var selectedRoute = EvaluateRoutes(decisionStep.Routes, context);

            if (selectedRoute != null)
            {
                result.IsCompleted = true;
                result.NextStepId = selectedRoute.TargetStepId;
                result.OutputData = queryResult;
            }
            else
            {
                result.IsCompleted = false;
                result.ErrorMessage = "Aucune route ne correspond aux conditions";
            }
        }
        catch (Exception ex)
        {
            result.IsCompleted = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private DecisionRoute? EvaluateRoutes(
        List<DecisionRoute> routes,
        Dictionary<string, object> context)
    {
        var sortedRoutes = routes.OrderBy(r => r.Priority).ToList();

        foreach (var route in sortedRoutes)
        {
            if (_conditionEvaluator.Evaluate(route.Condition, context))
            {
                return route;
            }
        }

        return null;
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
