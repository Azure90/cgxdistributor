
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.CustomPage
{
    public partial record CustomPageModel : BaseNopEntityModel
    {
        public int ID { get; set; }
        public string LanguageId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public string Image { get; set; }
        public IFormFile BannerImage { get; set; }
        public List<CustomPageModel> ModelList { get; set; }
        public CustomPageModel()
        {
            ModelList = new List<CustomPageModel>();
        }
    }
}
