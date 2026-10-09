using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.InvoicePaymentOptions
{
    public partial class InvoicePaymentOption : BaseEntity
    {
        public string CustomerRole { get; set; }
        public string PaymentTittle { get; set; }
        public string paymentOption { get; set; }
        public bool Paynowtoption { get; set; }
        public bool PaynowButton { get; set; }
        public bool Active { get; set; }
    }
}
