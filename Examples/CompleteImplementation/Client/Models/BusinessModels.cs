using System;
using Dapper;

namespace BpmClient.Models
{
    /// <summary>
    /// Order Model
    /// Database uses TBL_ORDERS with abbreviated column names
    /// </summary>
    public class Order
    {
        [Column("ORDER_ID")]
        public string OrderId { get; set; }

        [Column("CUST_ID")]
        public string CustomerId { get; set; }

        [Column("CUST_NAME")]
        public string CustomerName { get; set; }

        [Column("PROD_ID")]
        public string ProductId { get; set; }

        [Column("QTY")]
        public int Quantity { get; set; }

        [Column("TOTAL_AMT")]
        public decimal TotalAmount { get; set; }

        [Column("ORDER_STATUS")]
        public string OrderStatus { get; set; }

        [Column("APPROVAL_STATUS")]
        public string ApprovalStatus { get; set; }

        [Column("APPROVED_BY")]
        public string ApprovedBy { get; set; }

        [Column("APPROVAL_COMMENTS")]
        public string ApprovalComments { get; set; }

        [Column("REJECTION_REASON")]
        public string RejectionReason { get; set; }

        [Column("CREATED_DT")]
        public DateTime CreatedDate { get; set; }

        [Column("UPDATED_DT")]
        public DateTime UpdatedDate { get; set; }
    }

    /// <summary>
    /// Inventory Model
    /// Database uses TBL_INVENTORY with abbreviated names
    /// </summary>
    public class Inventory
    {
        [Column("PROD_ID")]
        public string ProductId { get; set; }

        [Column("PROD_NAME")]
        public string ProductName { get; set; }

        [Column("STOCK_LEVEL")]
        public int StockLevel { get; set; }

        [Column("IS_AVAILABLE")]
        public int IsAvailableFlag { get; set; }  // Oracle uses NUMBER(1) for boolean

        [Column("UPDATED_DT")]
        public DateTime UpdatedDate { get; set; }

        // Computed property
        public bool IsAvailable => IsAvailableFlag == 1;
    }
}
