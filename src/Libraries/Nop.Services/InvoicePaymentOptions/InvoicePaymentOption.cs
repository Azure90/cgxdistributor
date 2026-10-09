using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.CustomRewardPoint;
using Nop.Data;

namespace Nop.Services.InvoicePaymentOptions
{
    public partial class InvoicePaymentOption : IInvoicePaymentOption
    {
        private readonly IRepository<Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption> _InvoicePaymentOptionRepository;


        public InvoicePaymentOption(IRepository<Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption> InvoicePaymentOptionRepository)
        {
            _InvoicePaymentOptionRepository = InvoicePaymentOptionRepository;
        }
        public IList<Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption> GetAllInvoicePaymentOption()
        {
            var query = _InvoicePaymentOptionRepository.Table;

            //if (IsPriceRange)
            //    query = query.Where(x => x.IsPriceRange == IsPriceRange);

            //if (IsShoppingItem)
            //    query = query.Where(x => x.IsShoppingItem == IsShoppingItem);

            //query = query.OrderBy(v => v.Id).ThenBy(v => v.Id);
            //var promo = new PagedList<CustomCustomRewards>(query.ToList(), pageIndex, pageSize);
            return query.ToList();
        }

        /// <summary>
        /// Gets a CustomReward with id identifier 
        /// </summary>
        /// <param name="promoId">Identifier</param>
        /// <returns>
        /// Returns a CustomReward
        /// </returns>
        public Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption GetInvoicePaymentOptionById(int promoId)
        {
            if (promoId == 0)
                return null;

            return _InvoicePaymentOptionRepository.GetById(promoId);
        }


        /// <summary>
        /// Insert a InvoicePaymentOption
        /// </summary>
        /// <param name="promo">InvoicePaymentOption</param>
        public void InsertInvoicePaymentOption(Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption promo)
        {
            if (promo == null)
                throw new ArgumentNullException("InvoicePaymentOption");

            _InvoicePaymentOptionRepository.Insert(promo);
        }


        /// <summary>
        /// Update a InvoicePaymentOption
        /// </summary>
        /// <param name="promo">InvoicePaymentOption</param>
        public void UpdateInvoicePaymentOption(Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption promo)
        {
            if (promo == null)
                throw new ArgumentNullException("InvoicePaymentOption");

            _InvoicePaymentOptionRepository.Update(promo);
        }


        /// <summary>
        /// Delete a InvoicePaymentOption
        /// </summary>
        /// <param name="promo">InvoicePaymentOption</param>
        public void DeleteInvoicePaymentOption(Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption promo)
        {
            if (promo == null)
                throw new ArgumentNullException("InvoicePaymentOption");

            _InvoicePaymentOptionRepository.Delete(promo);
        }
    }
}
