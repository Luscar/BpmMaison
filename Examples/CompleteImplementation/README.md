# Complete Implementation Example: Order Processing with Approval Subprocess

This example demonstrates a **complete, production-ready BPM implementation** using:
- ✅ **JSON process definitions** (main process + subprocess)
- ✅ **Oracle database with Dapper** using different column names than models
- ✅ **CQRS service implementations** (Commands and Queries)
- ✅ **Complete client-side implementation** with all components wired together

## Quick Start

```bash
# 1. Setup Oracle database
sqlplus bpm_user/password@localhost:1521/XEPDB1 @Database/oracle-schema.sql

# 2. Update connection string in Client/Program.cs

# 3. Build and run
cd Client
dotnet build
dotnet run
```

See [SETUP-GUIDE.md](./SETUP-GUIDE.md) for detailed instructions.

## What's Included

### 📋 Process Definitions (JSON)
- `ProcessDefinitions/order-processing.json` - Main workflow
- `ProcessDefinitions/manager-approval.json` - Subprocess

### 🗄️ Database Layer
- `Database/oracle-schema.sql` - Complete Oracle schema with sample data
- Column names differ from C# models (e.g., `DEF_ID` ↔ `Id`)

### 📦 Models (with Dapper Mapping)
- `Client/Models/ProcessModels.cs` - BPM entities (Process, Step, Task)
- `Client/Models/BusinessModels.cs` - Business entities (Order, Inventory)
- All use `[Column("DB_COLUMN")]` attributes for mapping

### 🔧 Repositories (Dapper + Oracle)
- `Client/Repositories/ProcessDefinitionRepository.cs`
- `Client/Repositories/ProcessInstanceRepository.cs`
- `Client/Repositories/StepInstanceRepository.cs`
- `Client/Repositories/TaskRepository.cs`
- All implement BpmEngine interfaces with Oracle-specific SQL

### 🚀 Services (CQRS Implementation)
- `Client/Services/OrderCommandHandler.cs` - Business commands
  - ValidateOrder, FinalizeOrder, RejectOrder, RecordApproval
- `Client/Services/OrderQueryHandler.cs` - Business queries
  - CheckInventory, CheckApprovalStatus, GetOrderDetails
- `Client/Services/TaskService.cs` - User task management

### 🎯 Main Application
- `Client/Program.cs` - Complete working example
- `Client/BpmClient.csproj` - Project file with dependencies

### 📚 Documentation
- `SETUP-GUIDE.md` - Step-by-step setup and configuration
- `ARCHITECTURE.md` - Detailed architecture diagrams and flow
- This README

## Scenario

**Main Process**: Order Processing (ORDER_PROCESSING)
1. **Validate Order** → Business Step (Command: ValidateOrder)
2. **Check Inventory** → Decision Step (Query: CheckInventory)
   - ✅ If available → Proceed to approval
   - ❌ If not available → Reject order
3. **Manager Approval** → **SubProcess Step** (MANAGER_APPROVAL)
   - Creates approval task for Manager role
   - Records approval decision
4. **Check Approval Result** → Decision Step (Query: CheckApprovalStatus)
   - ✅ If approved → Finalize order
   - ❌ If rejected → Reject order
5. **Finalize/Reject Order** → Business Step (Command)

**SubProcess**: Manager Approval (MANAGER_APPROVAL)
1. **Create Approval Task** → Interactive Step
   - Generates task assigned to "Manager" role
   - Contains order details for review
2. **Record Approval** → Business Step (Command: RecordApproval)
   - Stores approval decision in database
   - Returns result to parent process

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Client Application                     │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  JSON Definitions ──► ProcessEngine ──► Repositories    │
│        │                    │                  │         │
│        │                    ▼                  │         │
│        │            ┌───────────────┐          │         │
│        └───────────►│ Step Handlers │◄─────────┤         │
│                     └───────┬───────┘          │         │
│                             │                  │         │
│                   ┌─────────┴─────────┐        │         │
│                   │                   │        │         │
│                   ▼                   ▼        ▼         │
│          OrderCommandHandler  OrderQueryHandler          │
│                   │                   │                  │
│                   └─────────┬─────────┘                  │
│                             │                            │
│                             ▼                            │
│                  Dapper (Column Mapping)                 │
└─────────────────────────────┼───────────────────────────┘
                              │
                              ▼
                     ┌────────────────┐
                     │ Oracle Database│
                     │  (Different    │
                     │ Column Names)  │
                     └────────────────┘
```

## Key Features Demonstrated

### 1. Column Name Mapping with Dapper

C# models use clean property names while database uses abbreviated columns:

```csharp
public class Order
{
    [Column("ORDER_ID")]      // Database: ORDER_ID
    public string OrderId { get; set; }  // C#: OrderId

    [Column("CUST_NAME")]     // Database: CUST_NAME
    public string CustomerName { get; set; }  // C#: CustomerName

    [Column("TOTAL_AMT")]     // Database: TOTAL_AMT
    public decimal TotalAmount { get; set; }  // C#: TotalAmount
}
```

### 2. Subprocess with Variable Mapping

Parent process passes variables to subprocess and receives results:

```json
{
  "type": 5,
  "subProcessId": "MANAGER_APPROVAL",
  "inputMapping": {
    "orderId": "orderId",
    "orderAmount": "totalAmount",
    "customerName": "customerName"
  },
  "outputMapping": {
    "approvalStatus": "approvalStatus",
    "approvedBy": "approvedBy",
    "approvalComments": "comments"
  }
}
```

### 3. CQRS Pattern

Clean separation of commands (writes) and queries (reads):

```csharp
// Command - Modifies state
await commandHandler.ExecuteAsync("FinalizeOrder", parameters);

// Query - Reads state for decisions
var result = await queryHandler.ExecuteAsync("CheckInventory", parameters);
```

### 4. Interactive Tasks

Process waits for human action:

```csharp
// Engine creates task and waits
var task = await taskService.CreateTaskAsync(...);

// Later, user completes task
await taskService.CompleteTaskAsync(taskId, result);

// Engine resumes and continues
await engine.CompleteTaskAsync(stepInstanceId, result);
```

### 5. Decision Routing

Dynamic process flow based on business logic:

```json
{
  "type": 2,
  "queryName": "CheckInventory",
  "routes": [
    {
      "condition": "isAvailable == true && quantity <= stockLevel",
      "nextStepId": "manager_approval_subprocess"
    },
    {
      "condition": "isAvailable == false || quantity > stockLevel",
      "nextStepId": "reject_order"
    }
  ]
}
```

## Example Output

```
=== BPM Order Processing Demo ===

Loading process definitions...
  ✓ Loaded: Order Processing v1
  ✓ Loaded: Manager Approval v1

Creating sample order...
  ✓ Created order ORD001 for customer John Doe

Starting Order Processing workflow...
  ✓ Started process instance: a1b2c3d4-...

Executing process steps...
[BusinessStepHandler] Executing command: ValidateOrder
[DecisionStepHandler] Executing query: CheckInventory
[SubProcessStepHandler] Starting subprocess MANAGER_APPROVAL

=== Approval Task Created ===
Task ID: e5f6g7h8-...
Type: ORDER_APPROVAL
Order ID: ORD001
Amount: 1500

Simulating manager approval...
[TaskService] Completed task e5f6g7h8-...

Resuming subprocess...
[BusinessStepHandler] Executing command: RecordApproval
Subprocess Status: Completed

Resuming main process...
[BusinessStepHandler] Executing command: FinalizeOrder

=== Final Results ===
Process Status: Completed
Approval Status: APPROVED
Order Status: FINALIZED
```

## File Structure

```
CompleteImplementation/
├── README.md                          (this file)
├── SETUP-GUIDE.md                     (step-by-step setup)
├── ARCHITECTURE.md                    (detailed architecture)
│
├── ProcessDefinitions/
│   ├── order-processing.json          (main process)
│   └── manager-approval.json          (subprocess)
│
├── Database/
│   └── oracle-schema.sql              (complete schema + data)
│
└── Client/
    ├── BpmClient.csproj               (project file)
    ├── Program.cs                     (main application)
    │
    ├── Models/
    │   ├── ProcessModels.cs           (BPM entities with column mapping)
    │   └── BusinessModels.cs          (Order, Inventory with mapping)
    │
    ├── Repositories/
    │   ├── ProcessDefinitionRepository.cs
    │   ├── ProcessInstanceRepository.cs
    │   ├── StepInstanceRepository.cs
    │   └── TaskRepository.cs
    │
    └── Services/
        ├── OrderCommandHandler.cs     (CQRS commands)
        ├── OrderQueryHandler.cs       (CQRS queries)
        └── TaskService.cs             (task management)
```

## Technologies Used

- **.NET 8.0** - Target framework
- **BpmEngine** - Workflow engine (from this repository)
- **Dapper 2.1.28** - Micro-ORM with attribute-based mapping
- **Oracle.ManagedDataAccess.Core 23.4.0** - Oracle database provider
- **System.Text.Json 8.0.0** - JSON serialization

## Next Steps

1. **Review Documentation**
   - Read [SETUP-GUIDE.md](./SETUP-GUIDE.md) for setup instructions
   - Read [ARCHITECTURE.md](./ARCHITECTURE.md) for architecture details

2. **Customize for Your Use Case**
   - Modify process definitions in JSON files
   - Add your own commands and queries
   - Extend models for your business entities

3. **Add Production Features**
   - Authentication and authorization
   - Logging (Serilog, NLog)
   - Error handling and retry logic
   - Monitoring and metrics
   - REST API layer
   - Web UI for process/task management

4. **Testing**
   - Unit tests for services
   - Integration tests for repositories
   - End-to-end process tests

## Support

For questions or issues with the BpmEngine, please refer to the main repository documentation.

For questions specific to this example, review the documentation files included in this directory.
