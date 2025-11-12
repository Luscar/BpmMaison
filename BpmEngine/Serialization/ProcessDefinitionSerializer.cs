using System.Text.Json;
using System.Text.Json.Serialization;
using BpmEngine.Core;
using BpmEngine.Core.Models;

namespace BpmEngine.Serialization;

public class StepDefinitionConverter : JsonConverter<StepDefinition>
{
    public override StepDefinition Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeElement))
            throw new JsonException("Property 'type' manquante");

        var stepType = (StepType)typeElement.GetInt32();

        var json = root.GetRawText();

        return stepType switch
        {
            StepType.Business => JsonSerializer.Deserialize<BusinessStepDefinition>(json, options)!,
            StepType.Interactive => JsonSerializer.Deserialize<InteractiveStepDefinition>(json, options)!,
            StepType.Decision => JsonSerializer.Deserialize<DecisionStepDefinition>(json, options)!,
            StepType.Scheduled => JsonSerializer.Deserialize<ScheduledStepDefinition>(json, options)!,
            StepType.Signal => JsonSerializer.Deserialize<SignalStepDefinition>(json, options)!,
            StepType.SubProcess => JsonSerializer.Deserialize<SubProcessStepDefinition>(json, options)!,
            _ => throw new JsonException($"Type d'étape non supporté: {stepType}")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        StepDefinition value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

public static class ProcessDefinitionSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new StepDefinitionConverter(), new JsonStringEnumConverter() }
    };

    public static string Serialize(ProcessDefinition definition)
    {
        return JsonSerializer.Serialize(definition, DefaultOptions);
    }

    public static ProcessDefinition Deserialize(string json)
    {
        var result = JsonSerializer.Deserialize<ProcessDefinition>(json, DefaultOptions);
        return result ?? throw new JsonException("Échec de la désérialisation");
    }

    public static ProcessDefinition DeserializeFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return Deserialize(json);
    }

    public static void SerializeToFile(ProcessDefinition definition, string filePath)
    {
        var json = Serialize(definition);
        File.WriteAllText(filePath, json);
    }
}
