# Outil de Visualisation de Processus BPM

## 📊 Vue d'ensemble

L'outil de visualisation permet de générer des diagrammes interactifs et élégants à partir de vos définitions de processus JSON.

## 🎯 Fonctionnalités

### ✅ Formats de sortie supportés
- **HTML** : Page web standalone avec diagramme interactif Mermaid.js
- **Mermaid** : Code Mermaid.js pour intégration dans votre documentation

### ✅ Caractéristiques visuelles
- 🎨 Couleurs distinctes pour chaque type d'étape
- 📐 Formes différentes selon le type d'étape
- 🔍 Zoom et navigation dans les grands diagrammes
- 📱 Responsive - fonctionne sur mobile
- 💡 Légende interactive
- 📊 Statistiques du processus

## 🚀 Utilisation

### Option 1: Visualiseur HTML Standalone (Recommandé)

Le fichier `visualizer.html` est un outil web complet qui fonctionne sans installation:

1. **Ouvrir le fichier** dans votre navigateur
   ```
   visualizer.html
   ```

2. **Charger un processus**
   - Cliquez sur "Sélectionner un fichier JSON"
   - Ou utilisez les boutons "Exemple Simple" / "Exemple Complexe"

3. **Actions disponibles**
   - 💾 Exporter le code Mermaid
   - 📥 Télécharger la visualisation en HTML

### Option 2: API C# (Intégration programmatique)

Utiliser la classe `ProcessVisualizer` dans votre code:

```csharp
using BpmEngine.Visualization;
using BpmEngine.Serialization;

// Charger la définition
var definition = ProcessDefinitionSerializer.DeserializeFromFile("process.json");

// Créer le visualiseur
var visualizer = new ProcessVisualizer();

// Générer HTML
visualizer.ExportToFile(definition, "output.html", "html");

// Ou générer Mermaid
visualizer.ExportToFile(definition, "output.mmd", "mermaid");

// Ou obtenir le code directement
string mermaidCode = visualizer.GenerateMermaidDiagram(definition);
string htmlContent = visualizer.GenerateHtmlVisualization(definition);
```

### Option 3: Ligne de commande (CLI)

Créer un projet console pour utiliser l'outil en ligne de commande:

```bash
# Générer en HTML (défaut)
dotnet run --project Visualizer.csproj -- process.json

# Générer en Mermaid
dotnet run --project Visualizer.csproj -- process.json mermaid

# Spécifier le fichier de sortie
dotnet run --project Visualizer.csproj -- process.json html my-diagram.html
```

## 📋 Exemple de Résultat

### Processus Complexe Inclus

Le fichier `process-complexe.json` démontre toutes les capacités du moteur:

**Statistiques:**
- ✅ 45 étapes au total
- ⚙️ 17 étapes Affaire
- 👤 8 étapes Interactive
- ❓ 8 points de Décision
- ⏰ 4 étapes Cédulée
- 📡 6 étapes Signal
- 📦 2 Sous-Processus

**Scénario:**
Gestion complète d'une commande client avec:
- Vérification d'inventaire
- Approbations multi-niveaux
- Traitement de paiement (sous-processus)
- Préparation et contrôle qualité
- Modes de livraison multiples
- Gestion des exceptions
- Suivi et clôture

### Visualisations Générées

Deux fichiers sont inclus comme exemples:

1. **[process-complexe-diagram.html](computer:///mnt/user-data/outputs/BpmEngine/process-complexe-diagram.html)**
   - Diagramme interactif complet
   - Statistiques et informations
   - Légende des types d'étapes
   - Prêt à partager avec les parties prenantes

2. **process-complexe.mermaid**
   - Code Mermaid.js pur
   - Pour intégration dans Markdown, Confluence, etc.
   - Compatible avec GitHub, GitLab, Notion

## 🎨 Code des Couleurs

Chaque type d'étape a sa propre couleur pour faciliter la lecture:

| Type | Couleur | Forme | Icône |
|------|---------|-------|-------|
| **Affaire** | 🔵 Bleu clair | Rectangle | ⚙️ |
| **Interactive** | 🟣 Violet | Arrondi | 👤 |
| **Décision** | 🟡 Jaune | Losange | ❓ |
| **Cédulée** | 🟢 Vert | Stade | ⏰ |
| **Signal** | 🔴 Rose | Stade | 📡 |
| **Sous-Processus** | 🟦 Turquoise | Double bordure | 📦 |

## 💡 Conseils d'Utilisation

### Pour les Présentations
1. Ouvrir le fichier HTML généré
2. Utiliser le mode plein écran (F11)
3. Zoom avec molette de la souris ou pinch

### Pour la Documentation
1. Exporter en Mermaid
2. Intégrer dans Markdown:
   ````markdown
   ```mermaid
   [coller le code ici]
   ```
   ````

### Pour le Partage
1. Générer le HTML
2. Héberger sur un serveur web ou partager le fichier
3. Pas de dépendances externes après génération

## 🔧 Intégration dans CI/CD

Exemple de script pour générer automatiquement les diagrammes:

```bash
#!/bin/bash
# generate-all-diagrams.sh

for file in processes/*.json; do
    echo "Génération de $file..."
    dotnet run --project Visualizer.csproj -- "$file" html "docs/diagrams/$(basename $file .json).html"
done

echo "Tous les diagrammes ont été générés dans docs/diagrams/"
```

Ajouter à votre pipeline:
```yaml
# .github/workflows/generate-diagrams.yml
name: Generate Process Diagrams

on:
  push:
    paths:
      - 'processes/**/*.json'

jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
      - name: Generate Diagrams
        run: ./generate-all-diagrams.sh
      - name: Commit Diagrams
        run: |
          git config --local user.email "action@github.com"
          git config --local user.name "GitHub Action"
          git add docs/diagrams/
          git commit -m "Auto-generate process diagrams" || exit 0
          git push
```

## 📚 Ressources

### Fichiers Inclus
- `ProcessVisualizer.cs` - API C# de visualisation
- `visualizer.html` - Outil web standalone
- `VisualizerCli.cs` - Outil ligne de commande
- `process-complexe.json` - Exemple de workflow complexe
- `process-complexe-diagram.html` - Visualisation générée

### Format Mermaid.js
Pour plus d'informations sur Mermaid.js:
- Documentation: https://mermaid.js.org/
- Éditeur en ligne: https://mermaid.live/
- GitHub support: Natif dans les fichiers .md

## ⚙️ Configuration Avancée

### Personnalisation des Couleurs

Modifier les styles dans `ProcessVisualizer.cs`:

```csharp
private void AddStepStyles(StringBuilder sb)
{
    // Personnaliser les couleurs ici
    sb.AppendLine("classDef businessStep fill:#VOTRE_COULEUR,stroke:#BORDURE");
    // ...
}
```

### Personnalisation du Template HTML

Le template HTML peut être extrait et personnalisé selon vos besoins:
- Logo de l'entreprise
- Couleurs corporate
- Sections additionnelles
- Intégration avec votre design system

## 🐛 Dépannage

**Le diagramme ne s'affiche pas**
→ Vérifier la console du navigateur (F12)
→ S'assurer que le JSON est valide
→ Vérifier la connexion internet (pour CDN Mermaid.js)

**Le diagramme est trop large**
→ Le container s'adapte automatiquement
→ Utiliser le scroll horizontal
→ Zoomer/dézoomer avec la molette

**Erreur "StepDefinition introuvable"**
→ Vérifier que tous les `nextStepId` référencent des étapes existantes
→ Vérifier que `startStepId` existe dans la liste des étapes

## 📞 Support

Pour les questions sur la visualisation, référez-vous à:
- `ARCHITECTURE.md` - Comprendre la structure des processus
- `README.md` - Documentation générale du moteur BPM
- `QUICKSTART.md` - Guide de démarrage rapide

## 🎉 Exemples de Cas d'Usage

### 1. Documentation Projet
Générer les diagrammes pour la documentation technique

### 2. Revue de Processus
Partager les HTML pour validation par les parties prenantes

### 3. Formation
Utiliser les diagrammes interactifs pour former les utilisateurs

### 4. Audit
Exporter en PDF via le navigateur pour archivage

### 5. Analyse
Identifier visuellement les goulots d'étranglement et chemins critiques
