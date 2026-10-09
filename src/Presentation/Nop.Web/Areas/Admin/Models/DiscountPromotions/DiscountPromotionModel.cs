using System;
using System.Collections.Generic;
using Nop.Core;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Web.Areas.Admin.Models.DiscountPromotions
{
    public class DiscountPromotionModel : BaseEntity
    {
        public string DiscountCode { get; set; }
        public string DaysforNewPurchasers { get; set; }
        public string DaysforLostCustomers { get; set; }
        public int OrderCount { get; set; }
        public int ReminderDays { get; set; }
        public string AddDays { get; set; }
        public bool Active { get; set; }
        public int MessageTemplatetId { get; set; }
        public int ReminderMessageTemplatetId { get; set; }
        public int DiscountPromotionType { get; set; }
        public IList<SelectListItem> MessageTemplateNames { get; set; }
        public IList<SelectListItem> ReminderMessageTemplateNames { get; set; }
        public IList<SelectListItem> AvailableMessageTemplates { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<DiscountPromotionModel> ModelList { get; set; }
        public DiscountPromotionModel()
        {
            
            ModelList = new List<DiscountPromotionModel>();
            MessageTemplateNames = new List<SelectListItem>();
            ReminderMessageTemplateNames = new List<SelectListItem>();
            AvailableMessageTemplates = new List<SelectListItem>();

        }
    }
}
