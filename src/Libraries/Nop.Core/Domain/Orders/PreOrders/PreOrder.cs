using System;

namespace Nop.Core.Domain.Orders.PreOrders
{
    public class PreOrder : BaseEntity
    {
        /// <summary>
        /// Comma-separated customer role IDs.
        /// </summary>
        public string CustomerRoleIds { get; set; }

        /// <summary>
        /// Comma-separated product IDs.
        /// </summary>
        public string ProductIds { get; set; }

        /// <summary>
        /// Comma-separated product SKUs.
        /// </summary>
        public string ProductSkus { get; set; }

        /// <summary>
        /// Comma-separated category IDs.
        /// </summary>
        public string CategoryIds { get; set; }

        /// <summary>
        /// Minimum lead time in days.
        /// </summary>
        public int MinimumLeadTime { get; set; }

        /// <summary>
        /// Number of allowed delivery days.
        /// </summary>
        public int AllowedDeliveryDays { get; set; }

        /// <summary>
        /// Comma-separated blackout dates (yyyy-MM-dd).
        /// </summary>
        public string BlackoutDates { get; set; }

        /// <summary>
        /// Minimum quantity allowed per product.
        /// </summary>
        public int ProductMinimumQuantity { get; set; }

        /// <summary>
        /// Maximum quantity allowed per product.
        /// </summary>
        public int ProductMaximumQuantity { get; set; }

        /// <summary>
        /// Gets or sets the last available delivery date customers can select for a preorder.
        /// </summary>
        public DateTime DeliveryEndDate { get; set; }

        /// <summary>
        /// Gets or sets the last date customers can place a preorder.
        /// This value is automatically calculated as DeliveryEndDate minus MinimumLeadTime
        /// and is used as the maximum selectable date in the preorder date picker.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Date the record was created (UTC).
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Date the record was last updated (UTC).
        /// </summary>
        public DateTime? UpdatedOnUtc { get; set; }
    }
}
