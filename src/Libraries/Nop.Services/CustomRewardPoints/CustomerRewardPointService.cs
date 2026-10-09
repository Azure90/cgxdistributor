using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.CustomRewardPoint;
using Nop.Data;

namespace Nop.Services.CustomRewardPoints
{
    public partial class CustomerRewardPointService : ICustomerRewardPointService
    {

        private readonly IRepository<RewardsPointCustom> _CustomRewardRepository;


        public CustomerRewardPointService(IRepository<RewardsPointCustom> CustomRewardRepository)
        {
            _CustomRewardRepository = CustomRewardRepository;
        }
        public IList<RewardsPointCustom> GetAllCustomReward()
        {
            var query = _CustomRewardRepository.Table;

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
        public RewardsPointCustom GetCustomRewardById(int promoId)
        {
            if (promoId == 0)
                return null;

            return _CustomRewardRepository.GetById(promoId);
        }


        /// <summary>
        /// Insert a CustomReward
        /// </summary>
        /// <param name="promo">CustomRewards</param>
        public void InsertCustomReward(RewardsPointCustom promo)
        {
            if (promo == null)
                throw new ArgumentNullException("CustomRewards");

            _CustomRewardRepository.Insert(promo);
        }


        /// <summary>
        /// Update a CustomReward
        /// </summary>
        /// <param name="promo">CustomRewards</param>
        public void UpdateCustomReward(RewardsPointCustom promo)
        {
            if (promo == null)
                throw new ArgumentNullException("CustomRewards");

            _CustomRewardRepository.Update(promo);
        }


        /// <summary>
        /// Delete a CustomReward
        /// </summary>
        /// <param name="promo">CustomRewards</param>
        public void DeleteCustomReward(RewardsPointCustom promo)
        {
            if (promo == null)
                throw new ArgumentNullException("CustomRewards");
            
            _CustomRewardRepository.Delete(promo);
        }

    }
}
