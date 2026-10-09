using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Vendors;
using Nop.Core.Infrastructure;
using Nop.Core.Rss;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Controllers
{
    [AutoValidateAntiforgeryToken]
    public partial class CatalogController : BasePublicController
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly IAclService _aclService;
        private readonly ICatalogModelFactory _catalogModelFactory;
        private readonly ICategoryService _categoryService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IManufacturerService _manufacturerService;
        private readonly INopUrlHelper _nopUrlHelper;
        private readonly IPermissionService _permissionService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IProductService _productService;
        private readonly IProductTagService _productTagService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IVendorService _vendorService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly MediaSettings _mediaSettings;
        private readonly VendorSettings _vendorSettings;
        private readonly Services.Logging.ILogger _logger;
        private readonly AzurePictureService _azurePictureService;
        private readonly INopFileProvider _fileProvider;
        private readonly IPictureService _pictureService;
        private readonly IDownloadService _downloadService;


        #endregion

        #region Ctor

        public CatalogController(CatalogSettings catalogSettings,
            IAclService aclService,
            ICatalogModelFactory catalogModelFactory,
            ICategoryService categoryService,
            ICustomerActivityService customerActivityService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            INopUrlHelper nopUrlHelper,
            IPermissionService permissionService,
            IProductModelFactory productModelFactory,
            IProductService productService,
            IProductTagService productTagService,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IUrlRecordService urlRecordService,
            IVendorService vendorService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            VendorSettings vendorSettings,
            Services.Logging.ILogger logger,
            AzurePictureService azurePictureService,
            INopFileProvider fileProvider,
            IPictureService pictureService,
            IDownloadService downloadService)
        {
            _catalogSettings = catalogSettings;
            _aclService = aclService;
            _catalogModelFactory = catalogModelFactory;
            _categoryService = categoryService;
            _customerActivityService = customerActivityService;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _manufacturerService = manufacturerService;
            _nopUrlHelper = nopUrlHelper;
            _permissionService = permissionService;
            _productModelFactory = productModelFactory;
            _productService = productService;
            _productTagService = productTagService;
            _storeContext = storeContext;
            _storeMappingService = storeMappingService;
            _urlRecordService = urlRecordService;
            _vendorService = vendorService;
            _webHelper = webHelper;
            _workContext = workContext;
            _mediaSettings = mediaSettings;
            _vendorSettings = vendorSettings;
            _logger = logger;
            _azurePictureService = azurePictureService;
            _fileProvider = fileProvider;
            _pictureService = pictureService;
            _downloadService = downloadService;
        }

        #endregion

        #region Categories

        public virtual async Task<IActionResult> Category(int categoryId, CatalogProductsCommand command)
        {
            var category = await _categoryService.GetCategoryByIdAsync(categoryId);

            if (!await CheckCategoryAvailabilityAsync(category))
                return InvokeHttp404();

            var store = await _storeContext.GetCurrentStoreAsync();

            //'Continue shopping' URL
            await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(),
                NopCustomerDefaults.LastContinueShoppingPageAttribute,
                _webHelper.GetThisPageUrl(false),
                store.Id);

            //display "edit" (manage) link
            if (await _permissionService.AuthorizeAsync(StandardPermissionProvider.AccessAdminPanel) && await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCategories))
                DisplayEditLink(Url.Action("Edit", "Category", new { id = category.Id, area = AreaNames.Admin }));

            //activity log
            await _customerActivityService.InsertActivityAsync("PublicStore.ViewCategory",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.PublicStore.ViewCategory"), category.Name), category);

            //model
            var model = await _catalogModelFactory.PrepareCategoryModelAsync(category, command);

            //template
            var templateViewPath = await _catalogModelFactory.PrepareCategoryTemplateViewPathAsync(category.CategoryTemplateId);
            return View(templateViewPath, model);
        }

        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> GetCategoryProducts(int categoryId, CatalogProductsCommand command)
        {
            var category = await _categoryService.GetCategoryByIdAsync(categoryId);

            if (!await CheckCategoryAvailabilityAsync(category))
                return NotFound();

            var model = await _catalogModelFactory.PrepareCategoryProductsModelAsync(category, command);

            return PartialView("_ProductsInGridOrLines", model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> GetCatalogRoot()
        {
            var model = await _catalogModelFactory.PrepareRootCategoriesAsync();

            return Json(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> GetCatalogSubCategories(int id)
        {
            var model = await _catalogModelFactory.PrepareSubCategoriesAsync(id);

            return Json(model);
        }

        #endregion

        #region Manufacturers

        public virtual async Task<IActionResult> Manufacturer(int manufacturerId, CatalogProductsCommand command)
        {
            var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(manufacturerId);

            if (!await CheckManufacturerAvailabilityAsync(manufacturer))
                return InvokeHttp404();

            var store = await _storeContext.GetCurrentStoreAsync();

            //'Continue shopping' URL
            await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(),
                NopCustomerDefaults.LastContinueShoppingPageAttribute,
                _webHelper.GetThisPageUrl(false),
                store.Id);

            //display "edit" (manage) link
            if (await _permissionService.AuthorizeAsync(StandardPermissionProvider.AccessAdminPanel) && await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageManufacturers))
                DisplayEditLink(Url.Action("Edit", "Manufacturer", new { id = manufacturer.Id, area = AreaNames.Admin }));

            //activity log
            await _customerActivityService.InsertActivityAsync("PublicStore.ViewManufacturer",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.PublicStore.ViewManufacturer"), manufacturer.Name), manufacturer);

            //model
            var model = await _catalogModelFactory.PrepareManufacturerModelAsync(manufacturer, command);

            //template
            var templateViewPath = await _catalogModelFactory.PrepareManufacturerTemplateViewPathAsync(manufacturer.ManufacturerTemplateId);

            return View(templateViewPath, model);
        }

        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> GetManufacturerProducts(int manufacturerId, CatalogProductsCommand command)
        {
            var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(manufacturerId);

            if (!await CheckManufacturerAvailabilityAsync(manufacturer))
                return NotFound();

            var model = await _catalogModelFactory.PrepareManufacturerProductsModelAsync(manufacturer, command);

            return PartialView("_ProductsInGridOrLines", model);
        }

        public virtual async Task<IActionResult> ManufacturerAll()
        {
            var model = await _catalogModelFactory.PrepareManufacturerAllModelsAsync();

            return View(model);
        }

        #endregion

        #region Vendors

        public virtual async Task<IActionResult> Vendor(int vendorId, CatalogProductsCommand command)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(vendorId);

            if (!await CheckVendorAvailabilityAsync(vendor))
                return InvokeHttp404();

            var store = await _storeContext.GetCurrentStoreAsync();

            //'Continue shopping' URL
            await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(),
                NopCustomerDefaults.LastContinueShoppingPageAttribute,
                _webHelper.GetThisPageUrl(false),
                store.Id);

            //display "edit" (manage) link
            if (await _permissionService.AuthorizeAsync(StandardPermissionProvider.AccessAdminPanel) && await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageVendors))
                DisplayEditLink(Url.Action("Edit", "Vendor", new { id = vendor.Id, area = AreaNames.Admin }));

            //model
            var model = await _catalogModelFactory.PrepareVendorModelAsync(vendor, command);

            return View(model);
        }

        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> GetVendorProducts(int vendorId, CatalogProductsCommand command)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(vendorId);

            if (!await CheckVendorAvailabilityAsync(vendor))
                return NotFound();

            var model = await _catalogModelFactory.PrepareVendorProductsModelAsync(vendor, command);

            return PartialView("_ProductsInGridOrLines", model);
        }

        public virtual async Task<IActionResult> VendorAll()
        {
            //we don't allow viewing of vendors if "vendors" block is hidden
            if (_vendorSettings.VendorsBlockItemsToDisplay == 0)
                return RedirectToRoute("Homepage");

            var model = await _catalogModelFactory.PrepareVendorAllModelsAsync();
            return View(model);
        }

        #endregion

        #region Product tags

        public virtual async Task<IActionResult> ProductsByTag(int productTagId, CatalogProductsCommand command)
        {
            var productTag = await _productTagService.GetProductTagByIdAsync(productTagId);
            if (productTag == null)
                return InvokeHttp404();

            var model = await _catalogModelFactory.PrepareProductsByTagModelAsync(productTag, command);

            return View(model);
        }

        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> GetTagProducts(int tagId, CatalogProductsCommand command)
        {
            var productTag = await _productTagService.GetProductTagByIdAsync(tagId);
            if (productTag == null)
                return NotFound();

            var model = await _catalogModelFactory.PrepareTagProductsModelAsync(productTag, command);

            return PartialView("_ProductsInGridOrLines", model);
        }

        public virtual async Task<IActionResult> ProductTagsAll()
        {
            var model = await _catalogModelFactory.PreparePopularProductTagsModelAsync();

            return View(model);
        }

        #endregion

        #region New (recently added) products page

        public virtual async Task<IActionResult> NewProducts(CatalogProductsCommand command)
        {
            if (!_catalogSettings.NewProductsEnabled)
                return InvokeHttp404();

            var model = new NewProductsModel
            {
                CatalogProductsModel = await _catalogModelFactory.PrepareNewProductsModelAsync(command)
            };

            return View(model);
        }

        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> GetNewProducts(CatalogProductsCommand command)
        {
            if (!_catalogSettings.NewProductsEnabled)
                return NotFound();

            var model = await _catalogModelFactory.PrepareNewProductsModelAsync(command);

            return PartialView("_ProductsInGridOrLines", model);
        }

        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> NewProductsRss()
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var feed = new RssFeed(
                $"{await _localizationService.GetLocalizedAsync(store, x => x.Name)}: New products",
                "Information about products",
                new Uri(_webHelper.GetStoreLocation()),
                DateTime.UtcNow);

            if (!_catalogSettings.NewProductsEnabled)
                return new RssActionResult(feed, _webHelper.GetThisPageUrl(false));

            var items = new List<RssItem>();

            var storeId = store.Id;
            var products = await _productService.GetProductsMarkedAsNewAsync(storeId: storeId);

            foreach (var product in products)
            {
                var seName = await _urlRecordService.GetSeNameAsync(product);
                var productUrl = await _nopUrlHelper.RouteGenericUrlAsync<Product>(new { SeName = seName }, _webHelper.GetCurrentRequestProtocol());
                var productName = await _localizationService.GetLocalizedAsync(product, x => x.Name);
                var productDescription = await _localizationService.GetLocalizedAsync(product, x => x.ShortDescription);
                var item = new RssItem(productName, productDescription, new Uri(productUrl), $"urn:store:{store.Id}:newProducts:product:{product.Id}", product.CreatedOnUtc);
                items.Add(item);
                //uncomment below if you want to add RSS enclosure for pictures
                //var picture = _pictureService.GetPicturesByProductId(product.Id, 1).FirstOrDefault();
                //if (picture != null)
                //{
                //    var imageUrl = _pictureService.GetPictureUrl(picture, _mediaSettings.ProductDetailsPictureSize);
                //    item.ElementExtensions.Add(new XElement("enclosure", new XAttribute("type", "image/jpeg"), new XAttribute("url", imageUrl), new XAttribute("length", picture.PictureBinary.Length)));
                //}

            }
            feed.Items = items;
            return new RssActionResult(feed, _webHelper.GetThisPageUrl(false));
        }

        #endregion

        #region Searching

        public virtual async Task<IActionResult> Search(SearchModel model, CatalogProductsCommand command)
        {
            var store = await _storeContext.GetCurrentStoreAsync();

            //'Continue shopping' URL
            await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(),
                NopCustomerDefaults.LastContinueShoppingPageAttribute,
                _webHelper.GetThisPageUrl(true),
                store.Id);

            if (model == null)
                model = new SearchModel();

            model = await _catalogModelFactory.PrepareSearchModelAsync(model, command);

            return View(model);
        }

        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> SearchTermAutoComplete(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Content("");

            term = term.Trim();

            if (string.IsNullOrWhiteSpace(term) || term.Length < _catalogSettings.ProductSearchTermMinimumLength)
                return Content("");

            //products
            var productNumber = _catalogSettings.ProductSearchAutoCompleteNumberOfProducts > 0 ?
                _catalogSettings.ProductSearchAutoCompleteNumberOfProducts : 10;
            var store = await _storeContext.GetCurrentStoreAsync();
            var products = await _productService.SearchProductsAsync(0,
                storeId: store.Id,
                keywords: term,
                languageId: (await _workContext.GetWorkingLanguageAsync()).Id,
                visibleIndividuallyOnly: true,
                pageSize: productNumber);

            var showLinkToResultSearch = _catalogSettings.ShowLinkToAllResultInSearchAutoComplete && (products.TotalCount > productNumber);

            var models = (await _productModelFactory.PrepareProductOverviewModelsAsync(products, false, _catalogSettings.ShowProductImagesInSearchAutoComplete, _mediaSettings.AutoCompleteSearchThumbPictureSize)).ToList();
            var result = (from p in models
                          select new
                          {
                              label = p.Name,
                              producturl = p.SeName.Contains("ProductDetailPage")?p.SeName:Url.RouteUrl<Product>(new { SeName = p.SeName }),
                              productpictureurl = p.PictureModels.FirstOrDefault()?.ImageUrl.Replace("_50","_100"),
                              showlinktoresultsearch = showLinkToResultSearch
                          })
                .ToList();
            return Json(result);
        }

        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> SearchProducts(SearchModel searchModel, CatalogProductsCommand command)
        {
            if (searchModel == null)
                searchModel = new SearchModel();

            var model = await _catalogModelFactory.PrepareSearchProductsModelAsync(searchModel, command);

            return PartialView("_ProductsInGridOrLines", model);
        }

        #endregion

        #region Utilities

        private async Task<bool> CheckCategoryAvailabilityAsync(Category category)
        {
            if (category is null)
                return false;

            var isAvailable = true;

            if (category.Deleted)
                isAvailable = false;

            var notAvailable =
                //published?
                !category.Published ||
                //ACL (access control list) 
                !await _aclService.AuthorizeAsync(category) ||
                //Store mapping
                !await _storeMappingService.AuthorizeAsync(category);
            //Check whether the current user has a "Manage categories" permission (usually a store owner)
            //We should allows him (her) to use "Preview" functionality
            var hasAdminAccess = await _permissionService.AuthorizeAsync(StandardPermissionProvider.AccessAdminPanel) && await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCategories);
            if (notAvailable && !hasAdminAccess)
                isAvailable = false;

            return isAvailable;
        }

        private async Task<bool> CheckManufacturerAvailabilityAsync(Manufacturer manufacturer)
        {
            var isAvailable = true;

            if (manufacturer == null || manufacturer.Deleted)
                isAvailable = false;

            var notAvailable =
                //published?
                !manufacturer.Published ||
                //ACL (access control list) 
                !await _aclService.AuthorizeAsync(manufacturer) ||
                //Store mapping
                !await _storeMappingService.AuthorizeAsync(manufacturer);
            //Check whether the current user has a "Manage categories" permission (usually a store owner)
            //We should allows him (her) to use "Preview" functionality
            var hasAdminAccess = await _permissionService.AuthorizeAsync(StandardPermissionProvider.AccessAdminPanel) && await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageManufacturers);
            if (notAvailable && !hasAdminAccess)
                isAvailable = false;

            return isAvailable;
        }

        private Task<bool> CheckVendorAvailabilityAsync(Vendor vendor)
        {
            var isAvailable = true;

            if (vendor == null || vendor.Deleted || !vendor.Active)
                isAvailable = false;

            return Task.FromResult(isAvailable);
        }


        public async Task<IActionResult> UploadCategory()
        {
            // Your action logic here
            var webRootPath = _fileProvider.WebRootPath.ToString();
            var directoryPath = @"\images\New_Category_Images\";
            var moveDirPath = @"\images\Uploaded_Images\";
            var fullDir = webRootPath + directoryPath;
            var removephoto = webRootPath + moveDirPath;

            try
            {
               


                // get all products
                //var products = (await _productService.SearchProductsAsync(showHidden: true)).Select(x => new { x.Id, x.Name, x.ManufacturerPartNumber, x.AdminComment, x.Sku }).ToListAsync();
                var categories = (await _categoryService.GetAllCategoriesAsync(showHidden: true)).Select(x => new { x.Id, x.Name, x.MetaKeywords, x.ParentCategoryId }).Where(d => d.ParentCategoryId != 0).ToListAsync();
               // var categories = (await _categoryService.GetAllCategoriesAsync(showHidden: true)).Select(x => new { x.Id, x.Name, x.MetaKeywords, x.ParentCategoryId }).Where(d => d.MetaKeywords == "Instruments >> Orthodontics").ToListAsync();
                var totallength = 0;
                foreach (var category in categories.Result.ToList())
                {
                                    if (category.Name == null)
                        continue;


                    var FullCategory = category.MetaKeywords.Replace(">>", "").Replace(" ", "").Replace(")", "").Replace("(", "").Replace("/", "").Trim();

                  

                    var files = _fileProvider.GetFiles(fullDir);

                    foreach (var file in files)
                    {
                        var isexistpicture = _categoryService.GetCategoryByIdAsync(category.Id).Result;
                        int i = 0;
                        var newFile = await ConvertFilePathToIFormFileAsync(file);
                        var filename = _fileProvider.GetFileNameWithoutExtension(newFile.FileName);
                        filename= filename.Replace(">>", "").Replace(" ", "").Replace(")", "").Replace("(", "").Replace("/","").Trim();
                        if (FullCategory== filename)
                        {
                           // var newFile = await ConvertFilePathToIFormFileAsync(file);
                            if (newFile == null)
                                continue;
                            if (i == 0)  //if (!newFile.FileName.Contains("@"))
                            {
                                var mpn = _fileProvider.GetFileNameWithoutExtension(newFile.FileName);


                                if (isexistpicture.PictureId != 1)
                                {
                                    i++;                                    
                                    var pictureobj = _pictureService.GetPictureByIdAsync(isexistpicture.PictureId).Result;
                                    //delete picture from blob if it exists in blob
                                     _azurePictureService.callprotected(pictureobj);
                                    await _pictureService.DeletePictureAsync(pictureobj);
                                   
                                }
                            }

                            var contentType = newFile.ContentType.ToLowerInvariant();

                            // get picture binary
                            var pictureBinary = await _downloadService.GetDownloadBitsAsync(newFile);

                            //insert picture
                            var picture = await _pictureService.InsertPictureAsync(pictureBinary, contentType, null);

                            await _pictureService.SetSeoFilenameAsync(picture.Id, await _pictureService.GetPictureSeNameAsync(category.Name));
                            isexistpicture.PictureId = picture.Id;
                            await _categoryService.UpdateCategoryAsync(isexistpicture);

                            // move file to upload folder 
                            //_fileProvider.FileMove(file, string.Concat(webRootPath, moveDirPath, newFile.FileName));
                             totallength++;
                        }

                    }






                    //// var files = _fileProvider.GetFiles(fullDir, string.Format("{0}*", "PENTOSMAX"));


                    ////string files = Path.Combine(fullDir, FullCategory);

                    //// Check if the file exists
                    ////if (_fileProvider.FileExists(files))
                    ////s{
                    //if (files.Length > 0)
                    //{
                    //    var isexistpicture = _categoryService.GetCategoryByIdAsync(category.Id).Result;
                    //    int i = 0;
                    //    foreach (var file in files)
                    //    {

                            
                    //    }
                    //}



                }
                System.IO.DirectoryInfo di = new DirectoryInfo(fullDir);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

                return Json(totallength);
            }
            catch (Exception ex)
            {
                 await _logger.InsertLogAsync(LogLevel.Error, "Failed to upload files from folder", ex.Message);
                return Json(0);
            }
        }



        public string GetContentTypeFromExtension(string fileExtension)
        {
            // Map file extensions to content types
            switch (fileExtension.ToLower())
            {
                case ".txt":
                    return "text/plain";
                case ".pdf":
                    return "application/pdf";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".webp":
                    return "image/webp";
                // Add more cases as needed
                default:
                    return "image/jpeg";
            }
        }

        public async Task<IFormFile> ConvertFilePathToIFormFileAsync(string filePath)
        {
            try
            {
                byte[] fileBytes = await _fileProvider.ReadAllBytesAsync(filePath);
                var memoryStream = new MemoryStream(fileBytes);

                // get file extension
                var extension = _fileProvider.GetFileExtension(filePath);

                // get content type from extension
                var contentType = GetContentTypeFromExtension(extension);

                // You can customize the 'FileName' parameter if needed
                IFormFile file = new FormFile(memoryStream, 0, memoryStream.Length, null, Path.GetFileName(filePath))
                {
                    Headers = new HeaderDictionary(),
                    ContentType = contentType
                };

                return file;
            }
            catch (Exception ex)
            {
                await _logger.InsertLogAsync(LogLevel.Error, "Failed to convert file to Iform file", ex.Message);
                return null;
            }

        }
        #endregion
    }
}