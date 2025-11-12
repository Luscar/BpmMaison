namespace BpmEngine.Services;

public interface IWebServiceClient
{
    Task<Dictionary<string, object>> CallAsync(
        string url, 
        string method, 
        Dictionary<string, object>? parameters = null);
}

public interface ITaskService
{
    Task<string> CreateTaskAsync(
        string processInstanceId,
        string stepInstanceId,
        string taskType,
        string assignedRole,
        Dictionary<string, object> taskData);
}

public interface IConditionEvaluator
{
    bool Evaluate(string condition, Dictionary<string, object> context);
}
