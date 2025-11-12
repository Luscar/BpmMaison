using BpmEngine.Core.Models;
using BpmEngine.Serialization;
using BpmEngine.Visualization;

namespace BpmEngine.Tools;

public class VisualizerCli
{
    public static void Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║   BPM Engine - Générateur de Visualisation           ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        Console.WriteLine();

        if (args.Length < 1)
        {
            ShowUsage();
            return;
        }

        try
        {
            var inputFile = args[0];
            var outputFormat = args.Length > 1 ? args[1].ToLower() : "html";
            var outputFile = args.Length > 2 ? args[2] : GetDefaultOutputPath(inputFile, outputFormat);

            Console.WriteLine($"📂 Lecture du fichier: {inputFile}");
            var definition = ProcessDefinitionSerializer.DeserializeFromFile(inputFile);
            
            Console.WriteLine($"✓ Processus chargé: {definition.Name} (v{definition.Version})");
            Console.WriteLine($"  - ID: {definition.Id}");
            Console.WriteLine($"  - Étapes: {definition.Steps.Count}");
            Console.WriteLine();

            var visualizer = new ProcessVisualizer();
            
            Console.WriteLine($"🎨 Génération du diagramme au format {outputFormat.ToUpper()}...");
            visualizer.ExportToFile(definition, outputFile, outputFormat);
            
            Console.WriteLine($"✓ Fichier généré: {outputFile}");
            Console.WriteLine();
            Console.WriteLine("✨ Génération terminée avec succès!");
            
            if (outputFormat == "html")
            {
                Console.WriteLine($"\n💡 Ouvrez le fichier {outputFile} dans votre navigateur pour visualiser le processus.");
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erreur: Fichier introuvable - {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erreur: {ex.Message}");
            Console.WriteLine($"\nDétails: {ex.StackTrace}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  BpmEngine.Visualizer <fichier.json> [format] [sortie]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  fichier.json    Fichier JSON de définition de processus (requis)");
        Console.WriteLine("  format          Format de sortie: 'html' ou 'mermaid' (défaut: html)");
        Console.WriteLine("  sortie          Nom du fichier de sortie (optionnel)");
        Console.WriteLine();
        Console.WriteLine("Exemples:");
        Console.WriteLine("  BpmEngine.Visualizer process.json");
        Console.WriteLine("  BpmEngine.Visualizer process.json html output.html");
        Console.WriteLine("  BpmEngine.Visualizer process.json mermaid diagram.mmd");
        Console.WriteLine();
        Console.WriteLine("Formats supportés:");
        Console.WriteLine("  - html     : Page HTML standalone avec diagramme interactif");
        Console.WriteLine("  - mermaid  : Code Mermaid.js pour intégration");
    }

    private static string GetDefaultOutputPath(string inputFile, string format)
    {
        var directory = Path.GetDirectoryName(inputFile) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(inputFile);
        var extension = format == "mermaid" ? ".mmd" : ".html";
        return Path.Combine(directory, fileName + "-diagram" + extension);
    }
}

// Exemple de script batch pour Windows
public class BatchScriptGenerator
{
    public static void GenerateWindowsBatchScript(string outputPath)
    {
        var script = @"@echo off
REM Script de génération de visualisation BPM
REM Usage: generate-diagram.bat <fichier.json>

if ""%1""=="""" (
    echo Usage: generate-diagram.bat fichier.json
    exit /b 1
)

dotnet run --project BpmEngine.Visualizer.csproj -- %1 html

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Visualisation generee avec succes!
    start """" ""%~n1-diagram.html""
) else (
    echo.
    echo Erreur lors de la generation
    exit /b 1
)
";
        File.WriteAllText(outputPath, script);
    }

    public static void GenerateLinuxShellScript(string outputPath)
    {
        var script = @"#!/bin/bash
# Script de génération de visualisation BPM
# Usage: ./generate-diagram.sh <fichier.json>

if [ -z ""$1"" ]; then
    echo ""Usage: ./generate-diagram.sh fichier.json""
    exit 1
fi

dotnet run --project BpmEngine.Visualizer.csproj -- ""$1"" html

if [ $? -eq 0 ]; then
    echo
    echo ""Visualisation générée avec succès!""
    filename=$(basename ""$1"" .json)
    xdg-open ""${filename}-diagram.html"" 2>/dev/null || open ""${filename}-diagram.html"" 2>/dev/null
else
    echo
    echo ""Erreur lors de la génération""
    exit 1
fi
";
        File.WriteAllText(outputPath, script);
    }
}
