namespace Nop.Core.Domain.Invoices
{
    public class InvoiceDetails
    {
        public string DocEntry { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string QtyOrd { get; set; }
        public string QtyShip { get; set; }
        public string QtyBackOrd { get; set; }
        public string UnitPrice { get; set; }
        public string DiscountPrc { get; set; }
        public string Tax { get; set; }
        public string NetPrice { get; set; }
        public string Total { get; set; }
        public string TaxCode { get; set; }
        public string TaxPrc { get; set; }
        public string NetTax { get; set; }
        public string TotalTax { get; set; }
    }
}
