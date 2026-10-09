using System;

namespace Nop.Core.Domain.Orders.PreOrders
{
    /// <summary>
    /// Represents a blackout date range.
    /// </summary>
    public class BlackoutDates : BaseEntity
    {
        /// <summary>
        /// Gets or sets the start date of the blackout period.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date of the blackout period.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Gets or sets the reason for the blackout period.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this blackout period is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the UTC date and time when the record was created.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }
    }
}
