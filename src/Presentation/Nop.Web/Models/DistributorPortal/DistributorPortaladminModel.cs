using System.Collections.Generic;
using Nop.Core;
using Nop.Web.Framework.Models;
using Nop.Web.Models.KnowledgeBaseArticle;

namespace Nop.Web.Models.DistributorPortal
{
    public class DistributorPortaladminModel : BaseEntity
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
        public List<DistributorPortaladminModel> ModelList { get; set; }
        public DistributorPortaladminModel()
        {
            ModelList = new List<DistributorPortaladminModel>();
        }
    }
}
