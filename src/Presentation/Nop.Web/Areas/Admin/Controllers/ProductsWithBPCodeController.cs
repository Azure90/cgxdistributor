using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.CustomPages;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Areas.Admin.Models.CustomPage;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Web.Areas.Admin.Controllers
{
    public class ProductsWithBPCodeController : BaseAdminController
    {

        #region Fields

        private readonly IPermissionService _permissionService;
        private readonly ICustomerService _customerService;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizationService;
        private readonly ICategoryModelFactory _categoryModelFactory;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IProductsWithBPCodeService _productsWithBPCodeService;

        #endregion

        #region Ctor

        public ProductsWithBPCodeController(IPermissionService permissionService,
            ICustomerService customerService,
            INotificationService notificationService,
            ILocalizationService localizationService,
            ICategoryModelFactory categoryModelFactory,
            IProductService productService,
            ICategoryService categoryService,
            IUrlRecordService urlRecordService,
            IProductsWithBPCodeService productsWithBPCodeService)
        {
            _permissionService = permissionService;
            _customerService = customerService;
            _notificationService = notificationService;
            _localizationService = localizationService;
            _categoryModelFactory = categoryModelFactory;
            _productService = productService;
            _categoryService = categoryService;
            _urlRecordService = urlRecordService;
            _productsWithBPCodeService = productsWithBPCodeService;
        }

        #endregion

        #region Utilities

        public virtual Task<ProductsWithBPCodeSearchModel> PrepareProductWithBPCodeSearchModelAsync(ProductsWithBPCodeSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return Task.FromResult(searchModel);
        }

        private async Task<ProductsWithBPCodeListModel> PrepareProductsWithBPCodeListModelAsync(ProductsWithBPCodeSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            // get all entities
            var allEntities = await _productsWithBPCodeService.GetAllProductWithBPCodeAsync();

            // Load all customers  
            var allCustomers = await _customerService.GetAllCustomersAsync();

            // apply paging from searchModel
            var pagedEntities = allEntities.ToPagedList(searchModel);

            var model = await new ProductsWithBPCodeListModel()
                .PrepareToGridAsync(searchModel, pagedEntities, () =>
                {
                    return pagedEntities.SelectAwait(async entity =>
                    {
                        // get product details
                        var product = await _productService.GetProductByIdAsync(entity.ProductId);

                        // get customer details
                        var bpCodeList = (entity.CustomerBPCodes ?? string.Empty)
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x?.Trim())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Distinct()
                            .ToList();

                        // get customers with bpcode
                        var customers = allCustomers
                        .Where(c => !string.IsNullOrWhiteSpace(c.BPCode)
                           && bpCodeList.Contains(c.BPCode.Trim()))
                        .ToList();

                        var emails = string.Empty;
                        var bpCodes = string.Empty;

                        if (customers?.Any() == true)
                        {
                            emails = string.Join(", ", customers.Where(c => !string.IsNullOrWhiteSpace(c.Email)).Select(c => c.Email.Trim()).Distinct());
                            bpCodes = string.Join(", ", customers.Where(c => !string.IsNullOrWhiteSpace(c.BPCode)).Select(c => c.BPCode.Trim()).Distinct());
                        }

                        return new ProductsWithBPCodeModel
                        {
                            Id = entity.Id,
                            ProductId = entity.ProductId,
                            ProductName = product?.Name,
                            Sku = product?.Sku,
                            BpCode = bpCodes,
                            Email = emails
                        };
                    });
                });

            return model;
        }

        public virtual async Task<AddProductToCategoryListModel> PrepareAddProductToCategoryListModelAsync(AddProductToCategorySearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            // show hidden/unpublished products only when any search filter is applied.            
            var showHidden = !string.IsNullOrWhiteSpace(searchModel.SearchProductName) || searchModel.SearchCategoryId > 0 || searchModel.SearchManufacturerId > 0
                || searchModel.SearchStoreId > 0 || searchModel.SearchVendorId > 0 || searchModel.SearchProductTypeId > 0;

            //get products
            var products = await _productService.SearchProductsAsync(showHidden: showHidden,
                categoryIds: new List<int> { searchModel.SearchCategoryId },
                manufacturerIds: new List<int> { searchModel.SearchManufacturerId },
                storeId: searchModel.SearchStoreId,
                vendorId: searchModel.SearchVendorId,
                productType: searchModel.SearchProductTypeId > 0 ? (ProductType?)searchModel.SearchProductTypeId : null,
                keywords: searchModel.SearchProductName,
                overridePublished: showHidden ? true : null,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare grid model
            var model = await new AddProductToCategoryListModel().PrepareToGridAsync(searchModel, products, () =>
            {
                return products.SelectAwait(async product =>
                {
                    var productModel = product.ToModel<ProductModel>();

                    productModel.SeName = await _urlRecordService.GetSeNameAsync(product, 0, true, false);

                    return productModel;
                });
            });

            return model;
        }

        private async Task<IList<SelectListItem>> PrepareAvailableBpCustomersAsync()
        {
            var customers = await _customerService.GetAllCustomersAsync();

            var list = customers.Where(c => !string.IsNullOrWhiteSpace(c.BPCode) && c.Active && !c.Deleted)
                 .GroupBy(c => c.BPCode)
                 .Select(g =>
                 {
                     return new SelectListItem
                     {
                         Value = g.Key, // BPCode
                         Text = $"{g.Key}"
                     };
                 }).OrderBy(x => x.Text).ToList();

            return list;
        }

        #endregion

        #region List

        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual async Task<IActionResult> List()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //prepare model
            var searchModel = await PrepareProductWithBPCodeSearchModelAsync(new ProductsWithBPCodeSearchModel());

            return View(searchModel);
        }

        [HttpPost]
        public virtual async Task<IActionResult> List(ProductsWithBPCodeSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await PrepareProductsWithBPCodeListModelAsync(searchModel);

            return Json(model);
        }

        #endregion

        #region Create / Edit / Delete

        public virtual async Task<IActionResult> Create()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //prepare model
            var model = new ProductsWithBPCodeModel
            {
                AvailableCustomers = await PrepareAvailableBpCustomersAsync()
            };

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual async Task<IActionResult> Create(ProductsWithBPCodeModel model, bool continueEditing)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            // Validate product
            if (model.ProductId is null or 0)
                ModelState.AddModelError(nameof(model.ProductId), "Please select a product.");

            // Validate customers
            if (model.SelectedCustomerBpCodes == null || !model.SelectedCustomerBpCodes.Any())
                ModelState.AddModelError(nameof(model.SelectedCustomerBpCodes), "Please select at least one BP Code customer.");

            if (ModelState.IsValid)
            {

                // Build comma separated BPCode string (no empty)
                var customerBpCodesCsv = string.Join(",", model.SelectedCustomerBpCodes
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());

                var entity = new ProductsWithBPCode
                {
                    ProductId = model.ProductId.Value,
                    CustomerBPCodes = customerBpCodesCsv
                };

                await _productsWithBPCodeService.InsertAsync(entity);

                _notificationService.SuccessNotification(await
                    _localizationService.GetResourceAsync("Admin.Catalog.Products.Added"));

                if (!continueEditing)
                    return RedirectToAction("List");

                return RedirectToAction("Edit", new { id = entity.Id });
            }

            // rebuild dropdowns on error
            model.AvailableCustomers = await PrepareAvailableBpCustomersAsync();

            return View(model);
        }

        public virtual async Task<IActionResult> Edit(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            var entity = await _productsWithBPCodeService.GetByIdAsync(id);
            if (entity == null)
                return RedirectToAction("List");

            var model = new ProductsWithBPCodeModel
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                ProductName = (await _productService.GetProductByIdAsync(entity.ProductId)).Name ?? string.Empty
            };

            // convert list for multi-select
            model.SelectedCustomerBpCodes = (entity.CustomerBPCodes ?? string.Empty)
             .Split(',', StringSplitOptions.RemoveEmptyEntries)
             .Select(x => x.Trim())
             .Where(x => !string.IsNullOrWhiteSpace(x))
             .Distinct()
             .ToList();

            model.AvailableCustomers = await PrepareAvailableBpCustomersAsync();

            return View(model);
        }


        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual async Task<IActionResult> Edit(ProductsWithBPCodeModel model, bool continueEditing)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            var entity = await _productsWithBPCodeService.GetByIdAsync(model.Id);

            if (entity == null)
                return RedirectToAction("List");

            if (model.ProductId is null or 0)
                ModelState.AddModelError(nameof(model.ProductId), "Please select a product.");

            if (model.SelectedCustomerBpCodes == null || !model.SelectedCustomerBpCodes.Any())
                ModelState.AddModelError(nameof(model.SelectedCustomerBpCodes), "Please select at least one BP Code customer.");

            if (ModelState.IsValid)
            {
                entity.ProductId = model.ProductId.Value;

                entity.CustomerBPCodes = string.Join(",",
                    model.SelectedCustomerBpCodes.Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()).Distinct());

                await _productsWithBPCodeService.UpdateAsync(entity);

                _notificationService.SuccessNotification(await
                    _localizationService.GetResourceAsync("Admin.Catalog.Products.Updated"));

                if (!continueEditing)
                    return RedirectToAction("List");

                return RedirectToAction("Edit", new { id = entity.Id });
            }

            model.AvailableCustomers = await PrepareAvailableBpCustomersAsync();

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Delete(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //try to get a product with the specified id
            var product = await _productsWithBPCodeService.GetByIdAsync(id);
            if (product == null)
                return RedirectToAction("List");

            await _productsWithBPCodeService.DeleteAsync(product);

            return new NullJsonResult();
        }


        #endregion

        #region Product Add Popup

        public virtual async Task<IActionResult> ProductAddPopup(int categoryId, string btnId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCategories))
                return AccessDeniedView();

            //prepare model
            var model = await _categoryModelFactory.PrepareAddProductToCategorySearchModelAsync(new AddProductToCategorySearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> ProductAddPopupList(AddProductToCategorySearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCategories))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await PrepareAddProductToCategoryListModelAsync(searchModel);

            return Json(model);
        }

        [HttpPost]
        [FormValueRequired("save")]
        public virtual async Task<IActionResult> ProductAddPopup(AddProductToCategoryModel model, string btnId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCategories))
                return AccessDeniedView();

            var productId = model.SelectedProductIds?.FirstOrDefault() ?? 0;

            if (productId > 0)
            {
                var product = await _productService.GetProductByIdAsync(productId);

                ViewBag.productId = product.Id.ToString();
                ViewBag.productName = product.Name;
            }

            ViewBag.RefreshPage = true;

            return View(new AddProductToCategorySearchModel());
        }

        #endregion
    }
}
