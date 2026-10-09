using System;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Checkout
{
    public partial record CheckoutPaymentInfoModel : BaseNopModel
    {
        public Type PaymentViewComponent { get; set; }

        /// <summary>
        /// Used on one-page checkout page
        /// </summary>
        public bool DisplayOrderTotals { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current One Page Checkout is for a PreOrder Cart.
        /// </summary>
        public bool IsPreOrderCart { get; set; }
    }
}