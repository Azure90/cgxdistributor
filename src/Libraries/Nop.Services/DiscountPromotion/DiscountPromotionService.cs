using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.CustomRewardPoint;
using Nop.Core.Domain.DiscountPromotions;
using Nop.Data;

namespace Nop.Services.DiscountPromotion
{
    public partial class DiscountPromotionService: IDiscountPromotionService
    {
        private readonly IRepository<DiscountPromotions> _DiscountPromotionsRepository;
        public DiscountPromotionService(IRepository<DiscountPromotions> DiscountPromotionsdRepository)
        {
            _DiscountPromotionsRepository = DiscountPromotionsdRepository;
        }
        public IList<DiscountPromotions> GetAllDiscountPromotions()
        {
            var query = _DiscountPromotionsRepository.Table;

            //if (IsPriceRange)
            //    query = query.Where(x => x.IsPriceRange == IsPriceRange);

            //if (IsShoppingItem)
            //    query = query.Where(x => x.IsShoppingItem == IsShoppingItem);

            //query = query.OrderBy(v => v.Id).ThenBy(v => v.Id);
            //var promo = new PagedList<CustomCustomRewards>(query.ToList(), pageIndex, pageSize);
            return query.ToList();
        }
        public DiscountPromotions GetDiscountPromotionsById(int promoId)
        {
            if (promoId == 0)
                return null;

            return _DiscountPromotionsRepository.GetById(promoId);
        }


        /// <summary>
        /// Insert a CustomReward
        /// </summary>
        /// <param name="promo">CustomRewards</param>
        public void InsertDiscountPromotions(DiscountPromotions promo)
        {
            if (promo == null)
                throw new ArgumentNullException("DiscountPromotions");

            _DiscountPromotionsRepository.Insert(promo);
        }
        public void UpdateDiscountPromotions(DiscountPromotions promo)
        {
            if (promo == null)
                throw new ArgumentNullException("DiscountPromotions");

            _DiscountPromotionsRepository.Update(promo);
        }


        /// <summary>
        /// Delete a CustomReward
        /// </summary>
        /// <param name="promo">CustomRewards</param>
        public void DeleteDiscountPromotions(DiscountPromotions promo)
        {
            if (promo == null)
                throw new ArgumentNullException("DiscountPromotions");

            _DiscountPromotionsRepository.Delete(promo);
        }
    }
}
