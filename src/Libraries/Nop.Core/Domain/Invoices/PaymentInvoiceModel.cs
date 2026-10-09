using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Nop.Core.Domain.Invoices
{
    public class PaymentInvoiceModel
    {
        public PaymentInvoiceModel()
        {
            InvoiceDetails = new List<PaymentInvoiceItemModel>();
            CustomerNumbers = new List<SelectListItem>();
        }

        public int PaymentId { get; set; }
        public string CustomerNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PaymentInvoiceItemModel> InvoiceDetails { get; set; }
        public string IsCitagenix { get; set; }
        public string Invoice { get; set; }
        public string WebPortalTransactionId { get; set; }

        public IList<SelectListItem> CustomerNumbers { get; set; }
    }
}
