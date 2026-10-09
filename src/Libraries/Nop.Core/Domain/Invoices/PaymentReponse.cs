using System;

namespace Nop.Core.Domain.Invoices
{
    [Serializable]
    public class PaymentReponse
    {
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }
    }
}
