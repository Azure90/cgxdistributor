using System;

namespace Nop.Web.Models.OfflineTransaction
{
    public class Invoice
    {
        public string DocEntry { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentDate { get; set; }
        public string DueDate { get; set; }
        public string CustomerNo { get; set; }
        public string ReferencePoNo { get; set; }
        public string AccountManager { get; set; }
        public string ServiceContractNo { get; set; }
        public string BillToName { get; set; }
        public string BillToAddress { get; set; }
        public string ShipToName { get; set; }
        public string ShipToAddress { get; set; }
        public string ShippingType { get; set; }
        public string FreightCharge { get; set; }
        public string TotalTaxAmount { get; set; }
        public string TotalOutstanding { get; set; }
        public string Downpayment { get; set; }
        public string Currency { get; set; }
        public string APEmail { get; set; }
        public string DescriptionFreight { get; set; }
        public string NetFreight { get; set; }
        public string TaxCodeFreight { get; set; }
        public string TaxPrcFreight { get; set; }
        public string TaxFreight { get; set; }
        public string GrossFreight { get; set; }
        public string Paid { get; set; }
        public string Balance { get; set; }
        public string OrderId { get; set; }
        public string DocStatus { get; set; }
        public string CreateDate { get; set; }
        public string UpdateDate { get; set; }

        public int StoreId { get; set; }
        public int CustomerLanguageId { get; set; }
    }
}
