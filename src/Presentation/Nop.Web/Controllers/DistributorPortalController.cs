
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Catalog;
using Nop.Web.Models.DistributorPortal;
using Dapper;
using Nop.Services.Seo;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Nop.Services.Customers;
using System;
using System.Collections.Generic;
using Nop.Web.Factories;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

using System.Runtime.InteropServices;
using Nop.Core.Domain.Catalog;
using DocumentFormat.OpenXml.EMMA;
using SkiaSharp;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Models.Media;
using Nop.Services.DistributorCourses;
using DocumentFormat.OpenXml.Wordprocessing;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Common;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Common;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Localization;
using Nop.Services.Html;
using Nop.Services.Messages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Nop.Web.Models.Customer;
using Nop.Web.Infrastructure;
using Nop.Core.Infrastructure;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using PdfiumViewer;
using Nop.Web.Models.Catalog;
using Nop.Services.Configuration;
using Nop.Web.Models.KnowledgeBaseArticle;
namespace Nop.Web.Controllers
{
    public partial class DistributorPortalController : BasePublicController
    {
        #region Fields

        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;
        private readonly Nop.Web.Areas.Admin.Factories.IProductModelFactory _productModelFactory1;
        private readonly IDistributorCoursesService _DistributorCoursesService;
        private readonly ILocalizationService _localizationService;
        private readonly CaptchaSettings _captchaSettings;
        private readonly CommonSettings _commonSettings;
        private readonly ICommonModelFactory _commonModelFactory;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ILanguageService _languageService;
        private readonly IHtmlFormatter _htmlFormatter;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly ICountryService _countryService;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly ITokenizer _tokenizer;
        private readonly ISettingService _settingService;
        private readonly IProductsWithBPCodeService _productsWithBPCodeService;
        private readonly ILogger _logger;

        #endregion


        #region Ctor

        public DistributorPortalController(IProductService productService,
            IUrlRecordService urlRecordService,
            IProductModelFactory productModelFactory,
            ICountryService countryService,
            IWorkContext workContext,
             ICustomerService customerService,
            IDistributorCoursesService DistributorCoursesService,
            Nop.Web.Areas.Admin.Factories.IProductModelFactory productModelFactory1,
            CaptchaSettings captchaSettings,
            CommonSettings commonSettings,
            ICommonModelFactory commonModelFactory,
            ICurrencyService currencyService,
            ICustomerActivityService customerActivityService,
             IHtmlFormatter htmlFormatter,
            ILocalizationService localizationService,
             IWorkflowMessageService workflowMessageService,
              IMessageTemplateService messageTemplateService,
           ITokenizer tokenizer,
            ICategoryService categoryService,
            ISettingService settingService,
            IProductsWithBPCodeService productsWithBPCodeService,
            ILogger logger)
        {
            _productService = productService;
            _categoryService = categoryService;
            _countryService = countryService;
            _urlRecordService = urlRecordService;
            _workContext = workContext;
            _customerService = customerService;
            _productModelFactory = productModelFactory;
            _productModelFactory1 = productModelFactory1;
            _DistributorCoursesService = DistributorCoursesService;
            _captchaSettings = captchaSettings;
            _commonSettings = commonSettings;
            _commonModelFactory = commonModelFactory;
            _currencyService = currencyService;
            _customerActivityService = customerActivityService;
            _htmlFormatter = htmlFormatter;
            _localizationService = localizationService;
            _workflowMessageService = workflowMessageService;
            _messageTemplateService = messageTemplateService;
            _tokenizer = tokenizer;
            _settingService = settingService;
            _productsWithBPCodeService = productsWithBPCodeService;
            _logger = logger;
        }

        #endregion

        public async Task<IActionResult> DistributorPortal(string? modeldata)
        {
            DistributorPortalModel distributorPortalModel = new DistributorPortalModel();
            distributorPortalModel.SidebarMenu.Add("Products");
            distributorPortalModel.SidebarMenu.Add("Non-Sterile Sample");
            // var products = (await _productService.SearchProductsAsync(showHidden: true)).Select(x => new { x.Id, x.Name, x.ManufacturerPartNumber, x.AdminComment, x.Sku, x.Published, x.VisibleIndividually, x.ShortDescription }).Where(s => s.Published == true && s.VisibleIndividually == true).ToListAsync();          
            int page = 1;
            int pageSize = 8;

            //var productTasks = products.Result.Select(async data =>
            //{
            //    var productDetail = await _productService.GetProductByIdAsync(data.Id);
            //    var productSeName = await _urlRecordService.GetSeNameAsync(productDetail);
            //    var picturedetail = await _productModelFactory.getpictures(productDetail, false);

            //    return new ProductDetails
            //    {
            //        Id = data.Id,
            //        Name = data.Name,
            //        Sku = data.Sku,
            //        UrlSlug = productSeName,
            //        ShortDescription = data.ShortDescription,
            //        Image = picturedetail.PictureModels.FirstOrDefault()?.FullSizeImageUrl
            //    };
            //});

            //var productList = await Task.WhenAll(productTasks);
            //distributorPortalModel.ProductList.AddRange(productList);



            // var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var SocialMedia = Convert.ToBoolean(_settingService.GetSetting("SocialActive/Inactive").Value.ToLower());
            var Training = Convert.ToBoolean(_settingService.GetSetting("TrainingActive/Inactive").Value.ToLower());
            var productstab = Convert.ToBoolean(_settingService.GetSetting("productsactive/inactive").Value.ToLower());
            var NonSterile = Convert.ToBoolean(_settingService.GetSetting("Non-SterileActive/Inactive").Value.ToLower());

            var aa = new List<string>();
            if (productstab == true)
            {
                distributorPortalModel.SidebarSubMenuName.Add(new SidebarSubMenu
                {
                    Titlename = "Products",
                });
            }
            if (NonSterile == true)
            {
                distributorPortalModel.SidebarSubMenuName.Add(new SidebarSubMenu
                {
                    Titlename = "Non-Sterile Sample",
                });
            }
            if (SocialMedia == true)
            {
                distributorPortalModel.SidebarSubMenuName.Add(new SidebarSubMenu
                {
                    Titlename = "Social Media",
                });
            }
            if (Training == true)
            {
                distributorPortalModel.SidebarSubMenuName.Add(new SidebarSubMenu
                {
                    Titlename = "Training Courses",
                });
            }
            var currcustomer = _workContext.GetCurrentCustomerAsync().Result;
            var currRoles = _customerService.GetCustomerRolesAsync(currcustomer).Result.Where(d => d.Name == "CAD B2C"
                                       || d.Name == "USD B2C"
                                       || d.Name == "USD B2B INTL"
                                       || d.Name == "CAD B2C HS"
                                       || d.Name == "USD B2B ST"
                                       || d.Name == "USD B2B DS"
                                       || d.Name == "CAD B2B ST"
                                       || d.Name == "CAD B2B DS"
                                       ).ToList();

            // var productonbasisofcategory= await _categoryService.
            // var allCategory = _categoryService.GetAllCategoriesAsync().Result;
            var connection = DataSettingsManager.LoadSettings().ConnectionString;
            var conn = new SqlConnection(connection);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();
            using (IDbConnection conn1 = new SqlConnection(connection))
            {

                DynamicParameters ds = new DynamicParameters();
                ds.Add("@customerroleid", currRoles?.FirstOrDefault()?.Id);
                var result = conn1.Query<SubMenuList>("GetDistributorportalCategory", param: ds, commandType: CommandType.StoredProcedure);


                foreach (var item in result)
                {
                    string parentsidebarbarmenuname;

                    if (item.distrisku == "socialmedia")
                    {
                        parentsidebarbarmenuname = "Social Media";
                    }
                    else if (item.IsSample == false)
                    {
                        parentsidebarbarmenuname = "Products";
                    }
                    else
                    {
                        parentsidebarbarmenuname = "Non-Sterile Sample";
                    }

                    distributorPortalModel.SubMenuListName.Add(new SubMenuList
                    {
                        parentsidebarbarmenuname = parentsidebarbarmenuname,
                        Childid = item.Childid,
                        Childcategory = item.Childcategory,
                        ParentSubid = item.ParentSubid,
                        Subcategory = item.Subcategory,
                        Parentid = item.Parentid,
                        Parentcategory = item.Parentcategory
                    });
                }

                var trainingcourseslist = _DistributorCoursesService.GetAllDistributorCourses().Where(x => x.MainCategory == "Training Courses").ToList();
                foreach (var i in trainingcourseslist)
                {
                    distributorPortalModel.SubMenuListName.Add(new SubMenuList
                    {
                        parentsidebarbarmenuname = "Training Courses",
                        Subcategory = i.Category,
                        Parentcategory = i.SubCategory
                    });
                }

                //distributorPortalModel.SubMenuListName.Add(new SubMenuList
                //{
                //    parentsidebarbarmenuname = "Training Courses",

                //    Parentcategory = "Online Course(Firmwater)",
                //    Parentid = 1

                //});

                //distributorPortalModel.SubMenuListName.Add(new SubMenuList
                //{
                //    parentsidebarbarmenuname = "Training Courses",

                //    Parentcategory = "Virtual Training Request",
                //    Parentid = 2
                //});

                //distributorPortalModel.SubMenuListName.Add(new SubMenuList
                //{
                //    parentsidebarbarmenuname = "Social Media",

                //    Parentcategory = "Image",
                //    Parentid = 3
                //});

                //distributorPortalModel.SubMenuListName.Add(new SubMenuList
                //{
                //    parentsidebarbarmenuname = "Social Media",

                //    Parentcategory = "Video",
                //    Parentid = 4
                //});


                var product = result.Where(x => x.IsSample == false && x.Parentcategory== "Regenerative Biomaterials").ToList();
                var productTasks = product.Select(async data =>
                {
                    var productDetail = await _productService.GetProductByIdAsync(data.Id);
                    var productSeName = await _urlRecordService.GetSeNameAsync(productDetail);
                    var picturedetail = await _productModelFactory.getpictures(productDetail, false);

                    return new ProductDetails
                    {
                        Id = data.Id,
                        Name = data.Name,
                        Sku = data.Sku,
                        UrlSlug = productSeName,
                        ShortDescription = data.ShortDescription,
                        Image = picturedetail.DefaultPictureModel.ImageUrl,
                        IsSample = data.IsSample,
                        ModuleName = data.IsSample == false ? "Products" : "Non-Sterile Sample",
                        socialmediashow = data.distrisku == "socialmedia" ? "show" : "Notshow",
                    };
                });

                var products = await Task.WhenAll(productTasks); // Wait for all tasks to complete

                #region product with bp code filter tpss sh

                // get products from the tbale
                var blockedIds = (await _productsWithBPCodeService
                    .GetBlockedProductIdsForCustomerAsync(currcustomer?.BPCode))
                    .ToHashSet();

                // remove the product from list
                products = products.Where(p => !blockedIds.Contains(p.Id)).ToArray();

                #endregion

                var productList = products
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .ToList();

                distributorPortalModel.ProductList.AddRange(productList);
                var totalItems = distributorPortalModel.ProductList.Count;


                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize); // make sure you set totalCount properly
                var pagedProducts = distributorPortalModel.ProductList
               .Skip((page - 1) * pageSize)
               .Take(pageSize)
               .ToList();

                distributorPortalModel.ProductList = pagedProducts;
                distributorPortalModel.CurrentPage = page;
                distributorPortalModel.TotalPages = totalPages;
                distributorPortalModel.PageSize = pageSize;
                var distrributorcourses = _DistributorCoursesService.GetAllDistributorCourses();
                if (distrributorcourses.Count > 0)
                {
                    var course = _DistributorCoursesService.GetAllDistributorCourses()?.Where(x => x.SubCategory == "Virtual Training Request"&& currRoles.Any(y => x.CustomerRole.Split(",").Any(z=>z.Trim() == y.Name))).FirstOrDefault();

                    distributorPortalModel.virtualtraining = course?.Description ?? "No training available";
                }
            }
            if (modeldata == "data")
            {
                return PartialView("_PartialProductDistributor", distributorPortalModel);
            }
            return View(distributorPortalModel);
        }

        public async Task<ActionResult> DistributorPortalforproducts(string value, string name, int page = 1, int pageSize = 8)
        {
            List<ProductDetails> allProducts = new List<ProductDetails>();
            List<ProductOverviewModel> allProducts1 = new List<ProductOverviewModel>();
            var currcustomer = _workContext.GetCurrentCustomerAsync().Result;
            var currRoles = _customerService.GetCustomerRolesAsync(currcustomer).Result.Where(d => d.Name == "CAD B2C"
                                       || d.Name == "USD B2C"
                                       || d.Name == "USD B2B INTL"
                                       || d.Name == "CAD B2C HS"
                                       || d.Name == "USD B2B ST"
                                       || d.Name == "USD B2B DS"
                                       || d.Name == "CAD B2B ST"
                                       || d.Name == "CAD B2B DS"
                                       ).ToList();

            // get products from the table tpss sh
            var blockedIds = (await _productsWithBPCodeService
                .GetBlockedProductIdsForCustomerAsync(currcustomer?.BPCode))
                .ToHashSet();

            if (name == "Training Courses"/*|| name == "Social Media"*/ && value != null)
            {
                var valuecourse = value.Split(",");

                var courseName = "";
                List<ProductDetails> allProductse = new List<ProductDetails>();

                foreach (var j in valuecourse)
                {
                    if (j != "0")
                    {
                        courseName = j;
                        var allRoles = _DistributorCoursesService.GetAllDistributorCourses()
                            .Where(x => x.Category == courseName)
                            .ToList();

                        List<Nop.Core.Domain.DistributorCourses.DistributorCourses> courses = new List<Nop.Core.Domain.DistributorCourses.DistributorCourses>();

                        foreach (var i in allRoles)
                        {
                            if (i.CustomerRole.Contains(","))
                            {
                                var splittedroles = i.CustomerRole.Split(",");
                                if (splittedroles.Any(y => currRoles.Any(x => x.Name == y)))
                                {
                                    courses.Add(i);
                                }
                            }
                            else
                            {
                                if (currRoles.Any(x => x.Name == i.CustomerRole))
                                {
                                    courses.Add(i);
                                }
                            }
                        }

                        var productTasks = courses.Select(async data =>
                        {
                            return new ProductDetails
                            {
                                Id = data.Id,
                                Name = data.Name,
                                Sku = name,
                                ShortDescription = data.Description,
                                Image = data.ImageLink,
                                UrlSlug = data.ButtonLink,
                                ModuleName = name,
                                SubModuleName = courseName
                            };
                        });

                        var productList = await Task.WhenAll(productTasks);
                        allProductse.AddRange(productList);
                    }
                }

                #region product with bp code filter tpss sh

                // remove the product from list
                allProducts = allProducts.Where(p => !blockedIds.Contains(p.Id)).ToList();

                #endregion

                // Paging logic AFTER collecting all products
                var totalItems = allProductse.Count;
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var pagedProducts = allProductse
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Declare the model OUTSIDE the loop
                var distributorPortalModel = new DistributorPortalModel
                {
                    ProductList = pagedProducts,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize
                };

                return PartialView("_PartialProductDistributor", distributorPortalModel);

            }
            else if (value != null)
            {
                var datasettings = DataSettingsManager.LoadSettings();
                var connstring = datasettings.ConnectionString;
                var dataproduct = value.Split(",");


                foreach (var item in dataproduct)
                {
                    //if (item!= "")
                    {
                        using (var conn = new SqlConnection(connstring))
                        {
                            conn.Open();
                            var query = "";
                            if (name == "Products")
                            {
                                query = "select p.Id,p.Name,p.Sku, iif((select Count(sku) from DistributorCourses where sku=p.Sku)>0,'show','notshow') as socialmediashow, p.ShortDescription,p.IsSample from Product p  join Product_Category_Mapping pm on p.Id=pm.ProductId left join AclRecord acl on acl.EntityId=p.Id and acl.CustomerRoleId=" + currRoles.FirstOrDefault().Id + " where pm.CategoryId=" + item + " and p.VisibleIndividually=1   and p.Published=1 and Deleted=0 and p.IsSample=0 and acl.CustomerRoleId=" + currRoles.FirstOrDefault().Id + "";
                            }
                            else if (name == "Non-Sterile Sample")
                            {
                                query = "select p.Id,p.Name,p.Sku, iif((select Count(sku) from DistributorCourses where sku=p.Sku)>0,'show','notshow') as socialmediashow, p.ShortDescription,p.IsSample from Product p  join Product_Category_Mapping pm on p.Id=pm.ProductId left join AclRecord acl on acl.EntityId=p.Id and acl.CustomerRoleId=" + currRoles.FirstOrDefault().Id + " where pm.CategoryId=" + item + " and p.VisibleIndividually=1 and p.Published=1 and Deleted=0 and p.IsSample=1 and acl.CustomerRoleId=" + currRoles.FirstOrDefault().Id + "";
                            }
                            else if (name == "Social Media")
                            {
                                query = "select p.Id,p.Name,p.Sku, iif((select Count(sku) from DistributorCourses where sku=p.Sku)>0,'show','notshow') as socialmediashow, p.ShortDescription,p.IsSample from Product p  join Product_Category_Mapping pm on p.Id=pm.ProductId left join AclRecord acl on acl.EntityId=p.Id and acl.CustomerRoleId=" + currRoles.FirstOrDefault().Id + " where pm.CategoryId=" + item + " and p.VisibleIndividually=1 and p.Published=1 and Deleted=0 and acl.CustomerRoleId=" + currRoles.FirstOrDefault().Id + " and p.sku in( select Sku from DistributorCourses) ";
                            }
                            var result = conn.Query<ProductDetails>(query);

                            var productTasks = result.Select(async data =>
                            {
                                var productDetail = await _productService.GetProductByIdAsync(data.Id);
                                var productSeName = await _urlRecordService.GetSeNameAsync(productDetail);
                                var picturedetail = await _productModelFactory.getpictures(productDetail, false);

                                return new ProductDetails
                                {
                                    Id = data.Id,
                                    Name = data.Name,
                                    socialmediashow = data.socialmediashow,
                                    Sku = data.Sku,
                                    UrlSlug = productSeName,
                                    ShortDescription = data.ShortDescription,
                                    Image = picturedetail.DefaultPictureModel.ImageUrl,
                                    IsSample = data.IsSample,
                                    ModuleName = name
                                };

                            });

                            var productList = await Task.WhenAll(productTasks);
                            allProducts.AddRange(productList);
                        }
                    }
                }

                #region product with bp code filter tpss sh

                // remove the product from list
                allProducts = allProducts.Where(p => !blockedIds.Contains(p.Id)).ToList();

                #endregion

                // Calculate paging
                var totalItems = allProducts.Count;
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var pagedProducts = allProducts
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Assign to model
                var distributorPortalModel = new DistributorPortalModel
                {
                    ProductList = pagedProducts,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize
                };

                return PartialView("_PartialProductDistributor", distributorPortalModel);
                
            }
            else
            {
                var connection = DataSettingsManager.LoadSettings().ConnectionString;
                var conn = new SqlConnection(connection);
                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();
                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@customerroleid", currRoles.FirstOrDefault().Id);
                    var result = conn1.Query<SubMenuList>("GetDistributorportalCategory", param: ds, commandType: CommandType.StoredProcedure);
                    var product = result.Where(x => x.IsSample == false && x.Parentcategory == "Regenerative Biomaterials").ToList();

                    var productTasks = product.Select(async data =>
                    {
                        var productDetail = await _productService.GetProductByIdAsync(data.Id);
                        var productSeName = await _urlRecordService.GetSeNameAsync(productDetail);
                        var picturedetail = await _productModelFactory.getpictures(productDetail, false);

                        return new ProductDetails
                        {
                            Id = data.Id,
                            Name = data.Name,
                            Sku = data.Sku,
                            UrlSlug = productSeName,
                            ShortDescription = data.ShortDescription,
                            Image = picturedetail.DefaultPictureModel.ImageUrl,
                            IsSample = data.IsSample,
                            ModuleName = data.IsSample == false ? "Products" : "Non-Sterile Sample",
                            socialmediashow = data.distrisku == "socialmedia" ? "show" : "Notshow",
                        };
                    });

                    var products = await Task.WhenAll(productTasks); // Wait for all tasks to complete

                    #region product with bp code filter tpss sh

                    // remove the product from list
                    products = products.Where(p => !blockedIds.Contains(p.Id)).ToArray();

                    #endregion

                    var productList = products
                        .GroupBy(p => p.Id)
                        .Select(g => g.First())
                        .ToList();
                    allProducts.AddRange(productList);

                    var totalItems = allProducts.Count;
                    //var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                    var totalPages = (int)Math.Ceiling((double)totalItems / pageSize); // make sure you set totalCount properly
                    var pagedProducts = allProducts
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    // Assign to model
                    var distributorPortalModel = new DistributorPortalModel
                    {
                        ProductList = pagedProducts,
                        CurrentPage = page,
                        TotalPages = totalPages,
                        PageSize = pageSize
                    };

                    return PartialView("_PartialProductDistributor", distributorPortalModel);
                }
                return PartialView("_PartialProductDistributor");
            }
        }



        public class tesmodel
        {
            public int productid { get; set; }
            public bool Isample { get; set; }
            public string ModuleName { get; set; }
            public string Name { get; set; }

        }



        public async Task<ActionResult> ProductDetailPage(int pid, bool s, string a, string sm)
        {
            ProductDetails details = new ProductDetails();
            if (a == "Training Courses")
            {
                var courses = _DistributorCoursesService.GetAllDistributorCourses().Where(x => x.Id == pid).FirstOrDefault();
                details.Id = courses.Id;
                details.Image = courses.ImageLink;
                details.Name = courses.Name;
                details.ShortDescription = courses.Description;
                details.Sku = a;
                details.UrlSlug = courses.ButtonLink;

                details.IsSample = s;


            }
            else
            {

                PriceChange priceChange = new PriceChange();
            var documents = priceChange.getdocumentdetail(pid);                                                                
            details.DocumentModels = documents;
            var productDetail = await _productService.GetProductByIdAsync(pid);
            var productSeName = await _urlRecordService.GetSeNameAsync(productDetail);
            var picturedetail = await _productModelFactory.getpictures(productDetail, false);
            details.Id = productDetail.Id;
            details.Image = picturedetail.DefaultPictureModel.ImageUrl;
            details.Name = productDetail.Name;
            details.ShortDescription = productDetail.ShortDescription;
            details.Sku = productDetail.Sku;
            ViewBag.griddata = Gettheproductdetail(pid);
            details.PictureModels = picturedetail.PictureModels;
            details.VideoModels = picturedetail.VideoModels;
            details.IsSample = s;
            details.ProductBreadcrumb = await _productModelFactory.getBreadcrumb(productDetail);


            var currcustomer = _workContext.GetCurrentCustomerAsync().Result;
            var currRoles = _customerService.GetCustomerRolesAsync(currcustomer).Result.Where(d => d.Name == "CAD B2C"
                                       || d.Name == "USD B2C"
                                       || d.Name == "USD B2B INTL"
                                       || d.Name == "CAD B2C HS"
                                       || d.Name == "USD B2B ST"
                                       || d.Name == "USD B2B DS"
                                       || d.Name == "CAD B2B ST"
                                       || d.Name == "CAD B2B DS"
                                       ).Select(r => r.Name).ToList();

            //var allRoles = _DistributorCoursesService.GetAllDistributorCourses().Where(x => x.Sku == productDetail.Sku  ).ToList();
            var allRoles = _DistributorCoursesService.GetAllDistributorCourses()
                            .Where(x => x.Sku == productDetail.Sku
                            && x.CustomerRole != null
                              && x.CustomerRole.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Any(d => currRoles.Contains(d.Trim()))).ToList();


            var productTasks = allRoles.Select(async data =>
                    {

                        return new ProductDetails
                        {
                            Id = data.Id,
                            Name = data.Name,
                            Sku = productDetail.Sku,
                            ShortDescription = data.Description,
                            Image = data.ImageLink,
                            UrlSlug = data.ButtonLink,
                            // ModuleName = name,
                            //SubModuleName = courseName


                        };
                    });



            //List<Nop.Core.Domain.DistributorCourses.DistributorCourses> courses = new List<Nop.Core.Domain.DistributorCourses.DistributorCourses>();
            //foreach (var i in allRoles)
            //{
            //    if (i.CustomerRole.Contains(","))
            //    {
            //        var splittedroles = i.CustomerRole.Split(",");
            //        if (splittedroles.Any(y => currRoles.Any(x => x.Name == y)))
            //        {
            //            courses.Add(i);
            //        }
            //    }
            //    else
            //    {
            //        if (currRoles.Any(x => x.Name == i.CustomerRole))
            //        {
            //            courses.Add(i);
            //        }
            //    }
            //}
            //var productTasks = courses.Select(async data =>
            //{

            //    return new ProductDetails
            //    {
            //        Id = data.Id,
            //        Name = data.Name,
            //        Sku = productDetail.Sku,
            //        ShortDescription = data.Description,
            //        Image = data.ImageLink,
            //        UrlSlug = data.ButtonLink,
            //       // ModuleName = name,
            //        //SubModuleName = courseName


            //    };
            //});


            var products = await Task.WhenAll(productTasks);
            details.ProductList.AddRange(products);








             }
            return View(details);
        }


        public class CustomerInfo
        {
            public string AttributesXml { get; set; }
            public string AdminComment { get; set; }
            public string Sku { get; set; }
            public string Name { get; set; }
        }
        public List<CustomerInfo> Gettheproductdetail(int productid)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var currcustomer = _workContext.GetCurrentCustomerAsync().Result.BPCode;
            var customerInfo = new List<CustomerInfo>();
            if (_workContext.OriginalCustomerIfImpersonated != null)
            {
                currcustomer = _workContext.OriginalCustomerIfImpersonated.BPCode;
            }
            //var currcustomer = "C80040";           
            using (var conn = new SqlConnection(connstring))
            {

                {
                    conn.Open();
                    var query = "select AdminComment,Sku,Name  from Product where sku in( Select sku from ProductAttributeCombination where productid=" + productid + ")";
                    var result = conn.Query<CustomerInfo>(query);
                    var searchModel = new ProductAttributeCombinationSearchModel();
                    var modifiedSearchModel = new ProductAttributeCombinationSearchModel
                    {
                        Page = searchModel.Page,
                        // Other properties if needed
                        PageSize = int.MaxValue // or int.MaxValue to fetch all
                    };


                    var product = _productService.GetProductByIdAsync(productid).Result;

                    var model = _productModelFactory1.PrepareProductAttributeCombinationListModelAsync(modifiedSearchModel, product);
                    customerInfo = result.ToList();
                    customerInfo = customerInfo.Select(ci =>
                    {

                        return new CustomerInfo
                        {
                            AttributesXml = model.Result.Data.Where(x => x.Sku == ci.Sku).Select(y => y.AttributesXml).FirstOrDefault(),
                            Sku = ci.Sku,
                            AdminComment = ci.AdminComment,
                            Name = ci.Name
                        };
                    }).ToList();

                    return (customerInfo.ToList());

                }
            }
        }
        //contact us page
        //available even when a store is closed
        [HttpGet("download-image")]
        public async Task<IActionResult> DownloadImage(List<string> url)
        {
            if (url == null || !url.Any())
                return BadRequest("URL is required.");

            using var httpClient = new HttpClient();
            if (url.Count() > 1)
            {
                try
                {
                    using var memoryStream = new MemoryStream();
                    using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var i in url)
                        {
                            var response = await httpClient.GetAsync(i);
                            if (!response.IsSuccessStatusCode)
                                return BadRequest($"Failed to download image from {i}");

                            var content = await response.Content.ReadAsByteArrayAsync();
                            var fileName = Path.GetFileName(new Uri(i).AbsolutePath);

                            var zipEntry = zip.CreateEntry(fileName, CompressionLevel.Fastest);
                            using var entryStream = zipEntry.Open();
                            await entryStream.WriteAsync(content, 0, content.Length);
                        }
                    }

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return File(memoryStream.ToArray(), "application/zip", "images.zip");
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error downloading images: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    foreach (var i in url)
                    {
                        var response = await httpClient.GetAsync(i);
                        if (!response.IsSuccessStatusCode)
                            return BadRequest("Failed to download image from source.");

                        var content = await response.Content.ReadAsByteArrayAsync();
                        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                        var fileName = Path.GetFileName(new Uri(i).AbsolutePath);
                        return File(content, contentType, fileName);
                    }


                }
                catch (Exception ex)
                {
                    return BadRequest($"Error downloading image: {ex.Message}");
                }

            }
            return BadRequest($"Error downloading image");
        }


        [AllowAnonymous]
        public virtual async Task<IActionResult> ContactUs()
        {
            var model = new ContactUsModel();
            //model = await _commonModelFactory.PrepareContactUsModelAsync(model, false);
            model.countrylist.Add(new SelectListItem { Text = "Select Country", Value = "0" });

            foreach (var c in await _countryService.GetAllCountriesAsync(showHidden: true))
                model.countrylist.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            return PartialView("~/Views/DistributorPortal/ContactUs.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAllVideos([FromBody] List<VideoDownloadModel> videos)
        {
            try
            {
                if (videos == null || !videos.Any())
                    return BadRequest("No videos were provided.");

                using var httpClient = new HttpClient();
                using var memoryStream = new MemoryStream();

                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var video in videos)
                    {
                        if (string.IsNullOrWhiteSpace(video.Url))
                            continue;

                        try
                        {
                            using var response = await httpClient.GetAsync(video.Url);

                            if (!response.IsSuccessStatusCode)
                                continue;

                            var fileBytes = await response.Content.ReadAsByteArrayAsync();

                            var fileName = string.IsNullOrWhiteSpace(video.FileName)
                                ? $"{Guid.NewGuid()}.mp4"
                                : $"{video.FileName}.mp4";

                            var entry = archive.CreateEntry(fileName, CompressionLevel.NoCompression);

                            await using var entryStream = entry.Open();
                            await entryStream.WriteAsync(fileBytes);
                        }
                        catch (Exception ex)
                        {
                            await _logger.ErrorAsync($"Failed to download video " +
                                $"'{video.FileName}'. Url: {video.Url}", ex);

                            // Continue downloading the remaining videos
                        }
                    }
                }

                memoryStream.Position = 0;

                return File(memoryStream.ToArray(), "application/zip",
                    $"Videos_{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Error occurred while creating video zip.", ex);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Unable to download videos. Please try again later.");
            }
        }


        ////available even when a store is closed
        //[CheckAccessClosedStore(ignore: true)]
        ////available even when navigation is not allowed
        //[CheckAccessPublicStore(ignore: true)]
        //public virtual async Task<IActionResult> ContactUsdetail(string FullName,string Email,string Subject,string country,string Enquiry)
        //{


        //    var subjectReplaced = "";
        //    var bodyReplaced = "";
        //    var customer = _workContext.GetCurrentCustomerAsync().Result;
        //    var template = _workflowMessageService.DistributorContactus(customer,1,FullName, Email, Subject, country, Enquiry).Result;
        //    subjectReplaced = _tokenizer.Replace(template[0].MessageTemplate.Subject, template[0].Tokens, false);
        //    bodyReplaced = _tokenizer.Replace(template[0].MessageTemplate.Body, template[0].Tokens, true);

        //    var template1 = _workflowMessageService.CustomerResponse(customer,1,FullName).Result;
        //    subjectReplaced = _tokenizer.Replace(template1[0].MessageTemplate.Subject, template1[0].Tokens, false);
        //    bodyReplaced = _tokenizer.Replace(template1[0].MessageTemplate.Body, template1[0].Tokens, true);

        //    // model = await _commonModelFactory.PrepareContactUsModelAsync(model, true);
        //    ContactUsModel contactUs = new ContactUsModel();
        //    //if (ModelState.IsValid)
        //    {
        //        var subject = Subject;
        //        var body = Enquiry;

        //        await _workflowMessageService.SendContactUsMessageAsync((await _workContext.GetWorkingLanguageAsync()).Id,
        //           Email.Trim(), FullName, subjectReplaced, bodyReplaced);

        //        await _workflowMessageService.SendContactUsMessageAsync((await _workContext.GetWorkingLanguageAsync()).Id,
        //          Email.Trim(), FullName, subjectReplaced, bodyReplaced);
        //        contactUs.SuccessfullySent = true;
        //        //model.Result = await _localizationService.GetResourceAsync("ContactUs.YourEnquiryHasBeenSent");

        //        //activity log
        //        await _customerActivityService.InsertActivityAsync("PublicStore.ContactUs",
        //            await _localizationService.GetResourceAsync("ActivityLog.PublicStore.ContactUs"));

        //        return Json(new { Result = true });
        //    }

        //    return Json(new { Result = false });
        //}
    }
}