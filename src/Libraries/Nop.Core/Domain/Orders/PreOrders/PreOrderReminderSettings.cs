using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.Orders.PreOrders
{
    /// <summary>
    /// Represents the pre-order reminder email configuration.
    /// </summary>
    public partial class PreOrderReminderSettings : BaseEntity
    {
        /// <summary>
        /// Gets or sets the initial reminder email template identifier.
        /// </summary>
        public int InitialReminderTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the number of days before the preferred delivery date
        /// to send the initial reminder email.
        /// </summary>
        public int InitialReminderDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the initial reminder is active.
        /// </summary>
        public bool InitialReminderActive { get; set; }

        /// <summary>
        /// Gets or sets the editable reminder email template identifier.
        /// </summary>
        public int EditableReminderTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the number of days before the preferred delivery date
        /// to send the editable reminder email.
        /// </summary>
        public int EditableReminderDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the editable reminder is active.
        /// </summary>
        public bool EditableReminderActive { get; set; }

        /// <summary>
        /// Gets or sets the final (non-editable) reminder email template identifier.
        /// </summary>
        public int FinalReminderTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the number of days before the preferred delivery date
        /// to send the final reminder email.
        /// </summary>
        public int FinalReminderDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the final reminder is active.
        /// </summary>
        public bool FinalReminderActive { get; set; }

        /// <summary>
        /// Gets or sets the delivery reminder email template identifier.
        /// </summary>
        public int DeliveryReminderTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the number of days before the preferred delivery date
        /// to send the delivery reminder email.
        /// </summary>
        public int DeliveryReminderDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the delivery reminder is active.
        /// </summary>
        public bool DeliveryReminderActive { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the reminder settings were created (UTC).
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the reminder settings were last updated (UTC).
        /// </summary>
        public DateTime? UpdatedOnUtc { get; set; }      

    }
}
