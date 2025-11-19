using BpmEngine.Core;
using BpmEngine.Core.Models;
using BpmEngine.Services;

namespace BpmEngine.Handlers;

public class DecisionStepHandler : IStepHandler
{
    private readonly IQueryHandler _queryHandler;
    private readonly IConditionEvaluator _conditionEvaluator;
    private readonly IWebServiceClient? _webServiceClient; // Legacy support

    public StepType SupportedType => StepType.Decision;

    public DecisionStepHandler(
        IQueryHandler queryHandler,
        IConditionEvaluator conditionEvaluator)
    {
        _queryHandler = queryHandler;
        _conditionEvaluator = conditionEvaluator;
        _webServiceClient = null;
    }

    // Legacy constructor for backward compatibility
    [Obsolete("Use constructor with IQueryHandler instead")]
    public DecisionStepHandler(
        IWebServiceClient webServiceClient,
        IConditionEvaluator conditionEvaluator)
    {
        _webServiceClient = webServiceClient;
        _conditionEvaluator = conditionEvaluator;
        _queryHandler = null!;
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

            Dictionary<string, object> queryResult;

            // Use new CQRS pattern if QueryName is provided
            if (!string.IsNullOrEmpty(decisionStep.QueryName))
            {
                if (_queryHandler == null)
                    throw new InvalidOperationException("IQueryHandler not configured. Please use the new constructor with IQueryHandler.");

                queryResult = await _queryHandler.ExecuteAsync(
                    decisionStep.QueryName,
                    parameters);
            }
            // Legacy support for QueryServiceUrl
            else if (!string.IsNullOrEmpty(decisionStep.QueryServiceUrl))
            {
                if (_webServiceClient == null)
                    throw new InvalidOperationException("IWebServiceClient not configured. Please migrate to IQueryHandler or use the legacy constructor.");

#pragma warning disable CS0618 // Type or member is obsolete
                queryResult = await _webServiceClient.CallAsync(
                    decisionStep.QueryServiceUrl,
                    decisionStep.Method,
                    parameters);
#pragma warning restore CS0618 // Type or member is obsolete
            }
            else
            {
                throw new InvalidOperationException("Either QueryName or QueryServiceUrl must be specified in DecisionStepDefinition.");
            }

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
