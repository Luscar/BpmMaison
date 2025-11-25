# Architecture Documentation

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client Application                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Program    │  │Process Defs  │  │   Services   │          │
│  │  (Main.cs)   │  │   (JSON)     │  │  (Commands/  │          │
│  │              │  │              │  │   Queries)   │          │
│  └──────┬───────┘  └──────────────┘  └──────┬───────┘          │
│         │                                     │                  │
│         └────────────────┬────────────────────┘                  │
│                          │                                       │
│                ┌─────────▼──────────┐                            │
│                │   ProcessEngine    │                            │
│                │  (BPM Execution)   │                            │
│                └─────────┬──────────┘                            │
│                          │                                       │
│         ┌────────────────┼────────────────┐                      │
│         │                │                │                      │
│    ┌────▼─────┐    ┌────▼─────┐    ┌────▼─────┐                │
│    │Command   │    │  Query   │    │   Task   │                │
│    │ Handler  │    │ Handler  │    │ Service  │                │
│    └────┬─────┘    └────┬─────┘    └────┬─────┘                │
│         │               │               │                        │
│         └───────────────┼───────────────┘                        │
│                         │                                        │
│                ┌────────▼─────────┐                              │
│                │   Repositories   │                              │
│                │ (Dapper+Oracle)  │                              │
│                └────────┬─────────┘                              │
│                         │                                        │
└─────────────────────────┼─────────────────────────────────────────┘
                          │
                          │ Oracle.ManagedDataAccess
                          │
                ┌─────────▼─────────┐
                │  Oracle Database  │
                │                   │
                │ ┌───────────────┐ │
                │ │TBL_PROC_      │ │
                │ │DEFINITIONS    │ │
                │ ├───────────────┤ │
                │ │TBL_PROC_      │ │
                │ │INSTANCES      │ │
                │ ├───────────────┤ │
                │ │TBL_STEP_      │ │
                │ │INSTANCES      │ │
                │ ├───────────────┤ │
                │ │TBL_TASK_      │ │
                │ │INSTANCES      │ │
                │ ├───────────────┤ │
                │ │TBL_ORDERS     │ │
                │ ├───────────────┤ │
                │ │TBL_INVENTORY  │ │
                │ └───────────────┘ │
                └───────────────────┘
```

## Process Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    ORDER_PROCESSING Process                      │
└─────────────────────────────────────────────────────────────────┘

    ┌──────────────┐
    │   START      │
    │ (Variables:  │
    │  orderId,    │
    │  customerId) │
    └──────┬───────┘
           │
           ▼
    ┌──────────────┐
    │ Validate     │  ◄── BusinessStep (Command)
    │ Order        │      ValidateOrder
    └──────┬───────┘      Returns: isValid, productId,
           │              quantity, totalAmount, customerName
           ▼
    ┌──────────────┐
    │ Check        │  ◄── DecisionStep (Query)
    │ Inventory    │      CheckInventory
    └──────┬───────┘      Returns: isAvailable, stockLevel
           │
           ├─── isAvailable==false OR quantity>stockLevel ───┐
           │                                                   │
           ▼ isAvailable==true && quantity<=stockLevel        │
    ┌──────────────┐                                          │
    │  Subprocess  │  ◄── SubProcessStep                      │
    │   Manager    │      MANAGER_APPROVAL                    │
    │   Approval   │                                          │
    └──────┬───────┘                                          │
           │                                                   │
   ┌───────┴────────────────────────────────────────┐         │
   │    MANAGER_APPROVAL Subprocess                 │         │
   │                                                 │         │
   │  ┌──────────────┐                              │         │
   │  │  Create      │  ◄── InteractiveStep         │         │
   │  │  Approval    │      Creates task for        │         │
   │  │  Task        │      Manager role            │         │
   │  └──────┬───────┘                              │         │
   │         │                                       │         │
   │  [User completes task]                         │         │
   │         │                                       │         │
   │         ▼                                       │         │
   │  ┌──────────────┐                              │         │
   │  │  Record      │  ◄── BusinessStep (Command)  │         │
   │  │  Approval    │      RecordApproval          │         │
   │  └──────┬───────┘      Returns: approvalStatus,│         │
   │         │              approvedBy, comments     │         │
   │         ▼                                       │         │
   │  ┌──────────────┐                              │         │
   │  │   END        │                              │         │
   │  │ Subprocess   │                              │         │
   │  └──────────────┘                              │         │
   └─────────────────────────────────────────────────┘         │
           │                                                   │
           │ (Variables returned to parent:                   │
           │  approvalStatus, approvedBy, comments)           │
           │                                                   │
           ▼                                                   │
    ┌──────────────┐                                          │
    │ Check        │  ◄── DecisionStep (Query)                │
    │ Approval     │      CheckApprovalStatus                 │
    │ Result       │                                          │
    └──────┬───────┘                                          │
           │                                                   │
           ├─── approvalStatus=='APPROVED' ──────┐            │
           │                                      │            │
           │                                      ▼            │
           │                              ┌──────────────┐    │
           │                              │  Finalize    │    │
           │                              │  Order       │ ◄──┤── BusinessStep
           │                              └──────┬───────┘    │   (Command)
           │                                     │            │   FinalizeOrder
           │                                     ▼            │
           │                              ┌──────────────┐    │
           │                              │    END       │    │
           │                              │  (Success)   │    │
           │                              └──────────────┘    │
           │                                                   │
           └─── approvalStatus=='REJECTED' ──────────────────┐│
                                                              ││
    ┌─────────────────────────────────────────────────────────┘│
    │                                                          │
    ▼                                                          │
┌──────────────┐                                              │
│  Reject      │  ◄── BusinessStep (Command)                  │
│  Order       │      RejectOrder                             │
└──────┬───────┘                                              │
       │                                                       │
       ▼                                                       │
┌──────────────┐                                              │
│    END       │  ◄───────────────────────────────────────────┘
│  (Rejected)  │
└──────────────┘
```

## Data Model Mapping

### ProcessDefinition Mapping

```
C# Model (ProcessDefinition)          Oracle Table (TBL_PROC_DEFINITIONS)
┌───────────────────────┐              ┌────────────────────────┐
│ Id                    │ ───────────► │ DEF_ID                 │
│ Name                  │ ───────────► │ DEF_NAME               │
│ Description           │ ───────────► │ DEF_DESCRIPTION        │
│ Version               │ ───────────► │ DEF_VERSION            │
│ [Full Object as JSON] │ ───────────► │ DEF_JSON (CLOB)        │
│                       │              │ DEF_CREATED_DT         │
│                       │              │ DEF_UPDATED_DT         │
└───────────────────────┘              └────────────────────────┘
```

### ProcessInstance Mapping

```
C# Model (ProcessInstance)             Oracle Table (TBL_PROC_INSTANCES)
┌───────────────────────┐              ┌────────────────────────┐
│ Id                    │ ───────────► │ INST_ID                │
│ ProcessDefinitionId   │ ───────────► │ PROC_DEF_ID            │
│ ProcessDefinitionVer  │ ───────────► │ PROC_DEF_VER           │
│ Status                │ ───────────► │ INST_STATUS            │
│ CurrentStepId         │ ───────────► │ CURRENT_STEP           │
│ ParentProcessInstId   │ ───────────► │ PARENT_INST_ID         │
│ Variables (Dict)      │ ───────────► │ VARS_JSON (CLOB)       │
│ StartedDate           │ ───────────► │ STARTED_DT             │
│ CompletedDate         │ ───────────► │ COMPLETED_DT           │
│ FailedDate            │ ───────────► │ FAILED_DT              │
│ ErrorMessage          │ ───────────► │ ERROR_MSG              │
└───────────────────────┘              └────────────────────────┘
```

### Order Model Mapping

```
C# Model (Order)                       Oracle Table (TBL_ORDERS)
┌───────────────────────┐              ┌────────────────────────┐
│ OrderId               │ ───────────► │ ORDER_ID               │
│ CustomerId            │ ───────────► │ CUST_ID                │
│ CustomerName          │ ───────────► │ CUST_NAME              │
│ ProductId             │ ───────────► │ PROD_ID                │
│ Quantity              │ ───────────► │ QTY                    │
│ TotalAmount           │ ───────────► │ TOTAL_AMT              │
│ OrderStatus           │ ───────────► │ ORDER_STATUS           │
│ ApprovalStatus        │ ───────────► │ APPROVAL_STATUS        │
│ ApprovedBy            │ ───────────► │ APPROVED_BY            │
│ ApprovalComments      │ ───────────► │ APPROVAL_COMMENTS      │
│ RejectionReason       │ ───────────► │ REJECTION_REASON       │
│ CreatedDate           │ ───────────► │ CREATED_DT             │
│ UpdatedDate           │ ───────────► │ UPDATED_DT             │
└───────────────────────┘              └────────────────────────┘
```

## Component Responsibilities

### 1. ProcessDefinitionRepository
- **Purpose**: Persist and retrieve process definitions
- **Technology**: Dapper + Oracle
- **Key Operations**:
  - `GetByIdAsync(id, version)` - Load process definition
  - `SaveAsync(definition)` - Store process definition as JSON
  - Uses MERGE statement for upsert

### 2. ProcessInstanceRepository
- **Purpose**: Manage process execution state
- **Technology**: Dapper + Oracle
- **Key Operations**:
  - `GetByIdAsync(id)` - Load process instance
  - `SaveAsync(instance)` - Persist process state
  - `GetByStatusAsync(status)` - Find processes by status
  - `GetByParentIdAsync(parentId)` - Find subprocesses

### 3. StepInstanceRepository
- **Purpose**: Track individual step execution
- **Technology**: Dapper + Oracle
- **Key Operations**:
  - `GetByProcessAndStepIdAsync()` - Find specific step
  - `SaveAsync(stepInstance)` - Save step state
  - `GetScheduledStepsAsync()` - Find steps ready to resume

### 4. TaskRepository
- **Purpose**: Manage user tasks
- **Technology**: Dapper + Oracle
- **Key Operations**:
  - `GetByUserAsync(userId, status)` - User's task list
  - `GetByRoleAsync(role, status)` - Role-based tasks
  - `SaveAsync(task)` - Persist task state

### 5. OrderCommandHandler (ICommandHandler)
- **Purpose**: Execute business write operations
- **Commands**:
  - `ValidateOrder` - Check order validity, update status
  - `FinalizeOrder` - Complete order processing
  - `RejectOrder` - Cancel order with reason
  - `RecordApproval` - Store approval decision
- **Technology**: Dapper for SQL execution

### 6. OrderQueryHandler (IQueryHandler)
- **Purpose**: Execute business read operations
- **Queries**:
  - `CheckInventory` - Verify product availability
  - `CheckApprovalStatus` - Decision routing logic
  - `GetOrderDetails` - Retrieve order information
- **Technology**: Dapper for SQL queries

### 7. TaskService (ITaskService)
- **Purpose**: Create and manage user tasks
- **Operations**:
  - `CreateTaskAsync()` - Generate new task
  - `CompleteTaskAsync()` - Mark task complete
  - `AssignTaskAsync()` - Assign to user

### 8. ProcessEngine
- **Purpose**: Orchestrate process execution
- **Key Methods**:
  - `StartProcessAsync()` - Initialize new process
  - `ExecuteProcessAsync()` - Execute process steps
  - `CompleteTaskAsync()` - Resume after task completion
- **Responsibilities**:
  - Load definitions and instances
  - Route to appropriate step handlers
  - Manage state transitions
  - Handle subprocesses

## Execution Flow

### Starting a Process

```
1. Client calls ProcessEngine.StartProcessAsync()
   ├─► Load ProcessDefinition from repository
   ├─► Create new ProcessInstance with GUID
   ├─► Set initial variables
   ├─► Set status = NotStarted
   ├─► Save ProcessInstance
   └─► Call ExecuteProcessAsync()
```

### Executing a Process

```
2. ProcessEngine.ExecuteProcessAsync(instanceId)
   ├─► Load ProcessInstance
   ├─► Load ProcessDefinition
   │
   └─► LOOP while currentStepId != null
       │
       ├─► Find StepDefinition by currentStepId
       ├─► Get or Create StepInstance
       ├─► Get appropriate handler by StepType
       │
       ├─► Execute Step
       │   │
       │   ├─► BusinessStep → CommandHandler.ExecuteAsync()
       │   ├─► DecisionStep → QueryHandler.ExecuteAsync()
       │   ├─► InteractiveStep → TaskService.CreateTaskAsync()
       │   ├─► ScheduledStep → Set ResumeAt, WAIT
       │   ├─► SignalStep → Set WaitingForSignal, WAIT
       │   └─► SubProcessStep → StartProcessAsync(subprocessId)
       │
       ├─► Get StepExecutionResult
       │
       ├─► IF RequiresWait
       │   ├─► Set ProcessInstance.Status = Waiting
       │   ├─► Save state
       │   └─► EXIT loop
       │
       ├─► IF Failed
       │   ├─► Set ProcessInstance.Status = Failed
       │   ├─► Save error
       │   └─► EXIT loop
       │
       ├─► IF Completed
       │   ├─► Merge OutputData into Variables
       │   ├─► currentStepId = result.NextStepId
       │   └─► CONTINUE loop
       │
       └─► IF currentStepId == null
           ├─► Set ProcessInstance.Status = Completed
           ├─► Set CompletedDate
           ├─► Save state
           │
           └─► IF has ParentProcessInstanceId
               └─► Resume parent process
```

### Subprocess Execution

```
3. SubProcessStepHandler.ExecuteAsync()
   │
   ├─► Map parent variables to child (InputMapping)
   │
   ├─► StartProcessAsync(subProcessId, variables)
   │   ├─► Set ParentProcessInstanceId
   │   └─► Execute subprocess
   │
   ├─► IF subprocess status = Waiting
   │   └─► Return RequiresWait = true
   │
   ├─► IF subprocess status = Completed
   │   ├─► Get subprocess variables
   │   ├─► Map child variables to parent (OutputMapping)
   │   └─► Return IsCompleted = true with output
   │
   └─► IF subprocess status = Failed
       └─► Return Failed
```

### Task Completion Flow

```
4. User completes task in UI
   │
   ├─► TaskService.CompleteTaskAsync(taskId, result)
   │   ├─► Load TaskInstance
   │   ├─► Update Status = Completed
   │   ├─► Set Result dictionary
   │   ├─► Set CompletedDate
   │   └─► Save TaskInstance
   │
   └─► ProcessEngine.CompleteTaskAsync(stepInstanceId, result)
       ├─► Load StepInstance
       ├─► Update OutputData = result
       ├─► Update Status = Completed
       ├─► Save StepInstance
       └─► ExecuteProcessAsync(processInstanceId)
           └─► Process continues from next step
```

## JSON to Model Deserialization

```json
// order-processing.json
{
  "id": "ORDER_PROCESSING",
  "name": "Order Processing",
  "version": 1,
  "startStepId": "validate_order",
  "steps": [ ... ]
}
```

```csharp
// Deserialization
var json = File.ReadAllText("order-processing.json");
var definition = JsonSerializer.Deserialize<ProcessDefinition>(
    json,
    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
);

// Result: ProcessDefinition object with all steps as StepDefinition[]
```

## Variable Substitution

Process variables use `{{variableName}}` syntax:

```json
{
  "commandName": "ValidateOrder",
  "parameters": {
    "orderId": "{{orderId}}",
    "customerId": "{{customerId}}"
  }
}
```

At runtime, the engine replaces these with actual values:

```csharp
// Variables dictionary
var variables = new Dictionary<string, object>
{
    { "orderId", "ORD001" },
    { "customerId", "CUST001" }
};

// After substitution, parameters become:
{
    "orderId": "ORD001",
    "customerId": "CUST001"
}
```

## Key Design Patterns

1. **Repository Pattern**: Data access abstraction
   - Interface-based (IProcessDefinitionRepository, etc.)
   - Enables testing with mocks
   - Database implementation independence

2. **Strategy Pattern**: Step handlers
   - Different behavior per step type
   - Registered in ProcessEngine by StepType enum
   - Extensible for new step types

3. **CQRS Pattern**: Commands vs Queries
   - Commands: Write operations (ValidateOrder, FinalizeOrder)
   - Queries: Read operations (CheckInventory, GetOrderDetails)
   - Different handlers for different concerns

4. **Template Pattern**: ProcessEngine execution
   - Common flow for all processes
   - Step-specific logic delegated to handlers
   - Consistent state management

5. **State Machine**: Process/Step status
   - Well-defined states (NotStarted → Running → Completed/Failed/Waiting)
   - Clear transition rules
   - Enables process resumption

## Performance Characteristics

| Operation | Complexity | Notes |
|-----------|-----------|-------|
| Start Process | O(1) | Single insert |
| Execute Step | O(1) | Per step, varies by handler |
| Load Process | O(1) | Single query by ID |
| Find Tasks by User | O(log n) | Indexed query |
| Subprocess Execution | O(m) | m = subprocess steps |

## Scalability Considerations

- **Horizontal Scaling**: Stateless execution engine
- **Database**: Connection pooling enabled by default
- **Concurrency**: Optimistic locking via version numbers
- **Long-Running**: Waiting states allow process persistence
- **Async**: All operations use async/await

## Security Considerations

- **SQL Injection**: Prevented by parameterized queries
- **Access Control**: Implement in TaskService (role-based)
- **Audit Trail**: All state changes timestamped
- **Sensitive Data**: Encrypt variables CLOB if needed
