using Microsoft.AspNetCore.Mvc;
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
using Nop.Services.CustomRewardPoints;
using Nop.Web.Areas.Admin.Models.CustomRewardPoint;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Irony.Parsing;
namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class CustomRewardPointsController : BaseAdminController
    {

        private readonly IPermissionService _permissionService;
        private readonly ICustomerRewardPointService _CustomerRewardPointService;
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



        #region Ctor

        public CustomRewardPointsController(IPermissionService permissionService, ICustomerRewardPointService CustomerRewardPointService,
            ILocalizationService localizationService, IProductService productService, ICategoryService categoryService,
            IStaticCacheManager cacheManager, IManufacturerService manufacturerService, ISettingService settingService,
            IBaseAdminModelFactory baseAdminModelFactory,
            INotificationService notificationService,
            ICategoryModelFactory categoryModelFactory,
            ICustomerService customerService,
            IStoreContext storeContext)
        {
            _permissionService = permissionService;
            _CustomerRewardPointService = CustomerRewardPointService;
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
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult CustomReward()
        {
            var getall = _CustomerRewardPointService.GetAllCustomReward();
            RewardsPointCustom obj = new RewardsPointCustom();

            foreach (var x in getall)
            {
                RewardsPointCustom rewardsPointCustom = new RewardsPointCustom();
                rewardsPointCustom.Id = x.Id;
                rewardsPointCustom.WeekDays = x.WeekDays;
                rewardsPointCustom.SelectedType = x.SelectedType;
                rewardsPointCustom.RewardPoints = x.RewardPoints;
                rewardsPointCustom.CategoryIds = x.CategoryIds;
                rewardsPointCustom.BrandIds = x.BrandIds;
                rewardsPointCustom.ProductIds = x.ProductIds;
                if (x.ProductIds != null)
                {


                    var name = x.ProductIds.Split(",");
                    foreach (var y in name)
                    {
                        if (y != "0")
                        {
                            var nameofproduct = _productService.GetProductByIdAsync(Convert.ToInt16(y)).Result.Name;
                            rewardsPointCustom.ProductName = nameofproduct + "," + rewardsPointCustom.ProductName;
                        }
                    }
                }
                rewardsPointCustom.Active = x.Active;
                rewardsPointCustom.CategoryName = _categoryService.GetCategoryByIdAsync(Convert.ToInt32(x.CategoryIds)).Result == null ? "0" : _categoryService.GetCategoryByIdAsync(Convert.ToInt32(x.CategoryIds)).Result.Name;
                rewardsPointCustom.BrandName = _manufacturerService.GetManufacturerByIdAsync(Convert.ToInt32(x.BrandIds)).Result == null ? "0" : _manufacturerService.GetManufacturerByIdAsync(Convert.ToInt32(x.BrandIds)).Result.Name;
                obj.ModelList.Add(rewardsPointCustom);
            }
            //obj.ModelList = getall.Select(x => new RewardsPointCustom
            //{
            //    Id = x.Id,
            //    WeekDays = x.WeekDays,
            //    SelectedType = x.SelectedType,
            //    RewardPoints = x.RewardPoints,
            //    CategoryIds = x.CategoryIds,
            //    BrandIds = x.BrandIds,
            //    ProductIds = x.ProductIds,

            //    Active = x.Active,
            //    CategoryName = _categoryService.GetCategoryByIdAsync(Convert.ToInt32(x.CategoryIds)).Result==null? "0": _categoryService.GetCategoryByIdAsync(Convert.ToInt32(x.CategoryIds)).Result.Name,
            //    BrandName = _manufacturerService.GetManufacturerByIdAsync(Convert.ToInt32(x.BrandIds)).Result == null ? "0" : _manufacturerService.GetManufacturerByIdAsync(Convert.ToInt32(x.BrandIds)).Result.Name
            //}).ToList();
            ViewBag.RewardPointActive = Convert.ToBoolean(_settingService.GetSetting("CustomRewardPointActive/Inactive").Value);
            return View(obj);
        }
        public IActionResult addoreditRewardponts(int id)
        {
            if (id > 0)
                    {
                if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                    return AccessDeniedView();

                var reward = _CustomerRewardPointService.GetCustomRewardById(id);
                //var allid = promotion.ProductId.Split(",").Where(x=>x);
                //foreach(var i in allid)
                //{
                //    var product = _productService.GetProductByIdAsync(i).Result;
                //}


                if (reward == null)
                    //No promotion found with the specified id
                    return RedirectToAction("CustomReward");
                    
                var model = new RewardsPointCustom();

                model.Id = reward.Id;
                model.WeekDays = reward.WeekDays;
                model.SelectedType = reward.SelectedType;
                model.RewardPoints = reward.RewardPoints;
                model.CategoryIds = reward.CategoryIds; 
                model.BrandIds = reward.BrandIds;
                model.ProductIds = reward.ProductIds;
                model.Active = reward.Active;
                var splitreward = reward.WeekDays.Split(",");
                model.StartWeek = splitreward[0];
                model.EndWeek = splitreward[1];
                model.ddlCategory = Convert.ToInt32(reward.CategoryIds);
                model.DesignerId = Convert.ToInt32(reward.BrandIds);
                model.DDlValue = reward.SelectedType;
                if (reward.CustomerRoleid != null)
                {
                    var aa = reward.CustomerRoleid.Split(',');
                    foreach (var i in aa)
                    {
                        if (i != "")
                            model.CustomerRoleid.Add(Convert.ToInt32(i));
                    }
                }
                //model.ProductName = product.Name;
                if (reward.ProductIds != null)
                {
                    var nameproduct = reward.ProductIds.Split(",");
                    foreach (var x in nameproduct)
                    {
                        if (x != "0")
                        {
                            var nameofproduct = _productService.GetProductByIdAsync(Convert.ToInt16(x)).Result.Name;
                            model.ProductName = nameofproduct + "$" + model.ProductName;
                        }
                    }
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
            else
            {
                if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                    return AccessDeniedView();

                //prepare model
                var model = new RewardsPointCustom();

                //categories
                //model.AvailableCategories.Add(new SelectListItem { Text = _localizationService.GetResourceAsync("Admin.Common.All").Result, Value = "0" });
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
        }
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public IActionResult addoreditRewardponts(RewardsPointCustom model, bool continueEditing)
        {
            if (model.Id == 0)
            {
                if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                    return AccessDeniedView();

                if (ModelState.IsValid)
                {

                    var rewards = new Nop.Core.Domain.CustomRewardPoint.RewardsPointCustom
                    {
                        WeekDays = model.StartWeek + "," + model.EndWeek,
                        RewardPoints = model.RewardPoints,
                        ProductIds = model.ProductIds,
                        Active = model.Active,
                        CategoryIds = Convert.ToString(model.ddlCategory),
                        BrandIds = Convert.ToString(model.DesignerId),
                        SelectedType = model.DDlValue,
                        CustomerRoleid = string.Join(", ", model.CustomerRoleid.Select(x => x)),
                    };

                    _CustomerRewardPointService.InsertCustomReward(rewards);
                    _notificationService.SuccessNotification(_localizationService.GetResourceAsync("Admin.Custom.Promotion.Added").Result);

                    if (continueEditing)
                        return RedirectToAction("addoreditRewardponts", new { id = rewards.Id });

                    return RedirectToAction("CustomReward");
                }
                return RedirectToAction("CustomReward");
            }
            else
            {

                if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                    return AccessDeniedView();

                if (ModelState.IsValid)
                {

                    var rewards = new Nop.Core.Domain.CustomRewardPoint.RewardsPointCustom
                    {
                        Id = model.Id,
                        WeekDays = model.StartWeek + "," + model.EndWeek,
                        RewardPoints = model.RewardPoints,
                        ProductIds = model.ProductIds,
                        Active = model.Active,
                        CategoryIds = Convert.ToString(model.ddlCategory),
                        BrandIds = Convert.ToString(model.DesignerId),
                        SelectedType = model.DDlValue,
                        CustomerRoleid = string.Join(", ", model.CustomerRoleid.Select(x => x)),
                    };

                    _CustomerRewardPointService.UpdateCustomReward(rewards);
                    _notificationService.SuccessNotification(_localizationService.GetResourceAsync("Admin.Custom.Promotion.Added").Result);

                    if (continueEditing)
                        return RedirectToAction("addoreditRewardponts", new { id = rewards.Id });

                    return RedirectToAction("CustomReward");
                }
                return RedirectToAction("CustomReward");
            }
        }
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
                        ViewBag.ProductIds = studentID;
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
        public IActionResult Delete(int id)
        {
            if (!_permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders).Result)
                return AccessDeniedView();

            var promotion = _CustomerRewardPointService.GetCustomRewardById(id);

            if (promotion == null)
                //No promotion found with the specified id
                return RedirectToAction("CustomReward");

            _CustomerRewardPointService.DeleteCustomReward(promotion);

            _notificationService.SuccessNotification(_localizationService.GetResourceAsync("Admin.Custom.Promotion.Deleted").Result);

            return RedirectToAction("CustomReward");
        }
        [ValidateAntiForgeryToken]
        public ActionResult SetActiveInactivePriceRange(bool priceStatus)
        {
            _settingService.SetSetting("CustomRewardPointActive/Inactive", priceStatus);
            return Json(new { Status = priceStatus });
        }
        #endregion
    }
}
