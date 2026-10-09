using System.Collections.Generic;
using Nop.Core;
using Microsoft.AspNetCore.Http;

namespace Nop.Web.Models.KnowledgeBaseArticle
{
    public class KnowledgeBaseArticleModel : BaseEntity
    {
        public string Category { get; set; }
        public string CategoryDescription { get; set; }
        public string DocEntry { get; set; }
        public string SubCategory { get; set; }
        public string SubCategoryDescription { get; set; }
        public string Brand { get; set; }
        public string Product { get; set; }
        public string ArticleTitle { get; set; }
        public string ArticleBody { get; set; }
        public string CustomerRoles { get; set; }
        public bool Active { get; set; }
        public string Pagename { get; set; }
        public string SubCategoryPagename { get; set; }
        public string ProductPagename { get; set; }
        public string PagenameDescription { get; set; }
        public string CategoryIcon { get; set; }
        public string ColorCode { get; set; }
        public string PagenameBanner { get; set; }
        public List<KnowledgeBaseArticleModel> ModelList { get; set; }
        public List<TittleModelList1> TittleModelList1 { get; set; }
        public List<mainsubTitle1> Titlename { get; set; }
        public List<mainmenu1> MainMenuList { get; set; }
        public List<string> Categorylist { get; set; }
        public IFormFile BannerImage { get; set; }
        public IFormFile KnowledgeBaseBannerImage { get; set; }
        public IFormFile DistributorRequestBannerImage { get; set; }
        public IFormFile DistributorRequestBannerusdImage { get; set; }
        public string KnowledgeBaseLabel { get; set; }
        public string DistributorRequestLabel { get; set; }
        public string DistributorRequesturl { get; set; }
        public string DistributorRequestusdLabel { get; set; }
        public string DistributorRequestusdurl { get; set; }
        public List<subcategorylist> subcategoryModelList { get; set; }
        public List<Productlist> ProductlistModel { get; set; }
        public KnowledgeBaseArticleModel()
        {            
            Titlename = new List<mainsubTitle1>();
            TittleModelList1 = new List<TittleModelList1>();
            ModelList = new List<KnowledgeBaseArticleModel>();
            subcategoryModelList = new List<subcategorylist>();
            ProductlistModel = new List<Productlist>();

        }
        
    }
    public class subcategorylist
    {
        public int id { get; set; }
        public string Category { get; set; }
        public string subcategory { get; set;}
        public string product { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }

    }
    public class Productlist
    {
        public string Category { get; set; }
        public string subcategory { get; set; }
        public string product { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }

    }
    public class mainsubTitle1
    {
        public string Titlename { get; set; }
        public int SubTitleID { get; set; }
        public int MainPageID { get; set; }
    }

    public class TittleModelList1
    {
        public string Titlename { get; set; }
        public int TittleID { get; set; }
    }
    public class mainmenu1
    {
        public string Tittle { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the image
        /// </summary>
        public string Image { get; set; }
    }
}
