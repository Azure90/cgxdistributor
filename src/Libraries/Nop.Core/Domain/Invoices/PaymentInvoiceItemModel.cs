namespace Nop.Core.Domain.Invoices
{
    public class PaymentInvoiceItemModel
    {
       public string Number { get; set; }
       public decimal Amount { get; set; }
       public string InvoiceType { get; set; }

    }
}
