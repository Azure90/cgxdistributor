using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.CustomRewardPoint;

namespace Nop.Services.CustomRewardPoints
{
    public partial interface ICustomerRewardPointService
    {
        IList<RewardsPointCustom> GetAllCustomReward();
        RewardsPointCustom GetCustomRewardById(int promoId);
        void InsertCustomReward(RewardsPointCustom promo);
        void UpdateCustomReward(RewardsPointCustom promo);
        void DeleteCustomReward(RewardsPointCustom promoId);
    }
}
