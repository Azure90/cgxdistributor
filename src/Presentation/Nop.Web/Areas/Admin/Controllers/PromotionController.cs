using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.DataTables;
using Nop.Web.Framework.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;


namespace Nop.Admin.Controllers
{
    public class PromotionController : BaseAdminController
    {
        // GET: Promotion

        #region Fields

        private readonly IPermissionService _permissionService;
        private readonly ICustomPromotionService _customPromotionService;
        private readonly IProductService _productService;
        private readonly ILocalizationService _localizationService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IStaticCacheManager _cacheManager;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IBaseAdminModelFactory _baseAdminModelFactory;
        private readonly INotificationService _notificationService;
        private readonly ICategoryModelFactory _categoryModelFactory;
        private readonly ICustomerService _customerService;

        #endregion

        #region Ctor

        public PromotionController(IPermissionService permissionService, ICustomPromotionService customPromotionService,
            ILocalizationService localizationService, IProductService productService, ICategoryService categoryService,
            IStaticCacheManager cacheManager, IManufacturerService manufacturerService, ISettingService settingService,
            IBaseAdminModelFactory baseAdminModelFactory,
            INotificationService notificationService,
            ICategoryModelFactory categoryModelFactory,
            ICustomerService customerService,
            IStoreContext storeContext)
        {
            _permissionService = permissionService;
            _customPromotionService = customPromotionService;
            _notificationService = notificationService;
            _productService = productService;
            _localizationService = localizationService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _cacheManager = cacheManager;
            _settingService = settingService;
            _categoryModelFactory = categoryModelFactory;
            _storeContext = storeContext;
            _customerService = customerService;
            _baseAdminModelFactory = baseAdminModelFactory;
        }

        #endregion

        #region Methods

        public ActionResult Index()
        {
            return View("List");
        }

        public IActionResult List()
        {
            if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                return AccessDeniedView();

            ViewBag.AllPriceRanges = Convert.ToBoolean(_settingService.GetSetting("customerdiscountasperpricerange").Value);

            return View();
        }
        public static string ConvertDateToStringWithoutOffset(DateTime? Date)
        {
            string ReturnDate = "";
            string cDateFormatId = "";
            try
            {
                if (Date != null)
                {
                                        
                    {
                        ReturnDate = Convert.ToDateTime(Date).ToString("yyyy/mm/dd").Replace("-", "/").Replace("-", "/");
                    }
                }
            }
            catch
            {
                ReturnDate = Convert.ToDateTime(Date).ToString("MM/dd/yyyy").Replace("-", "/").Replace("-", "/");
            }
            return ReturnDate;
        }
        [HttpPost]
        public IActionResult PromotionList(/*DataSourceRequest command*/)
        {
            if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                return AccessDeniedView();

            var promos = _customPromotionService.GetAllPromotions();
            CustomPromotionModel obj = new CustomPromotionModel();

            //var gridModel = new DataTablesModel
            //{
            //    Data = promos.Select(x => new CustomPromotionModel
            //    {
            //        Id = x.Id,
            //        Price = x.Price,
            //        FromRange = x.FromRange,
            //        ToRange = x.ToRange,
            //        IsActive = x.IsActive,
            //        CatDesName = x.TypeId == 0 ? "-" : x.SelectedType == "Category" ? _categoryService.GetCategoryByIdAsync(x.TypeId).Result.Name
            //        : x.SelectedType == "Product" ? _productService.GetProductByIdAsync(x.TypeId).Result.Name : _manufacturerService.GetManufacturerByIdAsync(x.TypeId).Result.Name,
            //        DDlValue = string.IsNullOrEmpty(x.SelectedType) ? "-" : x.SelectedType,
            //        //ProductName = x.ProductId != 0 ? _productService.GetProductByIdAsync(x.ProductId).Result.Name : ""
            //        ProductName=x.ProductId.Split(",").Select(x=> _productService.GetProductByIdAsync(Convert.ToInt16(x)).Result.Name).ToString()
            //    }).ToList(),
            //};
            var gridModel = new DataTablesModel();
            foreach (var promotion in promos)
            {
                CustomPromotionModel data = new CustomPromotionModel();
                data.Id = promotion.Id;
                data.Price = promotion.Price;
                data.FromRange = promotion.FromRange;
                data.ToRange = promotion.ToRange;
                data.IsActive = promotion.IsActive;
                //data.PromotionDateFrom = promotion.PromotionDateFrom;
                data.PromotionDateFrom = promotion.PromotionDateFrom;                
                data.PromotionDateTo = promotion.PromotionDateFrom;
                data.SelectedOption= promotion.SelectedOption;
                data.CatDesName = promotion.TypeId == 0 ? "-" : promotion.SelectedType == "Category" ? _categoryService.GetCategoryByIdAsync(promotion.TypeId).Result.Name
                    : promotion.SelectedType == "Product" ? _productService.GetProductByIdAsync(promotion.TypeId).Result.Name : _manufacturerService.GetManufacturerByIdAsync(promotion.TypeId).Result.Name;
                //data.DDlValue = string.IsNullOrEmpty(promotion.SelectedType) ? "-" : promotion.SelectedType;
                data.DDlValue = promotion.SelectedType == "Designer" ? "Brand" : promotion.SelectedType;
                var name = promotion.ProductId.Split(",");
                foreach (var x in name)
                {
                    if (x != "0")
                    {
                        var nameofproduct = _productService.GetProductByIdAsync(Convert.ToInt16(x)).Result.Name;
                        data.ProductName = nameofproduct + "," + data.ProductName;
                    }
                }

                obj.AvailableList.Add(data);
                gridModel.Data = obj.AvailableList;
            }
            return Json(gridModel);
        }


        #region Create / Edit / Delete

        public IActionResult Create()
        {
            if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                return AccessDeniedView();

            //prepare model
            var model = new CustomPromotionModel();

            //categories
           // model.AvailableCategories.Add(new SelectListItem { Text = _localizationService.GetResourceAsync("Admin.Common.All").Result, Value = "0" });
            //var categories = SelectListHelper.GetCategoryList(_categoryService, _cacheManager, true);            
            var categories = _baseAdminModelFactory.PrepareCategoriesAsync(model.AvailableCategories);
            //foreach (var c in categories.re)
            //    model.AvailableCategories.Add(c);

            //manufacturers
            model.AvailableManufacturers.Add(new SelectListItem { Text = _localizationService.GetResourceAsync("Admin.Common.All").Result, Value = "0" });
            foreach (var m in _manufacturerService.GetAllManufacturersAsync(showHidden: true).Result)
                model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });

            var availableCustomerRoles = _customerService.GetAllCustomerRolesAsync().Result.Where(role => role.Name.Contains("usd", StringComparison.OrdinalIgnoreCase) || role.Name.Contains("cad", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var customerRole in availableCustomerRoles)
            {
                model.AvailableCustomerRoles.Add(new SelectListItem { Value = customerRole.Id.ToString(), Text = customerRole.Name });
            }

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public IActionResult Create(CustomPromotionModel model, bool continueEditing)
        {

            if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                return AccessDeniedView();

            var promo = _customPromotionService.GetAllPromotions()
                .Where(x => x.FromRange >= model.FromRange && x.ToRange <= model.ToRange && x.IsActive == true)
                .FirstOrDefault();


            if (string.IsNullOrEmpty(model.DDlValue))
                ModelState.AddModelError("", "Please select a type");

            if (ModelState.IsValid)
            {
                var typeId = 0;
                if (model.DDlValue == "Category")
                    typeId = model.CategoryId;
                else if (model.DDlValue == "Designer")
                    typeId = model.DesignerId;
                else
                    typeId = model.SingleProductId;


                var promotion = new CustomPromotions
                {
                    FromRange = model.FromRange,
                    ToRange = model.ToRange,
                    ProductId = model.ProductId,
                    Price = model.Price,
                    IsActive = model.IsActive,
                    SelectedType = model.DDlValue,
                    CustomerRoleid = string.Join(", ", model.CustomerRoleid.Select(x => x)),
                    TypeId = typeId,
                    PromotionDateFrom = model.PromotionDateFrom,
                    PromotionDateTo = model.PromotionDateTo,
                    SelectedOption=model.SelectedOption
                };

                _customPromotionService.InsertPromotion(promotion);
                _notificationService.SuccessNotification(_localizationService.GetResourceAsync("Admin.Custom.Promotion.Added").Result);

                if (continueEditing)
                    return RedirectToAction("Edit", new { id = promotion.Id });

                return RedirectToAction("List");
            }

            //categories
            model.AvailableCategories.Add(new SelectListItem { Text = _localizationService.GetResourceAsync("Admin.Common.All").Result, Value = "0" });
            var categories = _baseAdminModelFactory.PrepareCategoriesAsync(model.AvailableCategories);
            //foreach (var c in model.AvailableCategories)                
            //    model.AvailableCategories.Add(c);

            //manufacturers
            model.AvailableManufacturers.Add(new SelectListItem { Text = _localizationService.GetResourceAsync("Admin.Common.All").Result, Value = "0" });
            foreach (var m in _manufacturerService.GetAllManufacturersAsync(showHidden: true).Result)
                model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                return AccessDeniedView();

            var promotion = _customPromotionService.GetPromotionById(id);
            //var allid = promotion.ProductId.Split(",").Where(x=>x);
            //foreach(var i in allid)
            //{
            //    var product = _productService.GetProductByIdAsync(i).Result;
            //}


            if (promotion == null)
                //No promotion found with the specified id
                return RedirectToAction("List");

            var model = new CustomPromotionModel();

            model.Id = promotion.Id;
            model.FromRange = promotion.FromRange;
            model.ToRange = promotion.ToRange;
            model.ProductId = promotion.ProductId;
            model.Price = promotion.Price;
            model.PromotionDateFrom = promotion.PromotionDateFrom;
            model.PromotionDateTo = promotion.PromotionDateTo;
            model.SelectedOption = promotion.SelectedOption;
            var aa = promotion.CustomerRoleid.Split(',');
            foreach (var i in aa)
            {
                if (i != "")                
                model.CustomerRoleid.Add(Convert.ToInt32(i));
            }
            //model.ProductName = product.Name;
            var nameproduct = promotion.ProductId.Split(",");
            foreach (var x in nameproduct)
            {
                if (x != "0")
                {
                    var nameofproduct = _productService.GetProductByIdAsync(Convert.ToInt16(x)).Result.Name;
                    model.ProductName = nameofproduct + "$" + model.ProductName;
                }
            }
            model.IsActive = promotion.IsActive;
            model.DDlValue = promotion.SelectedType;


            if (promotion.SelectedType == "Category")
            {
                model.CategoryId = promotion.TypeId;
            }
            else if (promotion.SelectedType == "Designer")
            {
                model.DesignerId = promotion.TypeId;
            }
            else
            {
                model.SingleProductId = promotion.TypeId;
                var name = _productService.GetProductByIdAsync(promotion.TypeId).Result;
                if (name != null)
                    model.SingleProductName = name.Name;
            }


            //categories
            model.AvailableCategories.Add(new SelectListItem { Text = _localizationService.GetResourceAsync("Admin.Common.All").Result, Value = "0" });
            var categories = _baseAdminModelFactory.PrepareCategoriesAsync(model.AvailableCategories);
            //foreach (var c in model.AvailableCategories)
            //    model.AvailableCategories.Add(c);

            //manufacturers
            model.AvailableManufacturers.Add(new SelectListItem { Text = _localizationService.GetResourceAsync("Admin.Common.All").Result, Value = "0" });
            foreach (var m in _manufacturerService.GetAllManufacturersAsync(showHidden: true).Result)
                model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });

            var availableCustomerRoles = _customerService.GetAllCustomerRolesAsync().Result.Where(role => role.Name.Contains("usd", StringComparison.OrdinalIgnoreCase) || role.Name.Contains("cad", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var customerRole in availableCustomerRoles)
            {
                model.AvailableCustomerRoles.Add(new SelectListItem { Value = customerRole.Id.ToString(), Text = customerRole.Name });
            }

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public IActionResult Edit(CustomPromotionModel model, bool continueEditing)
        {
            if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                return AccessDeniedView();

            var promotion = _customPromotionService.GetPromotionById(model.Id);

            if (promotion == null)
                //No promotion found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {

                if (promotion.SelectedType == "Category")
                    promotion.TypeId = model.CategoryId;
                else if (promotion.SelectedType == "Designer")
                    promotion.TypeId = model.DesignerId;
                else
                    promotion.TypeId = model.SingleProductId;
                promotion.FromRange = model.FromRange;
                promotion.ToRange = model.ToRange;
                promotion.ProductId = model.ProductId;
                promotion.Price = model.Price;
                promotion.IsActive = model.IsActive;
                promotion.SelectedType = model.DDlValue;
                promotion.CustomerRoleid = string.Join(", ", model.CustomerRoleid.Select(x => x));
                promotion.PromotionDateFrom = model.PromotionDateFrom;
                promotion.PromotionDateTo = model.PromotionDateTo;
                promotion.SelectedOption = model.SelectedOption;
                _customPromotionService.UpdatePromotion(promotion);
                _notificationService.SuccessNotification(_localizationService.GetResourceAsync("Admin.Custom.Promotion.Updated").Result);

                if (continueEditing)
                    return RedirectToAction("Edit", new { id = promotion.Id });

                return RedirectToAction("List");
            }

            //categories
            model.AvailableCategories.Add(new SelectListItem { Text = _localizationService.GetResourceAsync("Admin.Common.All").Result, Value = "0" });
            var categories = _baseAdminModelFactory.PrepareCategoriesAsync(model.AvailableCategories);
            //foreach (var c in model.AvailableCategories)
            //    model.AvailableCategories.Add(c);

            //manufacturers
            model.AvailableManufacturers.Add(new SelectListItem { Text = _localizationService.GetResourceAsync("Admin.Common.All").Result, Value = "0" });
            foreach (var m in _manufacturerService.GetAllManufacturersAsync(showHidden: true).Result)
                model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                return AccessDeniedView();

            var promotion = _customPromotionService.GetPromotionById(id);

            if (promotion == null)
                //No promotion found with the specified id
                return RedirectToAction("List");

            _customPromotionService.DeletePromotion(promotion);

            _notificationService.SuccessNotification(_localizationService.GetResourceAsync("Admin.Custom.Promotion.Deleted").Result);

            return RedirectToAction("List");
        }

        [ValidateAntiForgeryToken]
        public ActionResult SetActiveInactivePriceRange(bool priceStatus)
        {
            _settingService.SetSetting("Customerdiscountasperpricerange", priceStatus);
            return Json(new { Status = priceStatus });
        }

        #endregion

        #region Product Popup


        public IActionResult ProductAddPopup(int? id, string btnId)
        {
            if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCategories).Result)
                return AccessDeniedView();

            //prepare model
            var model = _categoryModelFactory.PrepareAddProductToCategorySearchModelAsync(new AddProductToCategorySearchModel()).Result;
            if (btnId == "singleproduct")
            {
                model.singleproduct = "singleproduct";
            }
            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> ProductAddPopupList(AddProductToCategorySearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCategories))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await _categoryModelFactory.PrepareAddProductToCategoryListModelAsync(searchModel);

            return Json(model);
        }


        [HttpPost]
        [FormValueRequired("save")]
        public async Task<IActionResult> ProductAddPopup(AddProductToCategoryModel model, string btnId)
        {
            if (model.SelectedProductIds.Any())
            {

                //var product = _productService.GetProductById(model.ProductId);
                //if (product != null)
                {
                    // if (btnId == "btnRefreshSingleProducts")
                    if (btnId == "singleproduct")
                    {
                        var studentID = model.SelectedProductIds;
                        var studentNames = string.Join("$", model.SelectedProductIds.Select(x => _productService.GetProductByIdAsync(x).Result.Name));
                        ViewBag.productId = studentID[0].ToString();
                        ViewBag.productName = studentNames;
                        ViewBag.btnId = "singleproduct";
                    }
                    else
                    {
                        var studentID = string.Join(", ", model.SelectedProductIds.Select(x => x));
                        var studentNames = string.Join("$", model.SelectedProductIds.Select(x => _productService.GetProductByIdAsync(x).Result.Name));
                        ViewBag.productId = studentID;
                        ViewBag.productName = studentNames;
                    }
                    //else
                    //{

                    //    ViewBag.productId = product.Id;
                    //    //ViewBag.productIdInput = productIdInput;
                    //    //ViewBag.productNameInput = productNameInput;
                    //    //ViewBag.productId = product.Id;
                    //    //ViewBag.productName = product.Name;
                    //}
                }
            }

            ViewBag.RefreshPage = true;

            return View(new AddProductToCategorySearchModel());
        }

        #endregion

        #endregion

    }
}