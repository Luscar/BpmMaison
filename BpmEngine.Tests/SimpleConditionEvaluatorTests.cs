namespace BpmEngine.Tests;

public class SimpleConditionEvaluatorTests
{
    private readonly SimpleConditionEvaluator _evaluator;

    public SimpleConditionEvaluatorTests()
    {
        _evaluator = new SimpleConditionEvaluator();
    }

    [Fact]
    public void Evaluate_EmptyCondition_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate("", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_NullCondition_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(null!, context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_TrueKeyword_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate("true", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_FalseKeyword_ReturnsFalse()
    {
        // Arrange
        var context = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate("false", context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_EqualityComparison_WithMatchingStrings_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["status"] = "approved"
        };

        // Act
        var result = _evaluator.Evaluate("status == approved", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_EqualityComparison_WithNonMatchingStrings_ReturnsFalse()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["status"] = "pending"
        };

        // Act
        var result = _evaluator.Evaluate("status == approved", context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_NotEqualComparison_WithDifferentValues_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["status"] = "pending"
        };

        // Act
        var result = _evaluator.Evaluate("status != approved", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_GreaterThanComparison_WithNumbers_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["amount"] = 150
        };

        // Act
        var result = _evaluator.Evaluate("amount > 100", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_GreaterThanComparison_WithNumbers_ReturnsFalse()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["amount"] = 50
        };

        // Act
        var result = _evaluator.Evaluate("amount > 100", context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_GreaterThanOrEqualComparison_WithEqualNumbers_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["amount"] = 100
        };

        // Act
        var result = _evaluator.Evaluate("amount >= 100", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_LessThanComparison_WithNumbers_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["amount"] = 50
        };

        // Act
        var result = _evaluator.Evaluate("amount < 100", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_LessThanOrEqualComparison_WithEqualNumbers_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["amount"] = 100
        };

        // Act
        var result = _evaluator.Evaluate("amount <= 100", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_VariableNotInContext_ReturnsFalse()
    {
        // Arrange
        var context = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate("missingVar == value", context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_WithQuotedValue_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["status"] = "approved"
        };

        // Act
        var result = _evaluator.Evaluate("status == \"approved\"", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_WithSingleQuotedValue_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["status"] = "approved"
        };

        // Act
        var result = _evaluator.Evaluate("status == 'approved'", context);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(100, "amount == 100", true)]
    [InlineData(100, "amount != 100", false)]
    [InlineData(150, "amount > 100", true)]
    [InlineData(100, "amount >= 100", true)]
    [InlineData(50, "amount < 100", true)]
    [InlineData(100, "amount <= 100", true)]
    public void Evaluate_IntegerComparisons_ReturnsExpectedResult(int amount, string condition, bool expected)
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["amount"] = amount
        };

        // Act
        var result = _evaluator.Evaluate(condition, context);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_WithDoubleValues_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["price"] = 99.99
        };

        // Act
        var result = _evaluator.Evaluate("price > 50.00", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_WithBooleanValues_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["isActive"] = true
        };

        // Act
        var result = _evaluator.Evaluate("isActive == true", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_InvalidConditionFormat_ReturnsFalse()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["status"] = "approved"
        };

        // Act
        var result = _evaluator.Evaluate("invalid condition format", context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_ConditionWithSpaces_ReturnsTrue()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["status"] = "approved"
        };

        // Act
        var result = _evaluator.Evaluate("  status   ==   approved  ", context);

        // Assert
        Assert.True(result);
    }
}
