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
    /// Query handler for Order Processing operations
    /// Implements business queries using Dapper + Oracle
    /// </summary>
    public class OrderQueryHandler : IQueryHandler
    {
        private readonly string _connectionString;

        public OrderQueryHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection()
        {
            return new OracleConnection(_connectionString);
        }

        public async Task<Dictionary<string, object>> ExecuteAsync(
            string queryName,
            Dictionary<string, object> parameters)
        {
            return queryName switch
            {
                "CheckInventory" => await CheckInventory(parameters),
                "CheckApprovalStatus" => await CheckApprovalStatus(parameters),
                "GetOrderDetails" => await GetOrderDetails(parameters),
                _ => throw new NotSupportedException($"Query '{queryName}' is not supported")
            };
        }

        private async Task<Dictionary<string, object>> CheckInventory(Dictionary<string, object> parameters)
        {
            var productId = parameters["productId"].ToString();

            // Extract quantity - handle both int and JsonElement types
            int quantity;
            if (parameters["quantity"] is int intQty)
            {
                quantity = intQty;
            }
            else if (parameters["quantity"] is long longQty)
            {
                quantity = (int)longQty;
            }
            else if (parameters["quantity"] is System.Text.Json.JsonElement jsonElement)
            {
                quantity = jsonElement.GetInt32();
            }
            else
            {
                quantity = Convert.ToInt32(parameters["quantity"]);
            }

            using var connection = CreateConnection();

            var sql = @"
                SELECT PROD_ID, PROD_NAME, STOCK_LEVEL, IS_AVAILABLE
                FROM TBL_INVENTORY
                WHERE PROD_ID = :ProductId";

            var inventory = await connection.QuerySingleOrDefaultAsync<Inventory>(
                sql,
                new { ProductId = productId });

            if (inventory == null)
            {
                return new Dictionary<string, object>
                {
                    { "isAvailable", false },
                    { "stockLevel", 0 },
                    { "reason", "Product not found" }
                };
            }

            var hasStock = inventory.IsAvailable && inventory.StockLevel >= quantity;

            return new Dictionary<string, object>
            {
                { "isAvailable", hasStock },
                { "stockLevel", inventory.StockLevel },
                { "productName", inventory.ProductName },
                { "requestedQuantity", quantity },
                { "reason", hasStock ? "In stock" : "Insufficient inventory" }
            };
        }

        private async Task<Dictionary<string, object>> CheckApprovalStatus(Dictionary<string, object> parameters)
        {
            var approvalStatus = parameters["approvalStatus"].ToString();

            // This is a simple status check that routes the process
            // In a real scenario, you might query additional approval details

            return new Dictionary<string, object>
            {
                { "approvalStatus", approvalStatus },
                { "isApproved", approvalStatus == "APPROVED" }
            };
        }

        private async Task<Dictionary<string, object>> GetOrderDetails(Dictionary<string, object> parameters)
        {
            var orderId = parameters["orderId"].ToString();

            using var connection = CreateConnection();

            var sql = @"
                SELECT ORDER_ID, CUST_ID, CUST_NAME, PROD_ID, QTY,
                       TOTAL_AMT, ORDER_STATUS, APPROVAL_STATUS,
                       APPROVED_BY, APPROVAL_COMMENTS, REJECTION_REASON,
                       CREATED_DT, UPDATED_DT
                FROM TBL_ORDERS
                WHERE ORDER_ID = :OrderId";

            var order = await connection.QuerySingleOrDefaultAsync<Order>(
                sql,
                new { OrderId = orderId });

            if (order == null)
            {
                return new Dictionary<string, object>
                {
                    { "found", false }
                };
            }

            return new Dictionary<string, object>
            {
                { "found", true },
                { "orderId", order.OrderId },
                { "customerId", order.CustomerId },
                { "customerName", order.CustomerName },
                { "productId", order.ProductId },
                { "quantity", order.Quantity },
                { "totalAmount", order.TotalAmount },
                { "orderStatus", order.OrderStatus },
                { "approvalStatus", order.ApprovalStatus ?? "" },
                { "approvedBy", order.ApprovedBy ?? "" },
                { "approvalComments", order.ApprovalComments ?? "" },
                { "rejectionReason", order.RejectionReason ?? "" }
            };
        }
    }
}
