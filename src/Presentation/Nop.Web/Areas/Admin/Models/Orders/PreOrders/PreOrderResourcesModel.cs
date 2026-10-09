using Nop.Core;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Orders.PreOrders
{
    public record PreOrderResourcesModel : BaseNopEntityModel
    {
        public string RegularOrderTitle { get; set; }
        public string RegularOrderShortDescription { get; set; }

        public string PreOrderTitle { get; set; }
        public string PreOrderShortDescription { get; set; }
        public string PreOrderDescription { get; set; }

        public string PreOrderNotAvailableMessage { get; set; }
        public string PreOrderNoLongerAvailableMessage { get; set; }

        public string RegularToPreOrderCartMessage { get; set; }
        public string PreOrderToRegularCartMessage { get; set; }
        public string PreOrderPreferredDateMessage { get; set; }
    }
}
