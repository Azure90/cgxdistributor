using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.DiscountPromotions
{
    public class DiscountPromotions : BaseEntity
    {
        public string DiscountCode { get; set; }
        public int OrderCount { get; set; }
        public int ReminderDays { get; set; }
         public string AddDays { get; set; }
        public int MessageTemplatetId { get; set; }
        public int ReminderMessageTemplatetId { get; set; }
        public int DiscountPromotionType { get; set; }
        public bool Active { get; set; }        
        public DateTime? CreatedDate { get; set; }
    }
}
