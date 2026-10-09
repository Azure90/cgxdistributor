using System.Collections.Generic;
using Nop.Core.Domain.CustomPages;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.KnowledgeBase
{
    public partial record KnowledgeBaseModel : BaseNopModel
    {
        public int Id { get; set; }
        public string Pagename { get; set; }
        public string PagenameDescription { get; set; }
        public string PagenameBanner { get; set; }
        public List<mainsubTitle> Titlename { get; set; }

        public KnowledgeBaseModel()
        {
            ContentModelList = new List<TblCustomPageContent>();
            CustomModelList = new List<TblCustomPages>();
            Titlename = new List<mainsubTitle>();
            TittleModelList = new List<TittleModelList>();
            ModelList = new List<KnowledgeBaseModel>();
        }
        public List<TblCustomPageContent> ContentModelList { get; set; }
        public List<TblCustomPages> CustomModelList { get; set; }
        public List<TblCustomPages> MainMenuList { get; set; }
        public List<TblCustomPageContent> SubMenuList { get; set; }
        public List<TittleModelList> TittleModelList { get; set; }
        public List<KnowledgeBaseModel> ModelList { get; set; }
    }

    public class mainsubTitle
    {
        public string Titlename { get; set; }
        public int SubTitleID { get; set; }
        public int MainPageID { get; set; }
    }

    public class TittleModelList
    {
        public string Titlename { get; set; }
        public int TittleID { get; set; }
    }
}
