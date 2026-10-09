using System.Collections.Generic;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Nop.Core.Domain.CustomPages;
using Nop.Web.Framework.Models;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;
using static Nop.Web.Infrastructure.PriceChange;

namespace Nop.Web.Models.DistributorPortal
{
    public partial record DistributorPortalModel : BaseNopModel
    {
        public IList<string> SidebarMenu { get; set; }

        public IList<SidebarSubMenu> SidebarSubMenuName { get; set; }
        public IList<SubMenuList> SubMenuListName { get; set; }
        public List<ProductDetails> ProductList { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public string ModuleName { get; set; }
        public string virtualtraining { get; set; }

        public DistributorPortalModel()
        {
            SidebarSubMenuName = new List<SidebarSubMenu>();
            SubMenuListName = new List<SubMenuList>();
            ProductList = new List<ProductDetails>();
            SidebarMenu = new List<string>();
            CurrentPage = 1;
            PageSize = 10;
        }

    }

    public class SidebarSubMenu
    {
        public string Titlename { get; set; }
    }
    public class SubMenuList
    {
        public string parentsidebarbarmenuname { get; set; }
        public int Childid { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public string distrisku { get; set; }
        public bool IsSample { get; set; }

        public string ShortDescription { get; set; }
        public string Childcategory { get; set; }
        public int ParentSubid { get; set; }
        public string Subcategory { get; set; }
        public int Parentid { get; set; }
        public string Parentcategory { get; set; }
    }
    public class ProductDetails
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string socialmediashow { get; set; }
        public string Sku { get; set; }
        public string UrlSlug { get; set; }
        public string ShortDescription { get; set; }
        public string Image { get; set; }
        public bool IsSample { get; set; }
        public string ModuleName { get; set; }
        public string SubModuleName { get; set; }
        public IList<PictureModel> PictureModels { get; set; }
        public PictureModel DefaultPictureModel { get; set; }
        //videos
        public IList<VideoModel> VideoModels { get; set; }
        public string DocumentModels { get; set; }
        public List<ProductDetails> ProductList { get; set; }
        public Nop.Web.Models.Catalog.ProductDetailsModel.ProductBreadcrumbModel ProductBreadcrumb { get; set; }
        public ProductDetails()
        {

            DefaultPictureModel = new PictureModel();
            ProductBreadcrumb = new Nop.Web.Models.Catalog.ProductDetailsModel.ProductBreadcrumbModel();
            PictureModels = new List<PictureModel>();
            VideoModels = new List<VideoModel>();
            ProductList = new List<ProductDetails>();

        }


       
    }
}