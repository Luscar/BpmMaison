using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BpmEngine.Services;
using BpmClient.Models;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace BpmClient.Services
{
    /// <summary>
    /// Command handler for Order Processing operations
    /// Implements business commands using Dapper + Oracle
    /// </summary>
    public class OrderCommandHandler : ICommandHandler
    {
        private readonly string _connectionString;

        public OrderCommandHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection()
        {
            return new OracleConnection(_connectionString);
        }

        public async Task<Dictionary<string, object>> ExecuteAsync(
            string commandName,
            Dictionary<string, object> parameters)
        {
            return commandName switch
            {
                "ValidateOrder" => await ValidateOrder(parameters),
                "FinalizeOrder" => await FinalizeOrder(parameters),
                "RejectOrder" => await RejectOrder(parameters),
                "RecordApproval" => await RecordApproval(parameters),
                _ => throw new NotSupportedException($"Command '{commandName}' is not supported")
            };
        }

        private async Task<Dictionary<string, object>> ValidateOrder(Dictionary<string, object> parameters)
        {
            var orderId = parameters["orderId"].ToString();
            var customerId = parameters["customerId"].ToString();

            using var connection = CreateConnection();

            // Get order details
            var sql = @"
                SELECT ORDER_ID, CUST_ID, CUST_NAME, PROD_ID, QTY,
                       TOTAL_AMT, ORDER_STATUS
                FROM TBL_ORDERS
                WHERE ORDER_ID = :OrderId AND CUST_ID = :CustomerId";

            var order = await connection.QuerySingleOrDefaultAsync<Order>(
                sql,
                new { OrderId = orderId, CustomerId = customerId });

            if (order == null)
            {
                return new Dictionary<string, object>
                {
                    { "isValid", false },
                    { "validationError", "Order not found" }
                };
            }

            // Basic validation
            if (order.Quantity <= 0)
            {
                return new Dictionary<string, object>
                {
                    { "isValid", false },
                    { "validationError", "Invalid quantity" }
                };
            }

            if (order.TotalAmount <= 0)
            {
                return new Dictionary<string, object>
                {
                    { "isValid", false },
                    { "validationError", "Invalid amount" }
                };
            }

            // Update order status
            await connection.ExecuteAsync(
                "UPDATE TBL_ORDERS SET ORDER_STATUS = 'VALIDATED', UPDATED_DT = SYSTIMESTAMP WHERE ORDER_ID = :OrderId",
                new { OrderId = orderId });

            // Return validation result with order details
            return new Dictionary<string, object>
            {
                { "isValid", true },
                { "productId", order.ProductId },
                { "quantity", order.Quantity },
                { "totalAmount", order.TotalAmount },
                { "customerName", order.CustomerName }
            };
        }

        private async Task<Dictionary<string, object>> FinalizeOrder(Dictionary<string, object> parameters)
        {
            var orderId = parameters["orderId"].ToString();
            var approvalStatus = parameters.ContainsKey("approvalStatus")
                ? parameters["approvalStatus"].ToString()
                : null;
            var approvedBy = parameters.ContainsKey("approvedBy")
                ? parameters["approvedBy"].ToString()
                : null;

            using var connection = CreateConnection();

            // Update order as finalized
            var sql = @"
                UPDATE TBL_ORDERS
                SET ORDER_STATUS = 'FINALIZED',
                    APPROVAL_STATUS = :ApprovalStatus,
                    APPROVED_BY = :ApprovedBy,
                    UPDATED_DT = SYSTIMESTAMP
                WHERE ORDER_ID = :OrderId";

            await connection.ExecuteAsync(sql, new
            {
                OrderId = orderId,
                ApprovalStatus = approvalStatus,
                ApprovedBy = approvedBy
            });

            // TODO: In real scenario, reduce inventory, create shipment, etc.

            return new Dictionary<string, object>
            {
                { "success", true },
                { "orderId", orderId },
                { "status", "FINALIZED" }
            };
        }

        private async Task<Dictionary<string, object>> RejectOrder(Dictionary<string, object> parameters)
        {
            var orderId = parameters["orderId"].ToString();
            var reason = parameters.ContainsKey("reason")
                ? parameters["reason"].ToString()
                : "Order rejected";

            using var connection = CreateConnection();

            var sql = @"
                UPDATE TBL_ORDERS
                SET ORDER_STATUS = 'REJECTED',
                    REJECTION_REASON = :Reason,
                    UPDATED_DT = SYSTIMESTAMP
                WHERE ORDER_ID = :OrderId";

            await connection.ExecuteAsync(sql, new
            {
                OrderId = orderId,
                Reason = reason
            });

            return new Dictionary<string, object>
            {
                { "success", true },
                { "orderId", orderId },
                { "status", "REJECTED" },
                { "reason", reason }
            };
        }

        private async Task<Dictionary<string, object>> RecordApproval(Dictionary<string, object> parameters)
        {
            var orderId = parameters["orderId"].ToString();
            var approvalStatus = parameters["approvalStatus"].ToString();
            var approvedBy = parameters["approvedBy"].ToString();
            var comments = parameters.ContainsKey("comments")
                ? parameters["comments"].ToString()
                : null;

            using var connection = CreateConnection();

            var sql = @"
                UPDATE TBL_ORDERS
                SET APPROVAL_STATUS = :ApprovalStatus,
                    APPROVED_BY = :ApprovedBy,
                    APPROVAL_COMMENTS = :Comments,
                    UPDATED_DT = SYSTIMESTAMP
                WHERE ORDER_ID = :OrderId";

            await connection.ExecuteAsync(sql, new
            {
                OrderId = orderId,
                ApprovalStatus = approvalStatus,
                ApprovedBy = approvedBy,
                Comments = comments
            });

            return new Dictionary<string, object>
            {
                { "success", true },
                { "orderId", orderId },
                { "approvalStatus", approvalStatus },
                { "approvedBy", approvedBy },
                { "comments", comments ?? "" }
            };
        }
    }
}
