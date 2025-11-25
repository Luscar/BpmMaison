# BpmEngine.Tests

This project contains xUnit tests for the BpmEngine NuGet package.

## Test Coverage

The test suite includes comprehensive tests for:

### Core Models
- **ProcessDefinitionTests**: Tests for ProcessDefinition and all step definition types
  - BusinessStepDefinition
  - InteractiveStepDefinition
  - DecisionStepDefinition
  - ScheduledStepDefinition
  - SignalStepDefinition
  - SubProcessStepDefinition

- **ProcessInstanceTests**: Tests for runtime instances
  - ProcessInstance
  - StepInstance
  - TaskInstance

### Services
- **SimpleConditionEvaluatorTests**: Comprehensive tests for condition evaluation
  - Equality comparisons (==, !=)
  - Numeric comparisons (>, >=, <, <=)
  - String comparisons
  - Boolean comparisons
  - Edge cases (null, empty, missing variables)

### Enums
- **EnumsTests**: Tests for all enum types
  - StepType
  - ProcessStatus
  - StepStatus

## Running the Tests

### Using dotnet CLI
```bash
dotnet test
```

### Using Visual Studio
Open the solution file and run tests through Test Explorer.

### Using Rider
Open the solution file and run tests through the Unit Tests window.

## Test Structure

Each test follows the Arrange-Act-Assert pattern:
- **Arrange**: Set up test data and preconditions
- **Act**: Execute the method being tested
- **Assert**: Verify the expected outcome

## Dependencies

- xUnit 2.6.3
- Microsoft.NET.Test.Sdk 17.8.0
- xunit.runner.visualstudio 2.5.5
- coverlet.collector 6.0.0 (for code coverage)
