# BPM Engine - Moteur de Workflow C#

## Structure du Projet

```
BpmEngine/
├── Core/
│   ├── Enums.cs                          # Types d'étapes et statuts
│   └── Models/
│       ├── ProcessDefinition.cs          # Modèles de définition JSON
│       └── ProcessInstance.cs            # Modèles d'instance runtime
├── Repository/
│   └── IRepositories.cs                  # Interfaces de persistence
├── Services/
│   ├── IServices.cs                      # Interfaces de services externes
│   └── Impl/
│       └── SimpleConditionEvaluator.cs   # Évaluateur de conditions
├── Handlers/
│   ├── IStepHandler.cs                   # Interface de base des handlers
│   ├── BusinessStepHandler.cs            # Handler étape affaire
│   ├── InteractiveStepHandler.cs         # Handler étape interactive
│   ├── DecisionStepHandler.cs            # Handler étape décision
│   ├── ScheduledStepHandler.cs           # Handler étape cédulée
│   ├── SignalStepHandler.cs              # Handler étape signal
│   └── SubProcessStepHandler.cs          # Handler sous-processus
├── Engine/
│   └── ProcessEngine.cs                  # Moteur d'exécution principal
├── Serialization/
│   └── ProcessDefinitionSerializer.cs    # Sérialisation JSON
└── Examples/
    ├── process-approbation-achat.json
    └── process-avec-subprocess.json
```

## Types d'Étapes

### 1. Étape Affaire (Business - Type 0)
- Appelle un service web pour exécuter la logique métier
- Linéaire (un seul nœud suivant)

### 2. Étape Interactive (Interactive - Type 1)
- Crée une tâche et l'assigne à un rôle
- Attend la complétion de la tâche
- Linéaire

### 3. Étape Décision (Decision - Type 2)
- Appelle un service pour obtenir des données
- Évalue des conditions pour choisir la route
- SEUL type permettant le branchement

### 4. Étape Cédulée/Attente (Scheduled - Type 3)
- Met en pause jusqu'à une date/heure
- Supporte délai ou date spécifique
- Linéaire

### 5. Étape Signal (Signal - Type 4)
- Attend un signal externe
- Supporte timeout optionnel
- Linéaire

### 6. Étape Sous-Processus (SubProcess - Type 5)
- Exécute un processus réutilisable
- Mapping des variables entrée/sortie
- Linéaire

## Implémentation Client

Le client doit implémenter les interfaces suivantes:

### 1. Repositories
```csharp
IProcessDefinitionRepository
IProcessInstanceRepository
IStepInstanceRepository
ITaskRepository
```

### 2. Services
```csharp
IWebServiceClient       // Appels de services web
ITaskService           // Création de tâches
IConditionEvaluator    // Évaluation de conditions (ou utiliser SimpleConditionEvaluator)
```

## Utilisation

### Initialisation
```csharp
var engine = new ProcessEngine(
    processDefinitionRepository,
    processInstanceRepository,
    stepInstanceRepository,
    new IStepHandler[] {
        new BusinessStepHandler(webServiceClient),
        new InteractiveStepHandler(taskService, taskRepository),
        new DecisionStepHandler(webServiceClient, conditionEvaluator),
        new ScheduledStepHandler(),
        new SignalStepHandler(),
        new SubProcessStepHandler(processDefinitionRepository, processInstanceRepository)
    });
```

### Démarrer un Processus
```csharp
var variables = new Dictionary<string, object>
{
    ["montant"] = 1500,
    ["clientId"] = "C123"
};

var instanceId = await engine.StartProcessAsync(
    "approbation-achat", 
    variables);
```

### Compléter une Tâche
```csharp
await taskRepository.UpdateAsync(new TaskInstance 
{
    Id = taskId,
    IsCompleted = true,
    CompletionData = new Dictionary<string, object>
    {
        ["approuve"] = true,
        ["commentaire"] = "OK"
    }
});

await engine.ExecuteProcessAsync(processInstanceId);
```

### Envoyer un Signal
```csharp
await engine.SendSignalAsync("produit-disponible", processInstanceId);
```

### Traiter les Étapes Cédulées
```csharp
// À exécuter périodiquement (ex: chaque minute)
await engine.ProcessScheduledStepsAsync();
```

## Format JSON des Processus

Les définitions de processus sont des fichiers JSON simples et lisibles.
Voir les exemples dans le dossier `Examples/`.

### Types d'Étapes (Enum)
- 0 = Business
- 1 = Interactive
- 2 = Decision
- 3 = Scheduled
- 4 = Signal
- 5 = SubProcess

## Package NuGet

Pour générer le package:
```bash
dotnet pack -c Release
```

Le package sera créé dans `bin/Release/`.
