namespace Nop.Core.Domain.Invoices
{
    public class PaymentPlacedEvent
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="order">Order</param>
        public PaymentPlacedEvent(PaymentInvoiceModel payment)
        {
            Payment = payment;
        }

        /// <summary>
        /// Order
        /// </summary>
        public PaymentInvoiceModel Payment { get; }
    }
}
