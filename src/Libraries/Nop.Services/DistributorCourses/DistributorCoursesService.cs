using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Data;

namespace Nop.Services.DistributorCourses
{
    public partial class DistributorCoursesService:IDistributorCoursesService
    {
        private readonly IRepository<Nop.Core.Domain.DistributorCourses.DistributorCourses> _DistributorCoursesRepository;


        public DistributorCoursesService(IRepository<Nop.Core.Domain.DistributorCourses.DistributorCourses> DistributorCoursesRepository)
        {
            _DistributorCoursesRepository = DistributorCoursesRepository;
        }
        public IList<Nop.Core.Domain.DistributorCourses.DistributorCourses> GetAllDistributorCourses()
        {
            var query = _DistributorCoursesRepository.Table;

            //if (IsPriceRange)
            //    query = query.Where(x => x.IsPriceRange == IsPriceRange);

            //if (IsShoppingItem)
            //    query = query.Where(x => x.IsShoppingItem == IsShoppingItem);

            //query = query.OrderBy(v => v.Id).ThenBy(v => v.Id);
            //var promo = new PagedList<DistributorCourses>(query.ToList(), pageIndex, pageSize);
            return query.ToList();
        }

        /// <summary>
        /// Gets a CustomReward with id identifier 
        /// </summary>
        /// <param name="promoId">Identifier</param>
        /// <returns>
        /// Returns a CustomReward
        /// </returns>
        public Nop.Core.Domain.DistributorCourses.DistributorCourses GetDistributorCoursesById(int promoId)
        {
            if (promoId == 0)
                return null;

            return _DistributorCoursesRepository.GetById(promoId);
        }


        /// <summary>
        /// Insert a CustomReward
        /// </summary>
        /// <param name="promo">DistributorCourses</param>
        public void InsertDistributorCourses(Nop.Core.Domain.DistributorCourses.DistributorCourses promo)
        {
            if (promo == null)
                throw new ArgumentNullException("DistributorCourses");

            _DistributorCoursesRepository.Insert(promo);
        }


        /// <summary>
        /// Update a CustomReward
        /// </summary>
        /// <param name="promo">DistributorCourses</param>
        public void UpdateDistributorCourses(Nop.Core.Domain.DistributorCourses.DistributorCourses promo)
        {
            if (promo == null)
                throw new ArgumentNullException("DistributorCourses");

            _DistributorCoursesRepository.Update(promo);
        }


        /// <summary>
        /// Delete a DistributorCourses
        /// </summary>
        /// <param name="promo">DistributorCourses</param>
        public void DeleteDistributorCourses(Nop.Core.Domain.DistributorCourses.DistributorCourses promo)
        {
            if (promo == null)
                throw new ArgumentNullException("DistributorCourses");




            _DistributorCoursesRepository.Delete(promo);
        }
    }
}
