# Quick Reference Guide

## Files Overview

| Category | File | Purpose |
|----------|------|---------|
| **Documentation** | README.md | Overview and quick start |
| | SETUP-GUIDE.md | Step-by-step setup instructions |
| | ARCHITECTURE.md | Detailed architecture and diagrams |
| | QUICK-REFERENCE.md | This file - quick reference |
| **Process Definitions** | ProcessDefinitions/order-processing.json | Main workflow (6 steps) |
| | ProcessDefinitions/manager-approval.json | Subprocess (2 steps) |
| **Database** | Database/oracle-schema.sql | Complete schema + sample data |
| **Models** | Client/Models/ProcessModels.cs | BPM entities (4 classes) |
| | Client/Models/BusinessModels.cs | Business entities (2 classes) |
| **Repositories** | Client/Repositories/ProcessDefinitionRepository.cs | Process definitions CRUD |
| | Client/Repositories/ProcessInstanceRepository.cs | Process instances CRUD |
| | Client/Repositories/StepInstanceRepository.cs | Step instances CRUD |
| | Client/Repositories/TaskRepository.cs | Task instances CRUD |
| **Services** | Client/Services/OrderCommandHandler.cs | 4 business commands |
| | Client/Services/OrderQueryHandler.cs | 3 business queries |
| | Client/Services/TaskService.cs | Task management |
| **Application** | Client/Program.cs | Main executable |
| | Client/BpmClient.csproj | Project file |

## Commands and Queries

### Commands (Write Operations)

| Command | Parameters | Returns | Purpose |
|---------|-----------|---------|---------|
| **ValidateOrder** | orderId, customerId | isValid, productId, quantity, totalAmount, customerName | Validate order and retrieve details |
| **FinalizeOrder** | orderId, approvalStatus, approvedBy | success, orderId, status | Mark order as finalized |
| **RejectOrder** | orderId, reason | success, orderId, status, reason | Mark order as rejected |
| **RecordApproval** | orderId, approvalStatus, approvedBy, comments | success, orderId, approvalStatus, approvedBy, comments | Record approval decision |

### Queries (Read Operations)

| Query | Parameters | Returns | Purpose |
|-------|-----------|---------|---------|
| **CheckInventory** | productId, quantity | isAvailable, stockLevel, productName, requestedQuantity, reason | Check product availability |
| **CheckApprovalStatus** | approvalStatus | approvalStatus, isApproved | Validate approval status |
| **GetOrderDetails** | orderId | found, orderId, customerId, customerName, productId, quantity, totalAmount, orderStatus, approvalStatus, approvedBy, approvalComments, rejectionReason | Retrieve complete order info |

## Step Types Reference

| Type | Enum Value | JSON type | Handler | Purpose |
|------|-----------|-----------|---------|---------|
| Business | 0 | 0 | BusinessStepHandler | Execute command |
| Interactive | 1 | 1 | InteractiveStepHandler | Create user task |
| Decision | 2 | 2 | DecisionStepHandler | Execute query + route |
| Scheduled | 3 | 3 | ScheduledStepHandler | Wait for time |
| Signal | 4 | 4 | SignalStepHandler | Wait for signal |
| SubProcess | 5 | 5 | SubProcessStepHandler | Execute subprocess |

## Process Status Values

| Status | Meaning | Next Actions |
|--------|---------|--------------|
| NotStarted | Process created but not executed | Call ExecuteProcessAsync() |
| Running | Process is executing | Engine is processing steps |
| Waiting | Process waiting (task, schedule, signal) | Complete task, wait for time, send signal |
| Completed | Process finished successfully | None - process done |
| Failed | Process encountered error | Review error, potentially restart |
| Cancelled | Process was cancelled | None - process terminated |

## Step Status Values

| Status | Meaning | Applicable Step Types |
|--------|---------|---------------------|
| NotStarted | Step created but not executed | All |
| Running | Step is executing | All |
| WaitingForTask | Waiting for user to complete task | Interactive |
| WaitingForSchedule | Waiting for scheduled time | Scheduled |
| WaitingForSignal | Waiting for external signal | Signal |
| Completed | Step finished successfully | All |
| Failed | Step encountered error | All |
| Skipped | Step was skipped by routing | Decision |

## Database Tables

| Table | Purpose | Key Columns |
|-------|---------|------------|
| TBL_PROC_DEFINITIONS | Store process definitions | DEF_ID, DEF_VERSION, DEF_JSON |
| TBL_PROC_INSTANCES | Track process execution | INST_ID, INST_STATUS, CURRENT_STEP, VARS_JSON |
| TBL_STEP_INSTANCES | Track step execution | STEP_INST_ID, PROC_INST_ID, STEP_STATUS |
| TBL_TASK_INSTANCES | Store user tasks | TASK_ID, TASK_STATUS, ASSIGNED_USER, ASSIGNED_ROLE |
| TBL_ORDERS | Business: Order data | ORDER_ID, ORDER_STATUS, APPROVAL_STATUS |
| TBL_INVENTORY | Business: Product inventory | PROD_ID, STOCK_LEVEL, IS_AVAILABLE |

## Column Mapping Examples

### Process Instance

| C# Property | Database Column |
|-------------|-----------------|
| Id | INST_ID |
| ProcessDefinitionId | PROC_DEF_ID |
| ProcessDefinitionVersion | PROC_DEF_VER |
| Status | INST_STATUS |
| CurrentStepId | CURRENT_STEP |
| Variables | VARS_JSON |
| StartedDate | STARTED_DT |

### Order

| C# Property | Database Column |
|-------------|-----------------|
| OrderId | ORDER_ID |
| CustomerId | CUST_ID |
| CustomerName | CUST_NAME |
| ProductId | PROD_ID |
| Quantity | QTY |
| TotalAmount | TOTAL_AMT |

## Common Code Snippets

### Initialize BPM Engine

```csharp
var engine = new ProcessEngine(
    processDefRepo,
    processInstRepo,
    stepInstRepo,
    commandHandler,
    queryHandler,
    taskService,
    conditionEvaluator
);
```

### Start Process

```csharp
var variables = new Dictionary<string, object>
{
    { "orderId", "ORD001" },
    { "customerId", "CUST001" }
};

var instanceId = await engine.StartProcessAsync(
    "ORDER_PROCESSING",
    1,  // version
    variables
);
```

### Load Process Definition from JSON

```csharp
var json = await File.ReadAllTextAsync("process.json");
var definition = JsonSerializer.Deserialize<ProcessDefinition>(
    json,
    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
);

await processDefRepo.SaveAsync(definition);
```

### Execute Dapper Query with Column Mapping

```csharp
using var connection = new OracleConnection(connectionString);

var sql = @"
    SELECT ORDER_ID, CUST_NAME, TOTAL_AMT
    FROM TBL_ORDERS
    WHERE ORDER_ID = :OrderId";

var order = await connection.QuerySingleAsync<Order>(sql, new { OrderId = "ORD001" });

// Dapper automatically maps:
// ORDER_ID → OrderId (via [Column("ORDER_ID")])
// CUST_NAME → CustomerName (via [Column("CUST_NAME")])
// TOTAL_AMT → TotalAmount (via [Column("TOTAL_AMT")])
```

### Complete User Task

```csharp
// User completes task
var result = new Dictionary<string, object>
{
    { "approvalStatus", "APPROVED" },
    { "approvedBy", "manager.smith" },
    { "comments", "Approved" }
};

await taskService.CompleteTaskAsync(taskId, result);

// Resume process
await engine.CompleteTaskAsync(stepInstanceId, result);
await engine.ExecuteProcessAsync(processInstanceId);
```

## Connection String Formats

### Basic Format
```
User Id=bpm_user;Password=your_password;Data Source=localhost:1521/XEPDB1
```

### With Connection Pooling
```
User Id=bpm_user;Password=your_password;Data Source=localhost:1521/XEPDB1;Min Pool Size=5;Max Pool Size=100
```

### TNS Names
```
User Id=bpm_user;Password=your_password;Data Source=TNSNAME
```

### EZConnect
```
User Id=bpm_user;Password=your_password;Data Source=//localhost:1521/XEPDB1
```

## Process Execution Flow

```
1. StartProcessAsync()
   └─> Creates ProcessInstance with status=NotStarted
   └─> Calls ExecuteProcessAsync()

2. ExecuteProcessAsync()
   └─> LOOP while currentStepId != null
       ├─> Get/Create StepInstance
       ├─> Get StepHandler by type
       ├─> Execute step
       │   ├─> Business: Execute command
       │   ├─> Decision: Execute query, evaluate conditions
       │   ├─> Interactive: Create task, WAIT
       │   ├─> SubProcess: Start child process
       │   └─> etc.
       ├─> If RequiresWait: Set status=Waiting, EXIT
       ├─> If Failed: Set status=Failed, EXIT
       └─> If Completed: Merge output, move to next step

3. When status=Completed
   └─> If has parent: Resume parent process
```

## Testing Scenarios

### Scenario 1: Successful Order with Approval
- Create order: ORD001, quantity: 10, product: PROD001 (stock: 100)
- Expected: Order validated → Inventory checked → Approval requested → Order finalized
- Result: ORDER_STATUS = 'FINALIZED', APPROVAL_STATUS = 'APPROVED'

### Scenario 2: Insufficient Inventory
- Create order: ORD002, quantity: 200, product: PROD001 (stock: 100)
- Expected: Order validated → Inventory checked → Order rejected
- Result: ORDER_STATUS = 'REJECTED', REJECTION_REASON = 'Insufficient inventory'

### Scenario 3: Manager Rejection
- Modify approval result to "REJECTED" in Program.cs
- Expected: Order validated → Inventory checked → Approval rejected → Order rejected
- Result: ORDER_STATUS = 'REJECTED', APPROVAL_STATUS = 'REJECTED'

## Troubleshooting

| Issue | Likely Cause | Solution |
|-------|--------------|----------|
| "Table or view does not exist" | Schema not created | Run oracle-schema.sql |
| "Invalid username/password" | Wrong credentials | Check connection string |
| "Column not found" | Missing [Column] attribute | Add attribute to model property |
| "JSON deserialization error" | Wrong case in JSON | Use camelCase in JSON files |
| Task not found | Task ID mismatch | Verify task was created and ID is correct |
| Process stuck in Waiting | Task not completed | Complete task and call CompleteTaskAsync |

## NuGet Packages

```xml
<PackageReference Include="Oracle.ManagedDataAccess.Core" Version="23.4.0" />
<PackageReference Include="Dapper" Version="2.1.28" />
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

## Key Configuration

### Enable Dapper Column Mapping
```csharp
SqlMapper.Settings.EnablePropertyMapping = true;
```

### JSON Serialization Options
```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

## Further Reading

- **SETUP-GUIDE.md** - Detailed setup instructions
- **ARCHITECTURE.md** - Complete architecture documentation
- **README.md** - Overview and feature showcase
- **BpmEngine Documentation** - Core engine documentation
