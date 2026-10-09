using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.DistributorCourses
{
    public partial interface IDistributorCoursesService
    {
        IList<Nop.Core.Domain.DistributorCourses.DistributorCourses> GetAllDistributorCourses();
        Nop.Core.Domain.DistributorCourses.DistributorCourses GetDistributorCoursesById(int promoId);
        void InsertDistributorCourses(Nop.Core.Domain.DistributorCourses.DistributorCourses promo);
        void UpdateDistributorCourses(Nop.Core.Domain.DistributorCourses.DistributorCourses promo);
        void DeleteDistributorCourses(Nop.Core.Domain.DistributorCourses.DistributorCourses promo);
    }
}
