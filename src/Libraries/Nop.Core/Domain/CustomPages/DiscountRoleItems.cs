using System;
using System.Collections.Generic;

namespace Nop.Core.Domain.CustomPages
{
    public class DiscountRoleItems
    {
        public DiscountRoleItems()
        {
            DiscountCustomerRoleId = new List<int>();
        }
        public IList<int> DiscountCustomerRoleId { get; set; } = new List<int>();
        public string DiscountLabelText { get; set; }
    }
}
