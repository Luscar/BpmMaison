# Changelog

Tous les changements notables à ce projet seront documentés dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/lang/fr/).

## [1.0.0] - 2024-11-12

### Ajouté
- Moteur de workflow BPM complet
- Support de 6 types d'étapes:
  - Étape Affaire (Business)
  - Étape Interactive
  - Étape Décision
  - Étape Cédulée/Attente (Scheduled)
  - Étape Signal
  - Étape Sous-Processus (SubProcess)
- Interfaces de repository pour persistence personnalisée
- Interfaces de services pour intégration externe
- Handlers pour chaque type d'étape
- Moteur d'exécution de processus (`ProcessEngine`)
- Sérialisation/Désérialisation JSON avec support polymorphique
- Évaluateur de conditions simple (`SimpleConditionEvaluator`)
- Documentation complète:
  - README.md
  - ARCHITECTURE.md
  - IMPLEMENTATION_EXAMPLE.cs
  - PROJECT_OVERVIEW.md
- Exemples de définitions de processus JSON
- Scripts de build (PowerShell et Bash)
- Configuration NuGet (.csproj et .nuspec)

### Caractéristiques
- Architecture avec inversion de dépendances
- Pattern Handler pour extensibilité
- Workflow linéaire avec point de décision unique
- Format JSON lisible pour les processus
- Pas de dépendances lourdes (seulement System.Text.Json)
- Compatible .NET 8.0
- Support pour Oracle et autres bases de données

### Notes de version
Cette version initiale fournit un moteur BPM fonctionnel avec toutes les fonctionnalités de base.
Le client doit implémenter les interfaces de repository et de services pour son environnement spécifique.

[1.0.0]: https://github.com/votre-organisation/bpm-engine/releases/tag/v1.0.0
