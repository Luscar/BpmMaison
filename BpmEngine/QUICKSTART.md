# Guide de Démarrage Rapide BPM Engine

## ⚡ Installation (5 minutes)

### 1. Ajouter le package à votre projet
```bash
# Si publié sur NuGet
dotnet add package BpmEngine

# Ou référencer le .nupkg localement
dotnet add reference path/to/BpmEngine.1.0.0.nupkg
```

### 2. Créer les tables dans Oracle
```sql
-- Exécuter le script SQL fourni dans IMPLEMENTATION_EXAMPLE.cs
-- Créer les tables: PROC_DEF, INST_PROC, INST_ETAPE, TACHE
```

## 🔧 Implémentation Minimale (20 minutes)

### Étape 1: Implémenter les Repositories

```csharp
public class OracleProcessInstanceRepository : IProcessInstanceRepository
{
    private readonly string _connectionString;
    
    public OracleProcessInstanceRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<ProcessInstance> CreateAsync(ProcessInstance instance)
    {
        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();
        
        const string sql = @"
            INSERT INTO INST_PROC 
            (INST_ID, PROC_ID, VERSION, STATUT, DATE_DEBUT, ETAPE_CUR, VARIABLES_JSON)
            VALUES 
            (:InstId, :ProcId, :Version, :Status, SYSTIMESTAMP, :CurrentStep, :Variables)";
        
        using var command = new OracleCommand(sql, connection);
        command.Parameters.Add("InstId", instance.Id);
        command.Parameters.Add("ProcId", instance.ProcessDefinitionId);
        command.Parameters.Add("Version", instance.ProcessVersion);
        command.Parameters.Add("Status", instance.Status.ToString());
        command.Parameters.Add("CurrentStep", instance.CurrentStepId);
        command.Parameters.Add("Variables", JsonSerializer.Serialize(instance.Variables));
        
        await command.ExecuteNonQueryAsync();
        return instance;
    }
    
    // Implémenter les autres méthodes...
}
```

### Étape 2: Implémenter les Services

```csharp
public class HttpWebServiceClient : IWebServiceClient
{
    private readonly HttpClient _httpClient;
    
    public HttpWebServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<Dictionary<string, object>> CallAsync(
        string url, string method, Dictionary<string, object>? parameters = null)
    {
        var json = JsonSerializer.Serialize(parameters);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        HttpResponseMessage response = method.ToUpper() switch
        {
            "GET" => await _httpClient.GetAsync(url),
            "POST" => await _httpClient.PostAsync(url, content),
            "PUT" => await _httpClient.PutAsync(url, content),
            _ => throw new NotSupportedException($"Méthode HTTP non supportée: {method}")
        };
        
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson) 
            ?? new Dictionary<string, object>();
    }
}
```

### Étape 3: Configurer le Moteur

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connectionString = Configuration.GetConnectionString("Oracle");
        
        // Repositories
        services.AddSingleton<IProcessDefinitionRepository>(
            new OracleProcessDefinitionRepository(connectionString));
        services.AddSingleton<IProcessInstanceRepository>(
            new OracleProcessInstanceRepository(connectionString));
        services.AddSingleton<IStepInstanceRepository>(
            new OracleStepInstanceRepository(connectionString));
        services.AddSingleton<ITaskRepository>(
            new OracleTaskRepository(connectionString));
        
        // Services
        services.AddHttpClient<IWebServiceClient, HttpWebServiceClient>();
        services.AddSingleton<ITaskService, CustomTaskService>();
        services.AddSingleton<IConditionEvaluator, SimpleConditionEvaluator>();
        
        // Handlers
        services.AddSingleton<IStepHandler, BusinessStepHandler>();
        services.AddSingleton<IStepHandler, InteractiveStepHandler>();
        services.AddSingleton<IStepHandler, DecisionStepHandler>();
        services.AddSingleton<IStepHandler, ScheduledStepHandler>();
        services.AddSingleton<IStepHandler, SignalStepHandler>();
        services.AddSingleton<IStepHandler, SubProcessStepHandler>();
        
        // Engine
        services.AddSingleton<ProcessEngine>();
    }
}
```

## 🚀 Premier Processus (10 minutes)

### 1. Créer une définition de processus JSON

```json
{
  "id": "hello-world",
  "name": "Hello World Process",
  "version": 1,
  "startStepId": "step1",
  "steps": [
    {
      "id": "step1",
      "name": "Appeler Service",
      "type": 0,
      "serviceUrl": "https://api.monapp.com/hello",
      "method": "POST",
      "parameters": {
        "message": "Hello BPM!"
      },
      "nextStepId": null
    }
  ]
}
```

### 2. Charger et sauvegarder la définition

```csharp
var json = File.ReadAllText("hello-world.json");
var definition = ProcessDefinitionSerializer.Deserialize(json);
await processDefinitionRepository.SaveAsync(definition);
```

### 3. Démarrer le processus

```csharp
var engine = serviceProvider.GetRequiredService<ProcessEngine>();

var variables = new Dictionary<string, object>
{
    ["userId"] = "user123",
    ["timestamp"] = DateTime.UtcNow
};

var instanceId = await engine.StartProcessAsync("hello-world", variables);
Console.WriteLine($"Processus démarré: {instanceId}");
```

## 📋 Cas d'Usage Courants

### Processus avec Approbation

```csharp
// 1. Démarrer le processus
var instanceId = await engine.StartProcessAsync("approbation-achat", new Dictionary<string, object>
{
    ["montant"] = 1500,
    ["demandeur"] = "jean.dupont"
});

// 2. L'étape interactive crée une tâche automatiquement
// 3. L'utilisateur complète la tâche dans votre application

// 4. Marquer la tâche comme complétée
var task = await taskRepository.GetByIdAsync(taskId);
task.IsCompleted = true;
task.CompletionData = new Dictionary<string, object>
{
    ["approuve"] = true,
    ["commentaire"] = "Approuvé"
};
await taskRepository.UpdateAsync(task);

// 5. Reprendre l'exécution du processus
await engine.ExecuteProcessAsync(instanceId);
```

### Processus avec Décision

```json
{
  "id": "step-decision",
  "type": 2,
  "queryServiceUrl": "https://api.monapp.com/check-amount",
  "routes": [
    {
      "targetStepId": "approbation-auto",
      "condition": "montant < 1000",
      "priority": 1
    },
    {
      "targetStepId": "approbation-manuelle",
      "condition": "montant >= 1000",
      "priority": 2
    }
  ]
}
```

### Processus avec Attente

```csharp
// Le processus attend automatiquement 24h
{
  "id": "attendre",
  "type": 3,
  "delayHours": 24,
  "nextStepId": "suite"
}

// Job à exécuter périodiquement (ex: chaque minute)
await engine.ProcessScheduledStepsAsync();
```

### Processus avec Signal

```csharp
// Le processus attend un signal
{
  "id": "attendre-signal",
  "type": 4,
  "signalName": "paiement-recu",
  "timeoutMinutes": 2880,
  "nextStepId": "suite"
}

// Envoyer le signal depuis votre application
await engine.SendSignalAsync("paiement-recu", processInstanceId);
```

## 🔍 Surveillance et Debugging

### Vérifier le statut d'un processus

```csharp
var instance = await processInstanceRepository.GetByIdAsync(instanceId);
Console.WriteLine($"Statut: {instance.Status}");
Console.WriteLine($"Étape courante: {instance.CurrentStepId}");
Console.WriteLine($"Variables: {JsonSerializer.Serialize(instance.Variables)}");
```

### Lister les tâches en attente

```csharp
var tasks = await taskRepository.GetPendingByRoleAsync("superviseur");
foreach (var task in tasks)
{
    Console.WriteLine($"Tâche {task.Id}: {task.TaskType}");
}
```

### Historique des étapes

```csharp
var steps = await stepInstanceRepository.GetByProcessInstanceIdAsync(instanceId);
foreach (var step in steps)
{
    Console.WriteLine($"{step.StepDefinitionId}: {step.Status} ({step.StartedAt})");
}
```

## 📚 Prochaines Étapes

1. **Lire la documentation complète**
   - `README.md` - Vue d'ensemble
   - `ARCHITECTURE.md` - Design patterns et décisions
   - `IMPLEMENTATION_EXAMPLE.cs` - Code complet d'exemple

2. **Adapter à votre environnement**
   - Implémenter tous les repositories
   - Configurer vos services web
   - Personnaliser l'évaluateur de conditions

3. **Créer vos processus**
   - Définir vos workflows en JSON
   - Tester avec des processus simples
   - Itérer et améliorer

4. **Mettre en production**
   - Configurer les jobs pour les étapes cédulées
   - Implémenter le monitoring
   - Configurer les logs et alertes

## 🆘 Aide

### Problèmes Courants

**Le processus ne démarre pas**
→ Vérifier que la définition existe dans la base de données
→ Vérifier les logs d'erreur dans `ProcessInstance.ErrorMessage`

**Les tâches ne se complètent pas**
→ Vérifier que `IsCompleted = true` est bien sauvegardé
→ Appeler `ExecuteProcessAsync()` après avoir mis à jour la tâche

**Les étapes cédulées ne s'exécutent pas**
→ S'assurer que `ProcessScheduledStepsAsync()` est appelé régulièrement
→ Vérifier que `ScheduledFor` est dans le passé

**Les signaux ne fonctionnent pas**
→ Vérifier que le nom du signal correspond exactement
→ Vérifier que l'étape est bien en statut `WaitingForSignal`

## 📞 Support

Pour plus d'aide, consulter:
- Les exemples dans `Examples/`
- Le code d'implémentation dans `IMPLEMENTATION_EXAMPLE.cs`
- La documentation d'architecture dans `ARCHITECTURE.md`
