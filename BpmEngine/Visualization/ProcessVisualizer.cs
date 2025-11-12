using System.Text;
using System.Text.Json;
using BpmEngine.Core;
using BpmEngine.Core.Models;
using BpmEngine.Serialization;

namespace BpmEngine.Visualization;

public class ProcessVisualizer
{
    public string GenerateMermaidDiagram(ProcessDefinition definition)
    {
        var sb = new StringBuilder();
        sb.AppendLine("graph TD");
        sb.AppendLine($"    Start([{definition.Name}])");
        sb.AppendLine($"    Start --> {definition.StartStepId}");
        sb.AppendLine();

        var processedSteps = new HashSet<string>();
        var decisionSteps = definition.Steps.Where(s => s.Type == StepType.Decision).ToList();

        foreach (var step in definition.Steps)
        {
            if (processedSteps.Contains(step.Id))
                continue;

            processedSteps.Add(step.Id);
            AddStepNode(sb, step);

            switch (step.Type)
            {
                case StepType.Decision:
                    var decisionStep = (DecisionStepDefinition)step;
                    foreach (var route in decisionStep.Routes.OrderBy(r => r.Priority))
                    {
                        var condition = route.Condition.Length > 30 
                            ? route.Condition.Substring(0, 27) + "..." 
                            : route.Condition;
                        sb.AppendLine($"    {step.Id} -->|{condition}| {route.TargetStepId}");
                    }
                    break;

                default:
                    if (!string.IsNullOrEmpty(step.NextStepId))
                    {
                        sb.AppendLine($"    {step.Id} --> {step.NextStepId}");
                    }
                    else
                    {
                        sb.AppendLine($"    {step.Id} --> End([Fin])");
                    }
                    break;
            }

            sb.AppendLine();
        }

        AddStepStyles(sb);

        return sb.ToString();
    }

    private void AddStepNode(StringBuilder sb, StepDefinition step)
    {
        var icon = GetStepIcon(step.Type);
        var shape = GetStepShape(step.Type);
        var label = $"{icon} {step.Name}";

        var nodeDefinition = shape switch
        {
            "rectangle" => $"    {step.Id}[\"{label}\"]",
            "rhombus" => $"    {step.Id}{{\"{label}\"}}",
            "rounded" => $"    {step.Id}(\"{label}\")",
            "stadium" => $"    {step.Id}([{label}])",
            "subroutine" => $"    {step.Id}[[{label}]]",
            "cylinder" => $"    {step.Id}[({label})]",
            _ => $"    {step.Id}[\"{label}\"]"
        };

        sb.AppendLine(nodeDefinition);
        sb.AppendLine($"    class {step.Id} {GetStepClass(step.Type)}");
    }

    private string GetStepIcon(StepType type)
    {
        return type switch
        {
            StepType.Business => "⚙️",
            StepType.Interactive => "👤",
            StepType.Decision => "❓",
            StepType.Scheduled => "⏰",
            StepType.Signal => "📡",
            StepType.SubProcess => "📦",
            _ => "•"
        };
    }

    private string GetStepShape(StepType type)
    {
        return type switch
        {
            StepType.Business => "rectangle",
            StepType.Interactive => "rounded",
            StepType.Decision => "rhombus",
            StepType.Scheduled => "stadium",
            StepType.Signal => "stadium",
            StepType.SubProcess => "subroutine",
            _ => "rectangle"
        };
    }

    private string GetStepClass(StepType type)
    {
        return type switch
        {
            StepType.Business => "businessStep",
            StepType.Interactive => "interactiveStep",
            StepType.Decision => "decisionStep",
            StepType.Scheduled => "scheduledStep",
            StepType.Signal => "signalStep",
            StepType.SubProcess => "subProcessStep",
            _ => "defaultStep"
        };
    }

    private void AddStepStyles(StringBuilder sb)
    {
        sb.AppendLine("    classDef businessStep fill:#e1f5ff,stroke:#01579b,stroke-width:2px");
        sb.AppendLine("    classDef interactiveStep fill:#f3e5f5,stroke:#4a148c,stroke-width:2px");
        sb.AppendLine("    classDef decisionStep fill:#fff9c4,stroke:#f57f17,stroke-width:3px");
        sb.AppendLine("    classDef scheduledStep fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px");
        sb.AppendLine("    classDef signalStep fill:#fce4ec,stroke:#880e4f,stroke-width:2px");
        sb.AppendLine("    classDef subProcessStep fill:#e0f2f1,stroke:#004d40,stroke-width:2px");
    }

    public string GenerateHtmlVisualization(ProcessDefinition definition)
    {
        var mermaidCode = GenerateMermaidDiagram(definition);
        
        var html = $@"<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{definition.Name} - Visualisation</title>
    <script src=""https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js""></script>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
        }}
        .container {{
            max-width: 1400px;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            padding: 30px;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
            padding-bottom: 20px;
            border-bottom: 3px solid #667eea;
        }}
        h1 {{
            color: #333;
            margin: 0;
            font-size: 2.5em;
        }}
        .info {{
            color: #666;
            font-size: 1.1em;
            margin-top: 10px;
        }}
        .diagram {{
            background: #fafafa;
            padding: 30px;
            border-radius: 8px;
            border: 2px solid #e0e0e0;
            margin: 20px 0;
            overflow-x: auto;
        }}
        .legend {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            margin-top: 30px;
            padding: 20px;
            background: #f5f5f5;
            border-radius: 8px;
        }}
        .legend-item {{
            display: flex;
            align-items: center;
            padding: 10px;
            background: white;
            border-radius: 6px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .legend-icon {{
            font-size: 1.5em;
            margin-right: 10px;
        }}
        .legend-text {{
            font-weight: 600;
            color: #333;
        }}
        .step-details {{
            margin-top: 30px;
            padding: 20px;
            background: #fff9c4;
            border-left: 4px solid #f57f17;
            border-radius: 4px;
        }}
        .step-details h3 {{
            margin-top: 0;
            color: #f57f17;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔄 {definition.Name}</h1>
            <div class=""info"">
                {definition.Description}<br>
                <strong>Version:</strong> {definition.Version} | 
                <strong>ID:</strong> {definition.Id} | 
                <strong>Étapes:</strong> {definition.Steps.Count}
            </div>
        </div>

        <div class=""diagram"">
            <div class=""mermaid"">
{mermaidCode}
            </div>
        </div>

        <div class=""legend"">
            <div class=""legend-item"">
                <span class=""legend-icon"">⚙️</span>
                <span class=""legend-text"">Étape Affaire</span>
            </div>
            <div class=""legend-item"">
                <span class=""legend-icon"">👤</span>
                <span class=""legend-text"">Étape Interactive</span>
            </div>
            <div class=""legend-item"">
                <span class=""legend-icon"">❓</span>
                <span class=""legend-text"">Étape Décision</span>
            </div>
            <div class=""legend-item"">
                <span class=""legend-icon"">⏰</span>
                <span class=""legend-text"">Étape Cédulée</span>
            </div>
            <div class=""legend-item"">
                <span class=""legend-icon"">📡</span>
                <span class=""legend-text"">Étape Signal</span>
            </div>
            <div class=""legend-item"">
                <span class=""legend-icon"">📦</span>
                <span class=""legend-text"">Sous-Processus</span>
            </div>
        </div>

        <div class=""step-details"">
            <h3>💡 Détails des Étapes</h3>
            <ul>
                {string.Join("\n                ", definition.Steps.Select(s => $"<li><strong>{s.Name}</strong> ({s.Type})</li>"))}
            </ul>
        </div>
    </div>

    <script>
        mermaid.initialize({{ 
            startOnLoad: true,
            theme: 'default',
            flowchart: {{
                useMaxWidth: true,
                htmlLabels: true,
                curve: 'basis'
            }}
        }});
    </script>
</body>
</html>";

        return html;
    }

    public void ExportToFile(ProcessDefinition definition, string outputPath, string format = "html")
    {
        string content = format.ToLower() switch
        {
            "mermaid" => GenerateMermaidDiagram(definition),
            "html" => GenerateHtmlVisualization(definition),
            _ => throw new ArgumentException($"Format non supporté: {format}")
        };

        File.WriteAllText(outputPath, content);
    }
}
