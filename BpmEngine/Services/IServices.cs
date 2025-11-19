namespace BpmEngine.Services;

/// <summary>
/// Interface for executing commands (write operations) in business steps.
/// Client applications must implement this to handle specific commands by name.
/// </summary>
public interface ICommandHandler
{
    Task<Dictionary<string, object>> ExecuteAsync(
        string commandName,
        Dictionary<string, object>? parameters = null);
}

/// <summary>
/// Interface for executing queries (read operations) in decision steps.
/// Client applications must implement this to handle specific queries by name.
/// </summary>
public interface IQueryHandler
{
    Task<Dictionary<string, object>> ExecuteAsync(
        string queryName,
        Dictionary<string, object>? parameters = null);
}

/// <summary>
/// Legacy interface for web service calls via HTTP.
/// Deprecated: Use ICommandHandler and IQueryHandler instead.
/// </summary>
[Obsolete("Use ICommandHandler and IQueryHandler instead for CQRS pattern")]
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
