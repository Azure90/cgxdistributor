using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.CustomRewardPoint;
using Nop.Core.Domain.DiscountPromotions;

namespace Nop.Services.DiscountPromotion
{
    public partial interface IDiscountPromotionService
    {
        IList<DiscountPromotions> GetAllDiscountPromotions();
        DiscountPromotions GetDiscountPromotionsById(int promoId);
        void InsertDiscountPromotions(DiscountPromotions promo);
        void UpdateDiscountPromotions(DiscountPromotions promo);
        void DeleteDiscountPromotions(DiscountPromotions promoId);
    }
}
