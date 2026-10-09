using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.DistributorCourses
{
    public partial class  DistributorCourses : BaseEntity
    {

        public string MainCategory { get; set; }
        public string SubCategory { get; set; }
        public string Sku { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageLink { get; set; }
        public string CustomerRole { get; set; }
        public string ButtonLink { get; set; }
       
    }
}
