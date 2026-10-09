using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.CustomRewardPoint;

namespace Nop.Services.InvoicePaymentOptions
{
    public partial interface IInvoicePaymentOption
    {
        IList<Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption> GetAllInvoicePaymentOption();
        Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption GetInvoicePaymentOptionById(int promoId);
        void InsertInvoicePaymentOption(Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption promo);
        void UpdateInvoicePaymentOption(Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption promo);
        void DeleteInvoicePaymentOption(Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption promoId);
    }
}
