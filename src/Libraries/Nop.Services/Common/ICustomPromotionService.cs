using Nop.Core;
using Nop.Core.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.Common
{
    public partial interface ICustomPromotionService
    {
        IList<CustomPromotions> GetAllPromotions();

        CustomPromotions GetPromotionById(int promoId);

        void InsertPromotion(CustomPromotions promo);

        void UpdatePromotion(CustomPromotions promo);

        void DeletePromotion(CustomPromotions promo);
    }
}
