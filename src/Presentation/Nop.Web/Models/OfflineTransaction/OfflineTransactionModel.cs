using System.Collections.Generic;

namespace Nop.Web.Models.OfflineTransaction
{
    public class OfflineTransactionModel
    {
        public OfflineTransactionModel()
        {
            Invoices = new List<InvoiceModel>();
        }

        public double TotalOutstandingPayment { get; set; } // not pay invoice
        public double OutstandingInvoices { get; set; } // Total Invoices Not Payed yet.
        public double AccountCredits { get; set; } // comming from Credit Memo Table
        public string ErrorMessage { get; set; }

        public IList<InvoiceModel> Invoices { get; set; }
    }
}
