using Nop.Core;
//using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.Common
{
    public partial class CustomPromotionService : ICustomPromotionService
    {
        #region Fields

        private readonly IRepository<CustomPromotions> _promotionRepository;

        #endregion

        #region Ctor
        public CustomPromotionService(IRepository<CustomPromotions> promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        #endregion


        #region Methods

        /// <summary>
        /// Get all promotions
        /// </summary>
        /// <param name="pageIndex">page index</param>
        /// <param name="pageSize">page size</param>
        /// <returns>
        /// Returns a list of promotions
        /// </returns>
        public IList<CustomPromotions> GetAllPromotions()
        {
            var query = _promotionRepository.Table;

            //if (IsPriceRange)
            //    query = query.Where(x => x.IsPriceRange == IsPriceRange);

            //if (IsShoppingItem)
            //    query = query.Where(x => x.IsShoppingItem == IsShoppingItem);

            //query = query.OrderBy(v => v.Id).ThenBy(v => v.Id);
            //var promo = new PagedList<CustomPromotions>(query.ToList(), pageIndex, pageSize);
            return query.ToList();
        }

        /// <summary>
        /// Gets a promotion with id identifier 
        /// </summary>
        /// <param name="promoId">Identifier</param>
        /// <returns>
        /// Returns a promotion
        /// </returns>
        public CustomPromotions GetPromotionById(int promoId)
        {
            if (promoId == 0)
                return null;

            return _promotionRepository.GetById(promoId);
        }


        /// <summary>
        /// Insert a promotion
        /// </summary>
        /// <param name="promo">Promotions</param>
        public void InsertPromotion(CustomPromotions promo)
        {
            if (promo == null)
                throw new ArgumentNullException("promotions");

            _promotionRepository.Insert(promo);
        }


        /// <summary>
        /// Update a promotion
        /// </summary>
        /// <param name="promo">Promotions</param>
        public void UpdatePromotion(CustomPromotions promo)
        {
            if (promo == null)
                throw new ArgumentNullException("promotions");

            _promotionRepository.Update(promo);
        }


        /// <summary>
        /// Delete a promotion
        /// </summary>
        /// <param name="promo">Promotions</param>
        public void DeletePromotion(CustomPromotions promo)
        {
            if (promo == null)
                throw new ArgumentNullException("promotions");

            _promotionRepository.Delete(promo);
        }

        #endregion
    }
}
