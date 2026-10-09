using System.Collections.Generic;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.CustomPage
{
    public partial record SubTitleModel : BaseNopEntityModel
    {
        public int ID { get; set; }
        public string LanguageId { get; set; }
        public string SubTittle { get; set; }
        public List<SubTitleModel> ModelList { get; set; }
        public SubTitleModel()
        {
            ModelList = new List<SubTitleModel>();
        }
    }
}
