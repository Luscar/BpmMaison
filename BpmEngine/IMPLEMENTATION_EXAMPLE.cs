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
// 1. IMPLÉMENTATION DES REPOSITORIES (ORACLE)
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
// 2. IMPLÉMENTATION DES SERVICES
// ----------------------------------------------------------------------------

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
// 3. CONFIGURATION ET INITIALISATION
// ----------------------------------------------------------------------------

public class BpmEngineSetup
{
    public static ProcessEngine CreateEngine(string connectionString)
    {
        // Repositories
        var processDefRepo = new OracleProcessDefinitionRepository(connectionString);
        var processInstRepo = new OracleProcessInstanceRepository(connectionString);
        var stepInstRepo = new OracleStepInstanceRepository(connectionString);
        var taskRepo = new OracleTaskRepository(connectionString);

        // Services
        var httpClient = new HttpClient();
        var webServiceClient = new HttpWebServiceClient(httpClient);
        var taskService = new CustomTaskService(taskRepo);
        var conditionEvaluator = new SimpleConditionEvaluator();

        // Handlers
        var handlers = new IStepHandler[]
        {
            new BusinessStepHandler(webServiceClient),
            new InteractiveStepHandler(taskService, taskRepo),
            new DecisionStepHandler(webServiceClient, conditionEvaluator),
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
}

// ----------------------------------------------------------------------------
// 4. EXEMPLE D'UTILISATION
// ----------------------------------------------------------------------------

public class Program
{
    public static async Task Main(string[] args)
    {
        var connectionString = "Data Source=oracle-server;User Id=bpm_user;Password=xxx;";
        var engine = BpmEngineSetup.CreateEngine(connectionString);

        // Démarrer un processus
        var variables = new Dictionary<string, object>
        {
            ["montant"] = 1500,
            ["clientId"] = "C12345",
            ["produitId"] = "P98765"
        };

        var instanceId = await engine.StartProcessAsync(
            "approbation-achat",
            variables);

        Console.WriteLine($"Processus démarré: {instanceId}");

        // Job de traitement des étapes cédulées (à exécuter périodiquement)
        // await engine.ProcessScheduledStepsAsync();

        // Envoyer un signal
        // await engine.SendSignalAsync("produit-disponible", instanceId);
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
