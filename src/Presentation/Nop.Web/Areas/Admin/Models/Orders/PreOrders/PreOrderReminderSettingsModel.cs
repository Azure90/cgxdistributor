using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Orders.PreOrders
{
    /// <summary>
    /// Represents pre-order reminder email settings.
    /// </summary>
    public record PreOrderReminderSettingsModel : BaseNopEntityModel
    {
        #region Ctor
        public PreOrderReminderSettingsModel()
        {
            AvailableMessageTemplates = new List<SelectListItem>();
        }

        #endregion

        public int InitialReminderTemplateId { get; set; }
        public int InitialReminderDays { get; set; }
        public bool InitialReminderActive { get; set; }
        public int EditableReminderTemplateId { get; set; }
        public int EditableReminderDays { get; set; }
        public bool EditableReminderActive { get; set; }
        public int FinalReminderTemplateId { get; set; }
        public int FinalReminderDays { get; set; }
        public bool FinalReminderActive { get; set; }
        public int DeliveryReminderTemplateId { get; set; }
        public int DeliveryReminderDays { get; set; }
        public bool DeliveryReminderActive { get; set; }
        public IList<SelectListItem> AvailableMessageTemplates { get; set; }
    }
}
