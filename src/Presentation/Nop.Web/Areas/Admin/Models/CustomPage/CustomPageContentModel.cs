using System.Collections.Generic;
using Nop.Core.Domain.CustomPages;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.CustomPage
{
    public partial record CustomPageContentModel : BaseNopEntityModel
    {

        #region Ctor

        public CustomPageContentModel()
        {
            TitleList = new List<TblSubTitle>();
            CustomPageList = new List<TblCustomPages>();
            ModelList = new List<CustomPageContentModel>();
        }

        #endregion

        #region properties

        public int ID { get; set; }
        public string LanguageId { get; set; }
        public string Title { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public bool Active { get; set; }
        public int TblcustomPageId { get; set; }
        public int SubTitleID { get; set; }
        public string CustomPage { get; set; }
        public string SubTitle { get; set; }
        public int Display { get; set; }
        public IList<TblSubTitle> TitleList { get; set; }
        public IList<TblCustomPages> CustomPageList { get; set; }
        public List<CustomPageContentModel> ModelList { get; set; }

        #endregion
    }
}
