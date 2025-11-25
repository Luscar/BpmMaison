using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BpmEngine.Core.Models;
using BpmEngine.Engine;
using BpmEngine.Services.Impl;
using BpmClient.Repositories;
using BpmClient.Services;
using Dapper;

namespace BpmClient
{
    class Program
    {
        // Oracle connection string - adjust for your environment
        private const string ConnectionString =
            "User Id=your_user;Password=your_password;Data Source=your_oracle_host:1521/your_service";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== BPM Order Processing Demo ===\n");

            // Configure Dapper to use column attributes
            SqlMapper.Settings.EnablePropertyMapping = true;

            try
            {
                // Step 1: Load and save process definitions
                await LoadProcessDefinitions();

                // Step 2: Create sample order
                await CreateSampleOrder();

                // Step 3: Execute the process
                await ExecuteOrderProcess();

                Console.WriteLine("\n=== Demo completed successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static async Task LoadProcessDefinitions()
        {
            Console.WriteLine("Loading process definitions...");

            var repository = new ProcessDefinitionRepository(ConnectionString);

            // Load main process
            var mainProcessJson = await File.ReadAllTextAsync(
                "ProcessDefinitions/order-processing.json");
            var mainProcess = JsonSerializer.Deserialize<ProcessDefinition>(
                mainProcessJson,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            await repository.SaveAsync(mainProcess);
            Console.WriteLine($"  ✓ Loaded: {mainProcess.Name} v{mainProcess.Version}");

            // Load subprocess
            var subProcessJson = await File.ReadAllTextAsync(
                "ProcessDefinitions/manager-approval.json");
            var subProcess = JsonSerializer.Deserialize<ProcessDefinition>(
                subProcessJson,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            await repository.SaveAsync(subProcess);
            Console.WriteLine($"  ✓ Loaded: {subProcess.Name} v{subProcess.Version}");

            Console.WriteLine();
        }

        static async Task CreateSampleOrder()
        {
            Console.WriteLine("Creating sample order...");

            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(ConnectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO TBL_ORDERS (
                    ORDER_ID, CUST_ID, CUST_NAME, PROD_ID, QTY, TOTAL_AMT,
                    ORDER_STATUS, CREATED_DT, UPDATED_DT
                ) VALUES (
                    :OrderId, :CustomerId, :CustomerName, :ProductId, :Quantity, :TotalAmount,
                    :OrderStatus, SYSTIMESTAMP, SYSTIMESTAMP
                )";

            await connection.ExecuteAsync(sql, new
            {
                OrderId = "ORD001",
                CustomerId = "CUST001",
                CustomerName = "John Doe",
                ProductId = "PROD001",
                Quantity = 10,
                TotalAmount = 1500.00m,
                OrderStatus = "PENDING"
            });

            Console.WriteLine("  ✓ Created order ORD001 for customer John Doe");
            Console.WriteLine();
        }

        static async Task ExecuteOrderProcess()
        {
            Console.WriteLine("Initializing BPM Engine...\n");

            // Initialize repositories
            var processDefRepo = new ProcessDefinitionRepository(ConnectionString);
            var processInstRepo = new ProcessInstanceRepository(ConnectionString);
            var stepInstRepo = new StepInstanceRepository(ConnectionString);
            var taskRepo = new TaskRepository(ConnectionString);

            // Initialize services
            var commandHandler = new OrderCommandHandler(ConnectionString);
            var queryHandler = new OrderQueryHandler(ConnectionString);
            var taskService = new TaskService(taskRepo);
            var conditionEvaluator = new SimpleConditionEvaluator();

            // Create BPM Engine
            var engine = new ProcessEngine(
                processDefRepo,
                processInstRepo,
                stepInstRepo,
                commandHandler,
                queryHandler,
                taskService,
                conditionEvaluator
            );

            // Start the main process
            Console.WriteLine("Starting Order Processing workflow...");

            var variables = new Dictionary<string, object>
            {
                { "orderId", "ORD001" },
                { "customerId", "CUST001" }
            };

            var processInstanceId = await engine.StartProcessAsync(
                "ORDER_PROCESSING",
                1,
                variables
            );

            Console.WriteLine($"  ✓ Started process instance: {processInstanceId}\n");

            // Execute until we hit a waiting state (Interactive step for approval)
            Console.WriteLine("Executing process steps...");
            await engine.ExecuteProcessAsync(processInstanceId);

            // Check process status
            var processInstance = await processInstRepo.GetByIdAsync(processInstanceId);
            Console.WriteLine($"\nMain Process Status: {processInstance.Status}");
            Console.WriteLine($"Current Step: {processInstance.CurrentStepId}");

            // Find pending tasks (approval task from subprocess)
            var tasks = await taskRepo.GetByRoleAsync("Manager", TaskStatus.Pending);
            var taskList = new List<TaskInstance>(tasks);

            if (taskList.Count > 0)
            {
                var approvalTask = taskList[0];
                Console.WriteLine($"\n=== Approval Task Created ===");
                Console.WriteLine($"Task ID: {approvalTask.Id}");
                Console.WriteLine($"Type: {approvalTask.TaskType}");
                Console.WriteLine($"Assigned to: {approvalTask.AssignedRole}");
                Console.WriteLine($"Order ID: {approvalTask.TaskData["orderId"]}");
                Console.WriteLine($"Amount: {approvalTask.TaskData["orderAmount"]}");

                // Simulate manager approval
                Console.WriteLine("\nSimulating manager approval...");

                // Assign to manager
                await taskService.AssignTaskAsync(approvalTask.Id, "manager.smith");

                // Complete the task
                var approvalResult = new Dictionary<string, object>
                {
                    { "approvalStatus", "APPROVED" },
                    { "approvedBy", "manager.smith" },
                    { "comments", "Order approved - customer has good credit history" }
                };

                await taskService.CompleteTaskAsync(approvalTask.Id, approvalResult);

                // Resume the subprocess
                Console.WriteLine("\nResuming subprocess...");
                await engine.CompleteTaskAsync(approvalTask.StepInstanceId, approvalResult);

                // Get the subprocess instance
                var subProcesses = await processInstRepo.GetByParentIdAsync(processInstanceId);
                var subProcessList = new List<ProcessInstance>(subProcesses);

                if (subProcessList.Count > 0)
                {
                    var subProcessId = subProcessList[0].Id;
                    Console.WriteLine($"Executing subprocess: {subProcessId}");
                    await engine.ExecuteProcessAsync(subProcessId);

                    var subProcess = await processInstRepo.GetByIdAsync(subProcessId);
                    Console.WriteLine($"Subprocess Status: {subProcess.Status}");

                    // Continue main process after subprocess completes
                    if (subProcess.Status == ProcessStatus.Completed)
                    {
                        Console.WriteLine("\nResuming main process...");
                        await engine.ExecuteProcessAsync(processInstanceId);
                    }
                }
            }

            // Final status
            processInstance = await processInstRepo.GetByIdAsync(processInstanceId);
            Console.WriteLine($"\n=== Final Results ===");
            Console.WriteLine($"Process Status: {processInstance.Status}");
            Console.WriteLine($"Approval Status: {processInstance.Variables.GetValueOrDefault("approvalStatus")}");
            Console.WriteLine($"Approved By: {processInstance.Variables.GetValueOrDefault("approvedBy")}");
            Console.WriteLine($"Comments: {processInstance.Variables.GetValueOrDefault("comments")}");

            // Check order in database
            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(ConnectionString);
            var order = await connection.QuerySingleAsync<dynamic>(
                @"SELECT ORDER_STATUS, APPROVAL_STATUS, APPROVED_BY, APPROVAL_COMMENTS
                  FROM TBL_ORDERS WHERE ORDER_ID = 'ORD001'");

            Console.WriteLine($"\n=== Order Status in Database ===");
            Console.WriteLine($"Order Status: {order.ORDER_STATUS}");
            Console.WriteLine($"Approval Status: {order.APPROVAL_STATUS}");
            Console.WriteLine($"Approved By: {order.APPROVED_BY}");
            Console.WriteLine($"Comments: {order.APPROVAL_COMMENTS}");
        }
    }
}
