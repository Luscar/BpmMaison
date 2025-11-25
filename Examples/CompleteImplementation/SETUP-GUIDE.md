# Complete Implementation Setup Guide

This guide walks through setting up and running the complete BPM implementation example with Oracle + Dapper.

## Prerequisites

- .NET 8.0 SDK
- Oracle Database 19c or later
- Oracle SQL Developer or similar tool
- Visual Studio / VS Code / Rider

## Step 1: Database Setup

### 1.1 Create Oracle User (if needed)

```sql
-- Connect as SYSDBA
CREATE USER bpm_user IDENTIFIED BY your_password;
GRANT CONNECT, RESOURCE TO bpm_user;
GRANT CREATE TABLE, CREATE VIEW, CREATE SEQUENCE TO bpm_user;
ALTER USER bpm_user QUOTA UNLIMITED ON USERS;
```

### 1.2 Create Schema

```bash
# Connect to Oracle
sqlplus bpm_user/your_password@your_oracle_host:1521/your_service

# Run the schema script
@Database/oracle-schema.sql
```

Verify tables were created:
```sql
SELECT table_name FROM user_tables ORDER BY table_name;
```

Expected tables:
- TBL_INVENTORY
- TBL_ORDERS
- TBL_PROC_DEFINITIONS
- TBL_PROC_INSTANCES
- TBL_STEP_INSTANCES
- TBL_TASK_INSTANCES

## Step 2: Update Connection String

Edit `Client/Program.cs` and update the connection string:

```csharp
private const string ConnectionString =
    "User Id=bpm_user;Password=your_password;Data Source=localhost:1521/XEPDB1";
```

Connection string formats:
- **Basic**: `User Id=user;Password=pwd;Data Source=host:1521/service`
- **TNS**: `User Id=user;Password=pwd;Data Source=TNSNAME`
- **EZConnect**: `User Id=user;Password=pwd;Data Source=//host:1521/service`

## Step 3: Build the Solution

```bash
# Navigate to the BpmEngine directory
cd /path/to/BpmMaison/BpmEngine
dotnet build

# Navigate to the example client directory
cd ../Examples/CompleteImplementation/Client
dotnet build
```

## Step 4: Run the Example

```bash
dotnet run
```

Expected output:
```
=== BPM Order Processing Demo ===

Loading process definitions...
  ✓ Loaded: Order Processing v1
  ✓ Loaded: Manager Approval v1

Creating sample order...
  ✓ Created order ORD001 for customer John Doe

Initializing BPM Engine...

Starting Order Processing workflow...
  ✓ Started process instance: <guid>

Executing process steps...
[BusinessStepHandler] Executing command: ValidateOrder
[DecisionStepHandler] Executing query: CheckInventory
[SubProcessStepHandler] Starting subprocess MANAGER_APPROVAL

Main Process Status: Waiting
Current Step: manager_approval_subprocess

=== Approval Task Created ===
Task ID: <guid>
Type: ORDER_APPROVAL
Assigned to: Manager
Order ID: ORD001
Amount: 1500

Simulating manager approval...
[TaskService] Assigned task <guid> to user 'manager.smith'
[TaskService] Completed task <guid>

Resuming subprocess...
Executing subprocess: <guid>
[BusinessStepHandler] Executing command: RecordApproval
Subprocess Status: Completed

Resuming main process...
[DecisionStepHandler] Executing query: CheckApprovalStatus
[BusinessStepHandler] Executing command: FinalizeOrder

=== Final Results ===
Process Status: Completed
Approval Status: APPROVED
Approved By: manager.smith
Comments: Order approved - customer has good credit history

=== Order Status in Database ===
Order Status: FINALIZED
Approval Status: APPROVED
Approved By: manager.smith
Comments: Order approved - customer has good credit history

=== Demo completed successfully ===
```

## Architecture Overview

### Component Mapping

| Component | Implementation | Database Mapping |
|-----------|---------------|------------------|
| **Process Definitions** | JSON files → ProcessDefinition | TBL_PROC_DEFINITIONS |
| **Models** | C# classes with [Column] attributes | Oracle tables with different names |
| **Repositories** | Dapper + Oracle.ManagedDataAccess | SQL queries with column mapping |
| **Commands** | OrderCommandHandler | Business write operations |
| **Queries** | OrderQueryHandler | Business read operations |
| **Engine** | ProcessEngine | Orchestrates workflow |

### Column Name Mapping Examples

```csharp
// C# Property          → Oracle Column
public string Id       → DEF_ID
public string Name     → DEF_NAME
public int Version     → DEF_VERSION
public DateTime CreatedDate → DEF_CREATED_DT
```

### Process Flow

1. **Main Process**: ORDER_PROCESSING
   - Step 1: ValidateOrder (Command)
   - Step 2: CheckInventory (Query/Decision)
   - Step 3: SubProcess: MANAGER_APPROVAL
   - Step 4: CheckApprovalStatus (Query/Decision)
   - Step 5: FinalizeOrder or RejectOrder (Command)

2. **SubProcess**: MANAGER_APPROVAL
   - Step 1: Create approval task (Interactive)
   - Step 2: RecordApproval (Command)

## Customization Guide

### Adding New Commands

1. Add method to `OrderCommandHandler`:
```csharp
private async Task<Dictionary<string, object>> YourCommand(Dictionary<string, object> parameters)
{
    // Your business logic here
    using var connection = CreateConnection();
    // Execute SQL with Dapper
    return new Dictionary<string, object> { { "success", true } };
}
```

2. Register in switch statement:
```csharp
return commandName switch
{
    "YourCommand" => await YourCommand(parameters),
    // ... existing commands
};
```

3. Use in process definition:
```json
{
  "id": "your_step",
  "type": 0,
  "commandName": "YourCommand",
  "parameters": {
    "param1": "{{variable1}}"
  }
}
```

### Adding New Queries

Same pattern as commands but in `OrderQueryHandler`.

### Creating New Processes

1. Create JSON file in `ProcessDefinitions/`
2. Load using `ProcessDefinitionRepository.SaveAsync()`
3. Start using `ProcessEngine.StartProcessAsync()`

### Working with Different Column Names

Use Dapper's `[Column]` attribute:

```csharp
public class YourModel
{
    [Column("DB_COLUMN_NAME")]
    public string PropertyName { get; set; }
}
```

Dapper will automatically map between property names and column names.

## Testing Different Scenarios

### Scenario 1: Insufficient Inventory

Update inventory:
```sql
UPDATE TBL_INVENTORY SET STOCK_LEVEL = 5 WHERE PROD_ID = 'PROD001';
```

Create order with quantity > 5. Process should reject the order.

### Scenario 2: Manager Rejection

In `Program.cs`, change approval result:
```csharp
var approvalResult = new Dictionary<string, object>
{
    { "approvalStatus", "REJECTED" },
    { "approvedBy", "manager.smith" },
    { "comments", "Order rejected - budget exceeded" }
};
```

### Scenario 3: Multiple Orders

Modify `CreateSampleOrder()` to create multiple orders with different IDs and run process for each.

## Common Issues

### Issue: Connection Timeout
**Solution**: Check Oracle listener is running, firewall rules, and connection string format.

### Issue: Table Not Found
**Solution**: Verify schema was created in correct user/schema. Check user permissions.

### Issue: Column Mapping Error
**Solution**: Ensure `SqlMapper.Settings.EnablePropertyMapping = true;` is set before any Dapper calls.

### Issue: JSON Deserialization Error
**Solution**: Verify JSON files use camelCase for property names. Check JsonSerializerOptions configuration.

## Performance Considerations

### Indexes

The schema includes indexes on:
- Process/Step/Task status columns
- Foreign key columns
- Date columns for scheduled steps

### Connection Pooling

Oracle.ManagedDataAccess automatically pools connections. Configure in connection string:
```
User Id=user;Password=pwd;Data Source=host:1521/service;Min Pool Size=5;Max Pool Size=100;
```

### JSON Storage

Process definitions and variables are stored as CLOB. For large volumes:
- Consider extracting frequently-queried fields into separate columns
- Use Oracle JSON functions for querying within CLOB

## Next Steps

1. **Add Authentication**: Integrate with your identity system
2. **Add Logging**: Use Serilog or similar for structured logging
3. **Add Monitoring**: Track process execution metrics
4. **Add API Layer**: Expose BPM operations via REST API
5. **Add UI**: Build web interface for process management and task completion
6. **Add Error Handling**: Implement retry logic, compensation, etc.

## Resources

- [Dapper Documentation](https://github.com/DapperLib/Dapper)
- [Oracle .NET Documentation](https://docs.oracle.com/en/database/oracle/oracle-database/21/odpnt/)
- [BPM Engine Source](/BpmEngine/)
