using System;

namespace Nop.Web.Models.OfflineTransaction
{
    public class InvoiceModel
    {
        public string Invoice { get; set; }
        public string CustomerName { get; set; }
        public string InvoiceStatus { get; set; }
        public string InvoiceDate { get; set; }
        public string InvoiceTotal { get; set; }
        public string InvoiceType { get; set; }
        public string Balance { get; set; }
        public string ButtonText { get; set; }
        public string EmailAddress { get; set; }
        public string OrderId { get; set; }
    }
}
