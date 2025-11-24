using BpmEngine.Core;
using BpmEngine.Core.Models;
using BpmEngine.Repository;
using BpmEngine.Services;
using BpmEngine.Handlers;
using BpmEngine.Engine;
using BpmEngine.Services.Impl;

// ============================================================================
// EXEMPLE D'IMPLÉMENTATION CLIENT
// ============================================================================

namespace ClientApp;

// ----------------------------------------------------------------------------
// 1a. IMPLÉMENTATION DES REPOSITORIES (JSON FILES - RECOMMENDED FOR PROCESS DEFINITIONS)
// ----------------------------------------------------------------------------

/// <summary>
/// JSON file-based repository for process definitions.
/// Stores each process definition as a JSON file with versioning support.
/// File naming: {processId}_v{version}.json
/// </summary>
public class JsonFileProcessDefinitionRepository : IProcessDefinitionRepository
{
    private readonly string _directoryPath;

    public JsonFileProcessDefinitionRepository(string directoryPath)
    {
        _directoryPath = directoryPath;

        // Ensure directory exists
        if (!Directory.Exists(_directoryPath))
        {
            Directory.CreateDirectory(_directoryPath);
        }
    }

    public async Task<ProcessDefinition?> GetByIdAsync(string processId, int? version = null)
    {
        if (version.HasValue)
        {
            // Get specific version
            var filePath = GetFilePath(processId, version.Value);
            if (!File.Exists(filePath))
                return null;

            return await ReadProcessDefinitionAsync(filePath);
        }
        else
        {
            // Get latest version
            var versions = await GetVersionsAsync(processId);
            return versions.OrderByDescending(p => p.Version).FirstOrDefault();
        }
    }

    public async Task<ProcessDefinition> SaveAsync(ProcessDefinition definition)
    {
        // If version is 0 or not set, auto-increment
        if (definition.Version == 0)
        {
            var existingVersions = await GetVersionsAsync(definition.Id);
            definition.Version = existingVersions.Any()
                ? existingVersions.Max(p => p.Version) + 1
                : 1;
        }

        var filePath = GetFilePath(definition.Id, definition.Version);

        // Serialize with proper options
        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = System.Text.Json.JsonSerializer.Serialize(definition, options);
        await File.WriteAllTextAsync(filePath, json);

        return definition;
    }

    public async Task<List<ProcessDefinition>> GetAllAsync()
    {
        var allFiles = Directory.GetFiles(_directoryPath, "*_v*.json");
        var allDefinitions = new List<ProcessDefinition>();

        foreach (var filePath in allFiles)
        {
            var definition = await ReadProcessDefinitionAsync(filePath);
            if (definition != null)
                allDefinitions.Add(definition);
        }

        // Group by process ID and return only latest versions
        return allDefinitions
            .GroupBy(p => p.Id)
            .Select(g => g.OrderByDescending(p => p.Version).First())
            .OrderBy(p => p.Name)
            .ToList();
    }

    public async Task<List<ProcessDefinition>> GetVersionsAsync(string processId)
    {
        var pattern = $"{SanitizeFileName(processId)}_v*.json";
        var files = Directory.GetFiles(_directoryPath, pattern);
        var versions = new List<ProcessDefinition>();

        foreach (var filePath in files)
        {
            var definition = await ReadProcessDefinitionAsync(filePath);
            if (definition != null)
                versions.Add(definition);
        }

        return versions.OrderBy(p => p.Version).ToList();
    }

    private async Task<ProcessDefinition?> ReadProcessDefinitionAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new ProcessDefinitionJsonConverter()
                }
            };

            return System.Text.Json.JsonSerializer.Deserialize<ProcessDefinition>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading process definition from {filePath}: {ex.Message}");
            return null;
        }
    }

    private string GetFilePath(string processId, int version)
    {
        var fileName = $"{SanitizeFileName(processId)}_v{version}.json";
        return Path.Combine(_directoryPath, fileName);
    }

    private string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}

/// <summary>
/// Custom JSON converter to handle polymorphic step definitions
/// </summary>
public class ProcessDefinitionJsonConverter : System.Text.Json.Serialization.JsonConverter<ProcessDefinition>
{
    public override ProcessDefinition Read(
        ref System.Text.Json.Utf8JsonReader reader,
        Type typeToConvert,
        System.Text.Json.JsonSerializerOptions options)
    {
        using var doc = System.Text.Json.JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var definition = new ProcessDefinition
        {
            Id = root.GetProperty("id").GetString() ?? string.Empty,
            Name = root.GetProperty("name").GetString() ?? string.Empty,
            Description = root.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
            Version = root.TryGetProperty("version", out var ver) ? ver.GetInt32() : 1,
            StartStepId = root.GetProperty("startStepId").GetString() ?? string.Empty
        };

        if (root.TryGetProperty("steps", out var stepsElement))
        {
            foreach (var stepElement in stepsElement.EnumerateArray())
            {
                var step = DeserializeStep(stepElement, options);
                if (step != null)
                    definition.Steps.Add(step);
            }
        }

        return definition;
    }

    private StepDefinition? DeserializeStep(
        System.Text.Json.JsonElement stepElement,
        System.Text.Json.JsonSerializerOptions options)
    {
        if (!stepElement.TryGetProperty("type", out var typeElement))
            return null;

        var typeString = typeElement.GetString();
        var stepType = Enum.Parse<StepType>(typeString ?? "Business");

        StepDefinition? step = stepType switch
        {
            StepType.Business => System.Text.Json.JsonSerializer.Deserialize<BusinessStepDefinition>(stepElement.GetRawText(), options),
            StepType.Interactive => System.Text.Json.JsonSerializer.Deserialize<InteractiveStepDefinition>(stepElement.GetRawText(), options),
            StepType.Decision => System.Text.Json.JsonSerializer.Deserialize<DecisionStepDefinition>(stepElement.GetRawText(), options),
            StepType.Scheduled => System.Text.Json.JsonSerializer.Deserialize<ScheduledStepDefinition>(stepElement.GetRawText(), options),
            StepType.Signal => System.Text.Json.JsonSerializer.Deserialize<SignalStepDefinition>(stepElement.GetRawText(), options),
            StepType.SubProcess => System.Text.Json.JsonSerializer.Deserialize<SubProcessStepDefinition>(stepElement.GetRawText(), options),
            _ => null
        };

        return step;
    }

    public override void Write(
        System.Text.Json.Utf8JsonWriter writer,
        ProcessDefinition value,
        System.Text.Json.JsonSerializerOptions options)
    {
        System.Text.Json.JsonSerializer.Serialize(writer, value, options);
    }
}

// ----------------------------------------------------------------------------
// 1b. IMPLÉMENTATION DES REPOSITORIES (ORACLE - FOR RUNTIME DATA)
// ----------------------------------------------------------------------------

public class OracleProcessDefinitionRepository : IProcessDefinitionRepository
{
    private readonly string _connectionString;

    public OracleProcessDefinitionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ProcessDefinition?> GetByIdAsync(string processId, int? version = null)
    {
        // TODO: Implémentation Oracle
        // SELECT * FROM PROC_DEF WHERE PROC_ID = :processId 
        // AND (VERSION = :version OR :version IS NULL)
        // ORDER BY VERSION DESC FETCH FIRST 1 ROW ONLY
        throw new NotImplementedException();
    }

    public async Task<ProcessDefinition> SaveAsync(ProcessDefinition definition)
    {
        // TODO: Implémentation Oracle
        // INSERT INTO PROC_DEF (PROC_ID, NOM, VERSION, DEF_JSON, DATE_CRE)
        // VALUES (:id, :name, :version, :json, SYSDATE)
        throw new NotImplementedException();
    }

    public async Task<List<ProcessDefinition>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<List<ProcessDefinition>> GetVersionsAsync(string processId)
    {
        throw new NotImplementedException();
    }
}

public class OracleProcessInstanceRepository : IProcessInstanceRepository
{
    private readonly string _connectionString;

    public OracleProcessInstanceRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ProcessInstance?> GetByIdAsync(string instanceId)
    {
        // TODO: Implémentation Oracle
        // SELECT * FROM INST_PROC WHERE INST_ID = :instanceId
        throw new NotImplementedException();
    }

    public async Task<ProcessInstance> CreateAsync(ProcessInstance instance)
    {
        // TODO: Implémentation Oracle
        // INSERT INTO INST_PROC (INST_ID, PROC_ID, STATUT, DATE_DEBUT, VARIABLES_JSON)
        // VALUES (:id, :procId, :status, SYSDATE, :varsJson)
        throw new NotImplementedException();
    }

    public async Task<ProcessInstance> UpdateAsync(ProcessInstance instance)
    {
        // TODO: Implémentation Oracle
        // UPDATE INST_PROC SET STATUT = :status, ETAPE_CUR = :stepId, 
        // VARIABLES_JSON = :varsJson, DATE_FIN = :completedAt, MSG_ERR = :error
        // WHERE INST_ID = :id
        throw new NotImplementedException();
    }

    public async Task<List<ProcessInstance>> GetByStatusAsync(ProcessStatus status)
    {
        throw new NotImplementedException();
    }

    public async Task<List<ProcessInstance>> GetByDefinitionIdAsync(string definitionId)
    {
        throw new NotImplementedException();
    }
}

public class OracleStepInstanceRepository : IStepInstanceRepository
{
    private readonly string _connectionString;

    public OracleStepInstanceRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<StepInstance?> GetByIdAsync(string stepInstanceId)
    {
        // TODO: Implémentation Oracle
        throw new NotImplementedException();
    }

    public async Task<StepInstance> CreateAsync(StepInstance instance)
    {
        // TODO: Implémentation Oracle
        // INSERT INTO INST_ETAPE (INST_ETAPE_ID, INST_PROC_ID, ETAPE_DEF_ID, 
        // TYPE_ETAPE, STATUT, DATE_DEBUT, DONNEES_IN_JSON)
        throw new NotImplementedException();
    }

    public async Task<StepInstance> UpdateAsync(StepInstance instance)
    {
        // TODO: Implémentation Oracle
        throw new NotImplementedException();
    }

    public async Task<List<StepInstance>> GetByProcessInstanceIdAsync(string processInstanceId)
    {
        // TODO: Implémentation Oracle
        throw new NotImplementedException();
    }

    public async Task<List<StepInstance>> GetScheduledStepsAsync()
    {
        // TODO: Implémentation Oracle
        // SELECT * FROM INST_ETAPE 
        // WHERE STATUT = 'WaitingForSchedule' 
        // AND DATE_PREVUE <= SYSDATE
        throw new NotImplementedException();
    }

    public async Task<List<StepInstance>> GetWaitingForSignalAsync(string signalName)
    {
        // TODO: Implémentation Oracle
        // SELECT * FROM INST_ETAPE 
        // WHERE STATUT = 'WaitingForSignal' 
        // AND NOM_SIGNAL = :signalName
        throw new NotImplementedException();
    }
}

public class OracleTaskRepository : ITaskRepository
{
    private readonly string _connectionString;

    public OracleTaskRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<TaskInstance?> GetByIdAsync(string taskId)
    {
        // TODO: Implémentation Oracle
        throw new NotImplementedException();
    }

    public async Task<TaskInstance> CreateAsync(TaskInstance task)
    {
        // TODO: Implémentation Oracle
        // INSERT INTO TACHE (TACHE_ID, INST_PROC_ID, INST_ETAPE_ID,
        // TYPE_TACHE, ROLE_ASSIGNE, DATE_CRE, DONNEES_JSON, EST_COMPLETE)
        throw new NotImplementedException();
    }

    public async Task<TaskInstance> UpdateAsync(TaskInstance task)
    {
        // TODO: Implémentation Oracle
        throw new NotImplementedException();
    }

    public async Task<List<TaskInstance>> GetByProcessInstanceIdAsync(string processInstanceId)
    {
        throw new NotImplementedException();
    }

    public async Task<List<TaskInstance>> GetPendingByRoleAsync(string role)
    {
        // TODO: Implémentation Oracle
        // SELECT * FROM TACHE 
        // WHERE ROLE_ASSIGNE = :role 
        // AND EST_COMPLETE = 0
        // ORDER BY DATE_CRE DESC
        throw new NotImplementedException();
    }

    public async Task<List<TaskInstance>> GetByUserIdAsync(string userId)
    {
        throw new NotImplementedException();
    }
}

// ----------------------------------------------------------------------------
// 2. IMPLÉMENTATION DES SERVICES (CQRS PATTERN - RECOMMENDED)
// ----------------------------------------------------------------------------

/// <summary>
/// Command handler implementation - handles write operations
/// Client implements specific commands based on their business logic
/// </summary>
public class ApplicationCommandHandler : ICommandHandler
{
    private readonly HttpClient _httpClient;
    // Add your dependencies: DbContext, repositories, etc.

    public ApplicationCommandHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Dictionary<string, object>> ExecuteAsync(
        string commandName,
        Dictionary<string, object>? parameters = null)
    {
        // Route to appropriate command handler based on commandName
        return commandName switch
        {
            "ValidateOrderRequest" => await ValidateOrderRequest(parameters),
            "ApproveOrderAutomatically" => await ApproveOrderAutomatically(parameters),
            "SendNotification" => await SendNotification(parameters),
            "ReceiveOrder" => await ReceiveOrder(parameters),
            "UpdateInventory" => await UpdateInventory(parameters),
            "CreateInvoice" => await CreateInvoice(parameters),
            _ => throw new NotImplementedException($"Command '{commandName}' is not implemented")
        };
    }

    private async Task<Dictionary<string, object>> ValidateOrderRequest(Dictionary<string, object>? parameters)
    {
        // TODO: Implement business logic
        // - Validate order data
        // - Check business rules
        // - Return validation result
        return new Dictionary<string, object>
        {
            ["isValid"] = true,
            ["validationDate"] = DateTime.UtcNow
        };
    }

    private async Task<Dictionary<string, object>> ApproveOrderAutomatically(Dictionary<string, object>? parameters)
    {
        // TODO: Implement auto-approval logic
        return new Dictionary<string, object>
        {
            ["approved"] = true,
            ["approvalDate"] = DateTime.UtcNow
        };
    }

    private async Task<Dictionary<string, object>> SendNotification(Dictionary<string, object>? parameters)
    {
        // TODO: Send notification via email/SMS
        return new Dictionary<string, object>
        {
            ["sent"] = true,
            ["timestamp"] = DateTime.UtcNow
        };
    }

    private async Task<Dictionary<string, object>> ReceiveOrder(Dictionary<string, object>? parameters)
    {
        // TODO: Implement order reception logic
        return new Dictionary<string, object>
        {
            ["received"] = true,
            ["orderId"] = Guid.NewGuid().ToString()
        };
    }

    private async Task<Dictionary<string, object>> UpdateInventory(Dictionary<string, object>? parameters)
    {
        // TODO: Update inventory levels
        return new Dictionary<string, object>
        {
            ["updated"] = true,
            ["newQuantity"] = 100
        };
    }

    private async Task<Dictionary<string, object>> CreateInvoice(Dictionary<string, object>? parameters)
    {
        // TODO: Create invoice
        return new Dictionary<string, object>
        {
            ["invoiceId"] = Guid.NewGuid().ToString(),
            ["created"] = true
        };
    }
}

/// <summary>
/// Query handler implementation - handles read operations
/// Client implements specific queries based on their business logic
/// </summary>
public class ApplicationQueryHandler : IQueryHandler
{
    private readonly HttpClient _httpClient;
    // Add your dependencies: DbContext, repositories, etc.

    public ApplicationQueryHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Dictionary<string, object>> ExecuteAsync(
        string queryName,
        Dictionary<string, object>? parameters = null)
    {
        // Route to appropriate query handler based on queryName
        return queryName switch
        {
            "GetOrderAmount" => await GetOrderAmount(parameters),
            "CheckInventoryAvailability" => await CheckInventoryAvailability(parameters),
            "GetProductStatus" => await GetProductStatus(parameters),
            "CheckProductAvailability" => await CheckProductAvailability(parameters),
            _ => throw new NotImplementedException($"Query '{queryName}' is not implemented")
        };
    }

    private async Task<Dictionary<string, object>> GetOrderAmount(Dictionary<string, object>? parameters)
    {
        // TODO: Query order amount from database or service
        // This is a read-only operation
        return new Dictionary<string, object>
        {
            ["montant"] = 1500.00,
            ["currency"] = "EUR"
        };
    }

    private async Task<Dictionary<string, object>> CheckInventoryAvailability(Dictionary<string, object>? parameters)
    {
        // TODO: Check inventory levels
        return new Dictionary<string, object>
        {
            ["available"] = true,
            ["quantity"] = 50,
            ["stockLevel"] = "sufficient"
        };
    }

    private async Task<Dictionary<string, object>> GetProductStatus(Dictionary<string, object>? parameters)
    {
        // TODO: Get product status
        return new Dictionary<string, object>
        {
            ["status"] = "available",
            ["price"] = 99.99
        };
    }

    private async Task<Dictionary<string, object>> CheckProductAvailability(Dictionary<string, object>? parameters)
    {
        // TODO: Check if product is available
        return new Dictionary<string, object>
        {
            ["available"] = true,
            ["deliveryDays"] = 3
        };
    }
}

// ----------------------------------------------------------------------------
// 2b. LEGACY IMPLEMENTATION (DEPRECATED - For backward compatibility only)
// ----------------------------------------------------------------------------

[Obsolete("Use ApplicationCommandHandler and ApplicationQueryHandler instead")]
public class HttpWebServiceClient : IWebServiceClient
{
    private readonly HttpClient _httpClient;

    public HttpWebServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Dictionary<string, object>> CallAsync(
        string url,
        string method,
        Dictionary<string, object>? parameters = null)
    {
        // TODO: Implémentation réelle avec HttpClient
        // - Sérialiser les paramètres en JSON
        // - Faire l'appel HTTP (GET/POST/PUT)
        // - Désérialiser la réponse
        // - Gérer les erreurs HTTP
        throw new NotImplementedException();
    }
}

public class CustomTaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    // Peut avoir d'autres dépendances: notificateur, etc.

    public CustomTaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<string> CreateTaskAsync(
        string processInstanceId,
        string stepInstanceId,
        string taskType,
        string assignedRole,
        Dictionary<string, object> taskData)
    {
        // TODO: Logique métier pour créer la tâche
        // - Générer l'ID
        // - Envoyer notification au rôle
        // - Créer l'entrée dans l'application cliente
        // - Persister via repository
        
        var taskId = Guid.NewGuid().ToString();
        
        var task = new TaskInstance
        {
            Id = taskId,
            ProcessInstanceId = processInstanceId,
            StepInstanceId = stepInstanceId,
            TaskType = taskType,
            AssignedRole = assignedRole,
            CreatedAt = DateTime.UtcNow,
            TaskData = taskData,
            IsCompleted = false
        };

        await _taskRepository.CreateAsync(task);
        
        // Notifier les utilisateurs du rôle
        // await _notificationService.NotifyRole(assignedRole, task);
        
        return taskId;
    }
}

// ----------------------------------------------------------------------------
// 3. CONFIGURATION ET INITIALISATION (CQRS PATTERN - RECOMMENDED)
// ----------------------------------------------------------------------------

public class BpmEngineSetup
{
    /// <summary>
    /// Creates engine with JSON file-based process definitions (RECOMMENDED)
    /// Process definitions stored as JSON files, runtime data in database
    /// </summary>
    public static ProcessEngine CreateEngineWithJsonDefinitions(
        string processDefinitionsPath,
        string connectionString)
    {
        // Repositories
        // - Process definitions: JSON files (easy to edit, version control friendly)
        // - Runtime data: Database (transactions, performance, querying)
        var processDefRepo = new JsonFileProcessDefinitionRepository(processDefinitionsPath);
        var processInstRepo = new OracleProcessInstanceRepository(connectionString);
        var stepInstRepo = new OracleStepInstanceRepository(connectionString);
        var taskRepo = new OracleTaskRepository(connectionString);

        // Services - CQRS pattern
        var httpClient = new HttpClient();
        var commandHandler = new ApplicationCommandHandler(httpClient);
        var queryHandler = new ApplicationQueryHandler(httpClient);
        var taskService = new CustomTaskService(taskRepo);
        var conditionEvaluator = new SimpleConditionEvaluator();

        // Handlers - using CQRS pattern
        var handlers = new IStepHandler[]
        {
            new BusinessStepHandler(commandHandler),
            new InteractiveStepHandler(taskService, taskRepo),
            new DecisionStepHandler(queryHandler, conditionEvaluator),
            new ScheduledStepHandler(),
            new SignalStepHandler(),
            new SubProcessStepHandler(processDefRepo, processInstRepo)
        };

        // Engine
        return new ProcessEngine(
            processDefRepo,
            processInstRepo,
            stepInstRepo,
            handlers);
    }

    /// <summary>
    /// Creates engine with database-based process definitions
    /// Uses ICommandHandler and IQueryHandler for business and decision steps
    /// </summary>
    public static ProcessEngine CreateEngine(string connectionString)
    {
        // Repositories
        var processDefRepo = new OracleProcessDefinitionRepository(connectionString);
        var processInstRepo = new OracleProcessInstanceRepository(connectionString);
        var stepInstRepo = new OracleStepInstanceRepository(connectionString);
        var taskRepo = new OracleTaskRepository(connectionString);

        // Services - CQRS pattern
        var httpClient = new HttpClient();
        var commandHandler = new ApplicationCommandHandler(httpClient);
        var queryHandler = new ApplicationQueryHandler(httpClient);
        var taskService = new CustomTaskService(taskRepo);
        var conditionEvaluator = new SimpleConditionEvaluator();

        // Handlers - using CQRS pattern
        var handlers = new IStepHandler[]
        {
            new BusinessStepHandler(commandHandler),           // Uses ICommandHandler
            new InteractiveStepHandler(taskService, taskRepo),
            new DecisionStepHandler(queryHandler, conditionEvaluator), // Uses IQueryHandler
            new ScheduledStepHandler(),
            new SignalStepHandler(),
            new SubProcessStepHandler(processDefRepo, processInstRepo)
        };

        // Engine
        return new ProcessEngine(
            processDefRepo,
            processInstRepo,
            stepInstRepo,
            handlers);
    }

    /// <summary>
    /// Creates engine with legacy pattern (deprecated)
    /// Uses IWebServiceClient for backward compatibility
    /// </summary>
    [Obsolete("Use CreateEngine() instead which uses CQRS pattern")]
    public static ProcessEngine CreateEngineLegacy(string connectionString)
    {
        // Repositories
        var processDefRepo = new OracleProcessDefinitionRepository(connectionString);
        var processInstRepo = new OracleProcessInstanceRepository(connectionString);
        var stepInstRepo = new OracleStepInstanceRepository(connectionString);
        var taskRepo = new OracleTaskRepository(connectionString);

        // Services - legacy pattern
        var httpClient = new HttpClient();
#pragma warning disable CS0618 // Type or member is obsolete
        var webServiceClient = new HttpWebServiceClient(httpClient);
        var taskService = new CustomTaskService(taskRepo);
        var conditionEvaluator = new SimpleConditionEvaluator();

        // Handlers - legacy pattern
        var handlers = new IStepHandler[]
        {
            new BusinessStepHandler(webServiceClient),
            new InteractiveStepHandler(taskService, taskRepo),
            new DecisionStepHandler(webServiceClient, conditionEvaluator),
            new ScheduledStepHandler(),
            new SignalStepHandler(),
            new SubProcessStepHandler(processDefRepo, processInstRepo)
        };
#pragma warning restore CS0618 // Type or member is obsolete

        // Engine
        return new ProcessEngine(
            processDefRepo,
            processInstRepo,
            stepInstRepo,
            handlers);
    }
}

// ----------------------------------------------------------------------------
// 4. EXEMPLE D'UTILISATION
// ----------------------------------------------------------------------------

public class Program
{
    public static async Task Main(string[] args)
    {
        // ========================================================================
        // EXAMPLE 1: Using JSON file repository for process definitions (RECOMMENDED)
        // ========================================================================

        var processDefinitionsPath = "./ProcessDefinitions";
        var connectionString = "Data Source=oracle-server;User Id=bpm_user;Password=xxx;";

        var engine = BpmEngineSetup.CreateEngineWithJsonDefinitions(
            processDefinitionsPath,
            connectionString);

        // Load process definition from JSON file
        var processDefRepo = new JsonFileProcessDefinitionRepository(processDefinitionsPath);

        // Get latest version of a process
        var processDef = await processDefRepo.GetByIdAsync("approbation-achat");
        Console.WriteLine($"Process: {processDef?.Name}, Version: {processDef?.Version}");

        // Get specific version
        var processDefV1 = await processDefRepo.GetByIdAsync("approbation-achat", 1);

        // Get all versions of a process
        var allVersions = await processDefRepo.GetVersionsAsync("approbation-achat");
        Console.WriteLine($"Found {allVersions.Count} versions");

        // Start a process instance
        var variables = new Dictionary<string, object>
        {
            ["montant"] = 1500,
            ["clientId"] = "C12345",
            ["produitId"] = "P98765"
        };

        var instanceId = await engine.StartProcessAsync("approbation-achat", variables);
        Console.WriteLine($"Processus démarré: {instanceId}");

        // ========================================================================
        // EXAMPLE 2: Creating and saving a new process definition
        // ========================================================================

        var newProcess = new ProcessDefinition
        {
            Id = "new-process",
            Name = "Nouveau Processus",
            Description = "Description du processus",
            StartStepId = "step1",
            Steps = new List<StepDefinition>
            {
                new BusinessStepDefinition
                {
                    Id = "step1",
                    Name = "Première étape",
                    Type = StepType.Business,
                    CommandName = "ExecuteFirstStep",
                    NextStepId = null
                }
            }
        };

        // Save as version 1 (auto-incremented if version = 0)
        await processDefRepo.SaveAsync(newProcess);
        Console.WriteLine($"Process saved: {newProcess.Id} v{newProcess.Version}");

        // ========================================================================
        // EXAMPLE 3: Scheduled steps and signals
        // ========================================================================

        // Job de traitement des étapes cédulées (à exécuter périodiquement)
        // await engine.ProcessScheduledStepsAsync();

        // Envoyer un signal
        // await engine.SendSignalAsync("produit-disponible", instanceId);

        // ========================================================================
        // FILE STRUCTURE EXAMPLE:
        // ========================================================================
        // ./ProcessDefinitions/
        //   ├── approbation-achat_v1.json
        //   ├── approbation-achat_v2.json
        //   ├── gestion-commande-complexe_v1.json
        //   └── commande-complete_v1.json
    }
}

// ----------------------------------------------------------------------------
// 5. SCHÉMA ORACLE SUGGÉRÉ
// ----------------------------------------------------------------------------

/*
-- Table des définitions de processus
CREATE TABLE PROC_DEF (
    PROC_ID VARCHAR2(100) NOT NULL,
    NOM VARCHAR2(200) NOT NULL,
    DESCRIPTION VARCHAR2(500),
    VERSION NUMBER NOT NULL,
    ETAPE_DEBUT VARCHAR2(100) NOT NULL,
    DEF_JSON CLOB NOT NULL,
    DATE_CRE TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT PK_PROC_DEF PRIMARY KEY (PROC_ID, VERSION)
);

-- Table des instances de processus
CREATE TABLE INST_PROC (
    INST_ID VARCHAR2(100) NOT NULL,
    PROC_ID VARCHAR2(100) NOT NULL,
    VERSION NUMBER NOT NULL,
    STATUT VARCHAR2(50) NOT NULL,
    DATE_DEBUT TIMESTAMP NOT NULL,
    DATE_FIN TIMESTAMP,
    ETAPE_CUR VARCHAR2(100),
    VARIABLES_JSON CLOB,
    MSG_ERR VARCHAR2(4000),
    INST_PROC_PARENT VARCHAR2(100),
    CONSTRAINT PK_INST_PROC PRIMARY KEY (INST_ID)
);

-- Table des instances d'étapes
CREATE TABLE INST_ETAPE (
    INST_ETAPE_ID VARCHAR2(100) NOT NULL,
    INST_PROC_ID VARCHAR2(100) NOT NULL,
    ETAPE_DEF_ID VARCHAR2(100) NOT NULL,
    TYPE_ETAPE VARCHAR2(50) NOT NULL,
    STATUT VARCHAR2(50) NOT NULL,
    DATE_DEBUT TIMESTAMP NOT NULL,
    DATE_FIN TIMESTAMP,
    DONNEES_IN_JSON CLOB,
    DONNEES_OUT_JSON CLOB,
    MSG_ERR VARCHAR2(4000),
    DATE_PREVUE TIMESTAMP,
    NOM_SIGNAL VARCHAR2(200),
    CONSTRAINT PK_INST_ETAPE PRIMARY KEY (INST_ETAPE_ID),
    CONSTRAINT FK_INST_ETAPE_PROC FOREIGN KEY (INST_PROC_ID) 
        REFERENCES INST_PROC(INST_ID)
);

-- Table des tâches
CREATE TABLE TACHE (
    TACHE_ID VARCHAR2(100) NOT NULL,
    INST_PROC_ID VARCHAR2(100) NOT NULL,
    INST_ETAPE_ID VARCHAR2(100) NOT NULL,
    TYPE_TACHE VARCHAR2(100) NOT NULL,
    ROLE_ASSIGNE VARCHAR2(100) NOT NULL,
    UTILISATEUR_ID VARCHAR2(100),
    DATE_CRE TIMESTAMP NOT NULL,
    DATE_COMPLETE TIMESTAMP,
    DONNEES_JSON CLOB,
    DONNEES_COMPLETE_JSON CLOB,
    EST_COMPLETE NUMBER(1) DEFAULT 0,
    CONSTRAINT PK_TACHE PRIMARY KEY (TACHE_ID),
    CONSTRAINT FK_TACHE_PROC FOREIGN KEY (INST_PROC_ID) 
        REFERENCES INST_PROC(INST_ID),
    CONSTRAINT FK_TACHE_ETAPE FOREIGN KEY (INST_ETAPE_ID) 
        REFERENCES INST_ETAPE(INST_ETAPE_ID)
);

-- Index
CREATE INDEX IDX_INST_PROC_STATUT ON INST_PROC(STATUT);
CREATE INDEX IDX_INST_PROC_DEF ON INST_PROC(PROC_ID, VERSION);
CREATE INDEX IDX_INST_ETAPE_PROC ON INST_ETAPE(INST_PROC_ID);
CREATE INDEX IDX_INST_ETAPE_PREVUE ON INST_ETAPE(DATE_PREVUE, STATUT);
CREATE INDEX IDX_INST_ETAPE_SIGNAL ON INST_ETAPE(NOM_SIGNAL, STATUT);
CREATE INDEX IDX_TACHE_ROLE ON TACHE(ROLE_ASSIGNE, EST_COMPLETE);
CREATE INDEX IDX_TACHE_USER ON TACHE(UTILISATEUR_ID);
*/
