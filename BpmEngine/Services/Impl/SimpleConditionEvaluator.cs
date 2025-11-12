using BpmEngine.Services;
using System.Text.RegularExpressions;

namespace BpmEngine.Services.Impl;

public class SimpleConditionEvaluator : IConditionEvaluator
{
    public bool Evaluate(string condition, Dictionary<string, object> context)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return true;

        condition = condition.Trim();

        if (condition.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;

        if (condition.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        var equalPattern = @"^\s*(\w+)\s*==\s*(.+)\s*$";
        var notEqualPattern = @"^\s*(\w+)\s*!=\s*(.+)\s*$";
        var greaterPattern = @"^\s*(\w+)\s*>\s*(.+)\s*$";
        var greaterOrEqualPattern = @"^\s*(\w+)\s*>=\s*(.+)\s*$";
        var lessPattern = @"^\s*(\w+)\s*<\s*(.+)\s*$";
        var lessOrEqualPattern = @"^\s*(\w+)\s*<=\s*(.+)\s*$";

        if (TryEvaluateComparison(condition, equalPattern, context, (a, b) => CompareValues(a, b) == 0, out var result))
            return result;

        if (TryEvaluateComparison(condition, notEqualPattern, context, (a, b) => CompareValues(a, b) != 0, out result))
            return result;

        if (TryEvaluateComparison(condition, greaterPattern, context, (a, b) => CompareValues(a, b) > 0, out result))
            return result;

        if (TryEvaluateComparison(condition, greaterOrEqualPattern, context, (a, b) => CompareValues(a, b) >= 0, out result))
            return result;

        if (TryEvaluateComparison(condition, lessPattern, context, (a, b) => CompareValues(a, b) < 0, out result))
            return result;

        if (TryEvaluateComparison(condition, lessOrEqualPattern, context, (a, b) => CompareValues(a, b) <= 0, out result))
            return result;

        return false;
    }

    private bool TryEvaluateComparison(
        string condition,
        string pattern,
        Dictionary<string, object> context,
        Func<object, object, bool> comparer,
        out bool result)
    {
        var match = Regex.Match(condition, pattern);
        
        if (!match.Success)
        {
            result = false;
            return false;
        }

        var variableName = match.Groups[1].Value;
        var expectedValueStr = match.Groups[2].Value.Trim().Trim('"', '\'');

        if (!context.TryGetValue(variableName, out var actualValue))
        {
            result = false;
            return true;
        }

        var expectedValue = ConvertValue(expectedValueStr, actualValue?.GetType());
        result = comparer(actualValue!, expectedValue!);
        return true;
    }

    private int CompareValues(object a, object b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        if (a is IComparable comparableA && b.GetType() == a.GetType())
        {
            return comparableA.CompareTo(b);
        }

        return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
    }

    private object? ConvertValue(string value, Type? targetType)
    {
        if (targetType == null)
            return value;

        try
        {
            if (targetType == typeof(int))
                return int.Parse(value);
            if (targetType == typeof(long))
                return long.Parse(value);
            if (targetType == typeof(double))
                return double.Parse(value);
            if (targetType == typeof(decimal))
                return decimal.Parse(value);
            if (targetType == typeof(bool))
                return bool.Parse(value);
            if (targetType == typeof(DateTime))
                return DateTime.Parse(value);

            return value;
        }
        catch
        {
            return value;
        }
    }
}
