# BPM Engine - Structure du Projet

## Vue d'ensemble

Moteur de workflow/processus BPM en C# sous forme de package NuGet avec support complet pour 6 types d'étapes de workflow.

## Structure des Fichiers

```
BpmEngine/
│
├── 📄 BpmEngine.csproj                   # Fichier projet .NET 8.0
├── 📄 BpmEngine.sln                      # Solution Visual Studio
├── 📄 BpmEngine.nuspec                   # Configuration NuGet
├── 📄 .gitignore                         # Exclusions Git
├── 📄 README.md                          # Documentation principale
├── 📄 ARCHITECTURE.md                    # Design et architecture détaillée
├── 📄 IMPLEMENTATION_EXAMPLE.cs          # Exemple complet d'implémentation client
├── 📄 build.ps1                          # Script de build Windows
├── 📄 build.sh                           # Script de build Linux/Mac
│
├── 📁 Core/                              # Modèles de domaine
│   ├── Enums.cs                          # Types d'étapes et statuts
│   └── Models/
│       ├── ProcessDefinition.cs          # Définitions de processus (JSON)
│       └── ProcessInstance.cs            # Instances runtime
│
├── 📁 Repository/                        # Interfaces de persistence
│   └── IRepositories.cs                  # À implémenter par le client
│       - IProcessDefinitionRepository
│       - IProcessInstanceRepository
│       - IStepInstanceRepository
│       - ITaskRepository
│
├── 📁 Services/                          # Services externes
│   ├── IServices.cs                      # Interfaces à implémenter
│   │   - IWebServiceClient
│   │   - ITaskService
│   │   - IConditionEvaluator
│   └── Impl/
│       └── SimpleConditionEvaluator.cs   # Implémentation basique fournie
│
├── 📁 Handlers/                          # Handlers d'étapes (fournis)
│   ├── IStepHandler.cs                   # Interface de base
│   ├── BusinessStepHandler.cs            # Étape Affaire
│   ├── InteractiveStepHandler.cs         # Étape Interactive
│   ├── DecisionStepHandler.cs            # Étape Décision
│   ├── ScheduledStepHandler.cs           # Étape Cédulée/Attente
│   ├── SignalStepHandler.cs              # Étape Signal
│   └── SubProcessStepHandler.cs          # Étape Sous-Processus
│
├── 📁 Engine/                            # Moteur d'orchestration
│   └── ProcessEngine.cs                  # Moteur principal
│
├── 📁 Serialization/                     # Gestion JSON
│   └── ProcessDefinitionSerializer.cs    # Sérialisation polymorphique
│
└── 📁 Examples/                          # Exemples de processus JSON
    ├── process-approbation-achat.json    # Processus complet
    └── process-avec-subprocess.json      # Avec sous-processus
```

## Types d'Étapes (StepType)

| Type | Enum | Description | Linéaire |
|------|------|-------------|----------|
| **Business** | 0 | Appel service web pour logique métier | ✓ |
| **Interactive** | 1 | Création de tâche assignée à un rôle | ✓ |
| **Decision** | 2 | Query + conditions pour routage | ✗ (branchement) |
| **Scheduled** | 3 | Pause jusqu'à date/heure | ✓ |
| **Signal** | 4 | Attente d'un signal externe | ✓ |
| **SubProcess** | 5 | Processus réutilisable | ✓ |

## Ce que le Client Doit Implémenter

### 1. Repositories (Oracle, SQL Server, etc.)
- `IProcessDefinitionRepository` - Stockage des définitions
- `IProcessInstanceRepository` - Instances en cours
- `IStepInstanceRepository` - Historique des étapes
- `ITaskRepository` - Gestion des tâches

### 2. Services
- `IWebServiceClient` - Appels HTTP aux services métier
- `ITaskService` - Création de tâches dans l'application
- `IConditionEvaluator` - Évaluation de conditions (optionnel)

### 3. Configuration
- Injection de dépendances
- Connection strings
- Initialisation du moteur

## Workflow d'Utilisation

1. **Installation**
   ```bash
   dotnet add package BpmEngine
   ```

2. **Implémentation des interfaces**
   - Voir `IMPLEMENTATION_EXAMPLE.cs`

3. **Configuration**
   ```csharp
   var engine = new ProcessEngine(
       processDefRepo,
       processInstRepo,
       stepInstRepo,
       handlers);
   ```

4. **Démarrage d'un processus**
   ```csharp
   var instanceId = await engine.StartProcessAsync(
       "mon-processus",
       variables);
   ```

5. **Gestion des événements**
   - Complétion de tâches → `ExecuteProcessAsync()`
   - Envoi de signaux → `SendSignalAsync()`
   - Traitement cédulé → `ProcessScheduledStepsAsync()`

## Build du Package

### Windows
```powershell
.\build.ps1
```

### Linux/Mac
```bash
chmod +x build.sh
./build.sh
```

Package généré dans: `./nupkg/BpmEngine.1.0.0.nupkg`

## Schéma Base de Données Suggéré

Voir `IMPLEMENTATION_EXAMPLE.cs` pour:
- Schéma Oracle complet
- Nomenclature de tables
- Index recommandés
- Contraintes de clés étrangères

Tables principales:
- `PROC_DEF` - Définitions de processus
- `INST_PROC` - Instances de processus
- `INST_ETAPE` - Instances d'étapes
- `TACHE` - Tâches utilisateur

## Format JSON des Processus

Les processus sont définis en JSON simple et lisible:

```json
{
  "id": "mon-processus",
  "name": "Mon Processus",
  "version": 1,
  "startStepId": "premiere-etape",
  "steps": [
    {
      "id": "premiere-etape",
      "name": "Première Étape",
      "type": 0,
      "serviceUrl": "https://...",
      "nextStepId": "deuxieme-etape"
    }
  ]
}
```

Voir les exemples dans `Examples/` pour des cas complets.

## Points Clés

✅ **Architecture Flexible** - Le client contrôle la persistence
✅ **Workflow Linéaire** - Pas de transitions, simplifié
✅ **Point de Décision Unique** - Seule l'étape Decision branche
✅ **JSON Lisible** - Format simple et éditable
✅ **Handlers Extensibles** - Facile d'ajouter des types
✅ **Pas de Dépendances Lourdes** - Seulement System.Text.Json
✅ **Support Oracle** - Conçu pour Oracle mais adaptable

## Documentation

- `README.md` - Guide de démarrage rapide
- `ARCHITECTURE.md` - Design et patterns
- `IMPLEMENTATION_EXAMPLE.cs` - Code d'exemple complet
- `Examples/` - Définitions de processus JSON

## Support

Le moteur est fourni "tel quel" avec le code source complet.
Le client a la responsabilité d'implémenter les interfaces selon ses besoins.

## Licence

À définir par votre organisation.
