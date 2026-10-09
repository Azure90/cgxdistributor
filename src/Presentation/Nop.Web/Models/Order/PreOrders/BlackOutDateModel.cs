using System;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Order.PreOrders
{
    public partial record BlackOutDateModel : BaseNopEntityModel
    {
        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public string Reason { get; set; }

        public bool IsActive { get; set; }
    }
}
