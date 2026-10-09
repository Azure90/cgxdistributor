using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.Common
{
    public class CustomPromotions : BaseEntity
    {
        public int FromRange { get; set; }
        public int ToRange { get; set; }
        public string ProductId { get; set; }
        public decimal Price { get; set; }
        public int TypeId { get; set; }
        public bool IsActive { get; set; }
        public string SelectedType { get; set; }
        public string CustomerRoleid { get; set; }        
        public DateTime? PromotionDateFrom { get; set; }

        public DateTime? PromotionDateTo { get; set; }
        public string SelectedOption { get; set; }
    }
}
