using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Web.Models.DistributorPortal;

namespace Nop.Web.Areas.Admin.Models.InvoicePayment
{
    public class InvoicePaymentModel : BaseEntity
    {
        public IList<int> CustomerRole { get; set; }
        public string CustomerRoleName { get; set; }
        public string PaymentTittle { get; set; }
        public string paymentOption { get; set; }
        public bool Paynowtoption { get; set; }
        public bool PaynowButton { get; set; }
        public bool Active { get; set; }
        public List<InvoicePaymentModel> ModelList { get; set; }
        public IList<SelectListItem> AvailableCustomerRoles { get; set; }
        public InvoicePaymentModel()
        {
            ModelList = new List<InvoicePaymentModel>();
            AvailableCustomerRoles = new List<SelectListItem>();
            CustomerRole = new List<int>();
        }
    }
}
