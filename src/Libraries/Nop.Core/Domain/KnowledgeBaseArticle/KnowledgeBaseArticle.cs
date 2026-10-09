using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.KnowledgeBaseArticle
{
    public partial class KnowledgeBaseArticle : BaseEntity
    {
        public string Category { get; set; }
        public string CategoryDescription { get; set; }        
        public string SubCategory { get; set; }
        public string SubCategoryDescription { get; set; }
        public string Brand { get; set; }
        public string Product { get; set; }
        public string ArticleTitle { get; set; }
        public string ArticleBody { get; set; }
        public bool Active { get; set; }
         
        public string CustomerRoles { get; set; }   
        public string CategoryIcon { get; set; }
        public string ColorCode { get; set; }

    }
}
