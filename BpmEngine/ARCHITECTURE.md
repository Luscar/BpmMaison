# Architecture BPM Engine

## Principes de Design

### 1. Séparation des Responsabilités

- **Core/Models**: Définitions de domaine pures (POCO)
- **Repository**: Interfaces pour la persistence (client implémente)
- **Services**: Interfaces pour services externes (client implémente)
- **Handlers**: Logique d'exécution des étapes (fourni par le moteur)
- **Engine**: Orchestration du workflow (fourni par le moteur)

### 2. Inversion de Dépendances

Le moteur dépend d'**interfaces** que le client implémente:
- Repositories → Permet choix de BD (Oracle, SQL Server, etc.)
- Services → Permet intégration avec systèmes existants
- ConditionEvaluator → Permet logique d'évaluation personnalisée

### 3. Pattern Handler

Chaque type d'étape a son propre handler:
- Responsabilité unique
- Facilement testable
- Extensible (ajout de nouveaux types)

### 4. Pattern Repository

Les repositories abstraient la persistence:
- Le client décide du schéma de BD
- Le client décide des noms de tables
- Le client gère les transactions
- Le client implémente les stratégies de cache

### 5. Workflow Linéaire avec Point de Décision Unique

Design volontaire pour simplifier:
- Pas de concept de "transitions"
- Étapes linéaires par défaut
- SEULE l'étape Decision permet le branchement
- Graphe plus simple à visualiser et debugger

## Flux d'Exécution

```
1. StartProcessAsync()
   ↓
2. Créer ProcessInstance
   ↓
3. Sauver via IProcessInstanceRepository
   ↓
4. ExecuteProcessAsync()
   ↓
5. Boucle sur les étapes:
   - Trouver StepDefinition
   - Créer/Récupérer StepInstance
   - Trouver Handler approprié
   - Exécuter Handler
   - Traiter le résultat:
     * Si RequiresWait → Pause (WaitingForTask, WaitingForSchedule, etc.)
     * Si IsCompleted → Continuer à l'étape suivante
     * Si Erreur → Marquer processus Failed
   ↓
6. Processus Completed ou Waiting
```

## Gestion des États

### ProcessStatus
- **NotStarted**: Défini mais pas démarré
- **Running**: En cours d'exécution
- **Waiting**: En attente (tâche, signal, schedule)
- **Completed**: Terminé avec succès
- **Failed**: Erreur fatale
- **Cancelled**: Annulé manuellement

### StepStatus
- **NotStarted**: Pas encore exécuté
- **Running**: En cours
- **WaitingForTask**: Attend complétion de tâche
- **WaitingForSchedule**: Attend date/heure
- **WaitingForSignal**: Attend signal externe
- **Completed**: Terminé
- **Failed**: Erreur
- **Skipped**: Ignoré (futur usage)

## Sérialisation JSON

### Polymorphisme des StepDefinition

Utilisation de `JsonConverter` personnalisé:
- Lecture: Détecte le Type et désérialise vers la classe appropriée
- Écriture: Sérialise en utilisant le type réel

### Format Simple

JSON volontairement simple:
- Lisible par les humains
- Éditable manuellement
- Versionnable dans Git
- Pas de XML complexe

## Points d'Extension

### 1. Nouveaux Types d'Étapes

Pour ajouter un nouveau type:
1. Ajouter enum dans `StepType`
2. Créer `XxxStepDefinition : StepDefinition`
3. Créer `XxxStepHandler : IStepHandler`
4. Enregistrer le handler dans le moteur
5. Mettre à jour le `StepDefinitionConverter`

### 2. Évaluation de Conditions Avancée

Le `SimpleConditionEvaluator` fourni est basique.
Le client peut implémenter `IConditionEvaluator` pour:
- Expressions complexes
- Fonctions personnalisées
- Intégration avec rule engine
- Support de DSL spécifique

### 3. Stratégies de Retry

Non implémenté par défaut, mais peut être ajouté:
- Dans les handlers individuels
- Via un wrapper de handler
- Dans l'implémentation des services

### 4. Monitoring et Observabilité

Le client peut implémenter:
- Logging dans les repositories
- Métriques dans les handlers
- Tracing distribué via les services
- Hooks d'événements

## Considérations de Performance

### 1. Requêtes de BD

Le moteur fait des requêtes séquentielles:
- Une requête par étape
- Pas de batch par défaut
- Le client peut optimiser dans ses repositories

### 2. Transactions

Non géré par le moteur:
- Le client décide de la stratégie
- Peut implémenter Unit of Work
- Peut grouper les opérations

### 3. Concurrence

Pas de locking par défaut:
- Le client doit gérer la concurrence
- Optimistic locking recommandé
- Utiliser versions ou timestamps

### 4. Cache

Non implémenté par défaut:
- ProcessDefinitions peuvent être cachées
- Le client implémente selon ses besoins

## Sécurité

### 1. Validation

Non implémentée par défaut:
- Validation des définitions de processus
- Validation des données d'entrée
- Le client doit implémenter

### 2. Autorisation

Délégué au client:
- Qui peut démarrer quels processus?
- Qui peut compléter quelles tâches?
- Le client gère via `ITaskService`

### 3. Audit

Le client peut implémenter:
- Logging des changements d'état
- Traçabilité des actions
- Rétention des historiques

## Patterns Recommandés

### 1. Saga Pattern

Pour transactions distribuées:
- Utiliser sous-processus pour étapes compensables
- Gérer les rollbacks via étapes décision

### 2. Event-Driven

Pour intégrations asynchrones:
- Utiliser étapes Signal
- Publier événements dans les handlers
- Consommer événements externes

### 3. Compensation

Pour gérer les erreurs:
- Définir processus de compensation
- Déclencher via étapes décision
- Tracer l'état original

## Limitations Connues

1. **Pas de parallélisme**: Une seule branche d'exécution
2. **Pas de boucles explicites**: Peut créer des cycles via Decision
3. **Pas de versioning automatique**: Le client gère les versions
4. **Pas de migration**: Processus en cours sur ancienne version
5. **Pas de timeout global**: Seulement sur Signal et Schedule

## Évolutions Futures Possibles

1. Support de branches parallèles
2. Étape "Fork/Join"
3. Sous-processus événementiels
4. Corrélation de messages
5. Gestion de versions automatique
6. Migration de processus en cours
7. Timeout configurable par étape
8. Retry policies intégrées
