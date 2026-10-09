using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.CustomPages;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Html;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.CustomDiscount;
using Nop.Web.Models.Order.PreOrders;

namespace Nop.Web.Controllers
{
    [AutoValidateAntiforgeryToken]
    public partial class ProductController : BasePublicController
    {
        #region Fields

        private readonly CaptchaSettings _captchaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly IAclService _aclService;
        private readonly ICompareProductsService _compareProductsService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerService _customerService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IHtmlFormatter _htmlFormatter;
        private readonly ILocalizationService _localizationService;
        private readonly INopUrlHelper _nopUrlHelper;
        private readonly IOrderService _orderService;
        private readonly IPermissionService _permissionService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IProductService _productService;
        private readonly IRecentlyViewedProductsService _recentlyViewedProductsService;
        private readonly IReviewTypeService _reviewTypeService;
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWorkContext _workContext;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly ISettingService _settingService;
        private readonly IDiscountService _discountService;
        private readonly IPreOrderService _preOrderService;
        private readonly IProductsWithBPCodeService _productsWithBPCodeService;

        #endregion

        #region Ctor

        public ProductController(CaptchaSettings captchaSettings,
            CatalogSettings catalogSettings,
            IAclService aclService,
            ICompareProductsService compareProductsService,
            ICustomerActivityService customerActivityService,
            ICustomerService customerService,
            IEventPublisher eventPublisher,
            IHtmlFormatter htmlFormatter,
            ILocalizationService localizationService,
            INopUrlHelper nopUrlHelper,
            IOrderService orderService,
            IPermissionService permissionService,
            IProductAttributeParser productAttributeParser,
            IProductModelFactory productModelFactory,
            IProductService productService,
            IRecentlyViewedProductsService recentlyViewedProductsService,
            IReviewTypeService reviewTypeService,
            IShoppingCartModelFactory shoppingCartModelFactory,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IUrlRecordService urlRecordService,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            LocalizationSettings localizationSettings,
            ShoppingCartSettings shoppingCartSettings,
            ShippingSettings shippingSettings,
            ISettingService settingService,
            IDiscountService discountService,
            IPreOrderService preOrderService,
            IProductsWithBPCodeService productsWithBPCodeService)
        {
            _captchaSettings = captchaSettings;
            _catalogSettings = catalogSettings;
            _aclService = aclService;
            _compareProductsService = compareProductsService;
            _customerActivityService = customerActivityService;
            _customerService = customerService;
            _eventPublisher = eventPublisher;
            _htmlFormatter = htmlFormatter;
            _localizationService = localizationService;
            _nopUrlHelper = nopUrlHelper;
            _orderService = orderService;
            _permissionService = permissionService;
            _productAttributeParser = productAttributeParser;
            _productModelFactory = productModelFactory;
            _productService = productService;
            _reviewTypeService = reviewTypeService;
            _recentlyViewedProductsService = recentlyViewedProductsService;
            _shoppingCartModelFactory = shoppingCartModelFactory;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _storeMappingService = storeMappingService;
            _urlRecordService = urlRecordService;
            _workContext = workContext;
            _workflowMessageService = workflowMessageService;
            _localizationSettings = localizationSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _shippingSettings = shippingSettings;
            _settingService = settingService;
            _discountService = discountService;
            _preOrderService = preOrderService;
            _productsWithBPCodeService = productsWithBPCodeService;
        }

        #endregion

        #region Utilities

        protected virtual async Task ValidateProductReviewAvailabilityAsync(Product product)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (await _customerService.IsGuestAsync(customer) && !_catalogSettings.AllowAnonymousUsersToReviewProduct)
                ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Reviews.OnlyRegisteredUsersCanWriteReviews"));

            if (!_catalogSettings.ProductReviewPossibleOnlyAfterPurchasing)
                return;

            var hasCompletedOrders = product.ProductType == ProductType.SimpleProduct
                ? await HasCompletedOrdersAsync(product)
                : await (await _productService.GetAssociatedProductsAsync(product.Id)).AnyAwaitAsync(HasCompletedOrdersAsync);

            if (!hasCompletedOrders)
                ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Reviews.ProductReviewPossibleOnlyAfterPurchasing"));
        }

        protected virtual async ValueTask<bool> HasCompletedOrdersAsync(Product product)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            return (await _orderService.SearchOrdersAsync(customerId: customer.Id,
                productId: product.Id,
                osIds: new List<int> { (int)OrderStatus.Complete },
                pageSize: 1)).Any();
        }

        #endregion

        #region Product details page

        public bool Getproductrole(Product product)
        {
            bool isexist = false;
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

            var productrole = _aclService.GetAclRecordsAsync(product).Result;
            var roleIds = currRoles.Select(r => r.Id).ToList();
            isexist = productrole.Any(x => roleIds.Contains(x.CustomerRoleId));

            return isexist;
        }

        public virtual async Task<IActionResult> ProductDetails(int productId, int updatecartitemid = 0)
        {
            var product = await _productService.GetProductByIdAsync(productId);

            // commented by tpss sh 
            //if (product == null || product.Deleted || !product.Issample)

            if (product == null || product.Deleted)
                return InvokeHttp404();

            // Get current logged in customer
            var customer = await _workContext.GetCurrentCustomerAsync();

            // Check if this product is blocked for this customer
            var hideProduct = await _productsWithBPCodeService
                .GetBlockedProductIdsForCustomerAsync(customer?.BPCode, productId);

            // If any record returned the customer must NOT see this product
            if (hideProduct.Any())
                return InvokeHttp404();

            var isAnyDiscount = false;
            if (!product.Issample)
            {
                // check for any discount or special for product
                isAnyDiscount = await _productService.CheckForSpecialOrDiscountAppliedToProductAsync(productId, customer?.BPCode ?? string.Empty);

                //isAnyDiscount = existedIds.Contains(productId);

                if (!isAnyDiscount)
                    return InvokeHttp404();
            }

            if (!Getproductrole(product))
                return InvokeHttp404();

            var notAvailable =
                //published?
                (!product.Published && !_catalogSettings.AllowViewUnpublishedProductPage) ||
                //ACL (access control list) 
                !await _aclService.AuthorizeAsync(product) ||
                //Store mapping
                !await _storeMappingService.AuthorizeAsync(product) ||
                //availability dates
                !_productService.ProductIsAvailable(product);

            //Check whether the current user has a "Manage products" permission (usually a store owner)
            //We should allows him (her) to use "Preview" functionality
            var hasAdminAccess = await _permissionService.AuthorizeAsync(StandardPermissionProvider.AccessAdminPanel) && await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts);
            if (notAvailable && !hasAdminAccess)
                return InvokeHttp404();

            //visible individually?
            if (!product.VisibleIndividually)
            {
                //is this one an associated products?
                var parentGroupedProduct = await _productService.GetProductByIdAsync(product.ParentGroupedProductId);
                if (parentGroupedProduct == null)
                    return RedirectToRoute("Homepage");

                var seName = await _urlRecordService.GetSeNameAsync(parentGroupedProduct);
                var productUrl = await _nopUrlHelper.RouteGenericUrlAsync<Product>(new { SeName = seName });
                return LocalRedirectPermanent(productUrl);
            }

            //update existing shopping cart or wishlist  item?
            ShoppingCartItem updatecartitem = null;
            DateTime? preferredDeliveryDate = null;

            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, storeId : store.Id);

            // Get the existing preorder date (if any)
            preferredDeliveryDate = cart?
                .Where(x => x.IsPreOrder && x.PreferredDeliveryDate.HasValue)
                .Select(x => x.PreferredDeliveryDate)
                .FirstOrDefault();

            if (_shoppingCartSettings.AllowCartItemEditing && updatecartitemid > 0)
            {
                var seName = await _urlRecordService.GetSeNameAsync(product);
                var productUrl = await _nopUrlHelper.RouteGenericUrlAsync<Product>(new { SeName = seName });
                
                updatecartitem = cart.FirstOrDefault(x => x.Id == updatecartitemid);              

                //not found?
                if (updatecartitem == null)
                    return LocalRedirect(productUrl);

                //is it this product?
                if (product.Id != updatecartitem.ProductId)
                    return LocalRedirect(productUrl);
            }

            //save as recently viewed
            await _recentlyViewedProductsService.AddProductToRecentlyViewedListAsync(product.Id);

            //display "edit" (manage) link
            if (await _permissionService.AuthorizeAsync(StandardPermissionProvider.AccessAdminPanel) &&
                await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
            {
                //a vendor should have access only to his products
                var currentVendor = await _workContext.GetCurrentVendorAsync();
                if (currentVendor == null || currentVendor.Id == product.VendorId)
                {
                    DisplayEditLink(Url.Action("Edit", "Product", new { id = product.Id, area = AreaNames.Admin }));
                }
            }

            //activity log
            await _customerActivityService.InsertActivityAsync("PublicStore.ViewProduct",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.PublicStore.ViewProduct"), product.Name), product);

            //model
            var model = await _productModelFactory.PrepareProductDetailsModelAsync(product, updatecartitem, false);

            //if(model.ProductAttributes.Count() > 0)
            {
                var lowestprice = getthelowestprice(productId);
                if (lowestprice != "" && lowestprice != null)
                {
                    model.ProductPrice.Price = string.Format("{0:c}", Convert.ToDouble(lowestprice));
                    if (model.ProductAttributes.Count() > 0)
                    {
                        model.ProductPrice.Price = model.ProductPrice.Price + await _localizationService.GetResourceAsync("ShoppingCart.Startingfrom");
                    }
                }
            }
            //template
            var productTemplateViewPath = await _productModelFactory.PrepareProductTemplateViewPathAsync(product);

            model.HasDiscountOrSpecialPrice = isAnyDiscount;

            // hide show preorder section
            var preOrderEnabled = await _productService
                .IsPreOrderAvailableForCustomerOrProductAsync(customer, productId);

            model.ShowPreOrderSection = preOrderEnabled;

            if (preOrderEnabled)
                await PreparePreOrderSettingsAsync(model);

            model.PreferredDeliveryDate = preferredDeliveryDate;

            return View(productTemplateViewPath, model);
        }

        public string getthelowestprice(int productid)
        {

            try
            {
                var customer = _workContext.GetCurrentCustomerAsync().Result;
                var connection = DataSettingsManager.LoadSettings().ConnectionString;
                using (IDbConnection conn1 = new SqlConnection(connection))
                {
                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@ProductId", productid);
                    ds.Add("@CustomerId", customer.Id);

                    var result = conn1.Query("GetSpecialPriceByCustomerandPorduct", param: ds, commandType: CommandType.StoredProcedure);
                    if (result.Count() > 0)
                    {
                        var FinalPrice = result.FirstOrDefault().price;
                        if (FinalPrice > 0)
                        {
                            return Convert.ToString(FinalPrice);
                        }
                    }

                }
                //var GetallRoles = _customerService.GetCustomerRoles(customer).Where(d => d.Name == "LISTPRICE 2023 CAD"
                //          || d.Name == "LISTPRICE 2023 USD"
                //          || (d.Name == "INTNAT 2023" || d.Name == "INTNAT 2023 USD")
                //          || d.Name == "HOSPIT 2023 CAD"
                //          || d.Name == "20OFF 2023 CAD"
                //          || d.Name == "DIST1 2023 CAD"
                //          || d.Name == "DISTD 2023 CAD"
                //          ).FirstOrDefault();
                var GetallRoles = _customerService.GetCustomerRolesAsync(customer).Result.Where(d => d.Name == "CAD B2C"
                 || d.Name == "USD B2C"
                 || d.Name == "USD B2B INTL"
                 || d.Name == "CAD B2C HS"
                 || d.Name == "USD B2B ST"
                 || d.Name == "USD B2B DS"
                 || d.Name == "CAD B2B ST"
                 || d.Name == "CAD B2B DS"
                 ).FirstOrDefault();
                if (GetallRoles != null)
                {


                    using (IDbConnection conn1 = new SqlConnection(connection))
                    {

                        DynamicParameters ds = new DynamicParameters();

                        ds.Add("@prodid", productid);
                        ds.Add("@CustomerRoleId", GetallRoles.Id);
                        var result = conn1.Query("getpriceasperproductid", param: ds, commandType: CommandType.StoredProcedure);
                        return Convert.ToString(result.FirstOrDefault().price);

                    }
                }
            }
            catch (Exception ex)
            {
                return "";
            }
            return "";
        }
        [HttpPost]
        public virtual async Task<IActionResult> EstimateShipping([FromQuery] ProductDetailsModel.ProductEstimateShippingModel model, IFormCollection form)
        {
            if (model == null)
                model = new ProductDetailsModel.ProductEstimateShippingModel();

            var errors = new List<string>();

            if (!_shippingSettings.EstimateShippingCityNameEnabled && string.IsNullOrEmpty(model.ZipPostalCode))
                errors.Add(await _localizationService.GetResourceAsync("Shipping.EstimateShipping.ZipPostalCode.Required"));

            if (_shippingSettings.EstimateShippingCityNameEnabled && string.IsNullOrEmpty(model.City))
                errors.Add(await _localizationService.GetResourceAsync("Shipping.EstimateShipping.City.Required"));

            if (model.CountryId == null || model.CountryId == 0)
                errors.Add(await _localizationService.GetResourceAsync("Shipping.EstimateShipping.Country.Required"));

            if (errors.Count > 0)
                return Json(new
                {
                    Success = false,
                    Errors = errors
                });

            var product = await _productService.GetProductByIdAsync(model.ProductId);
            if (product == null || product.Deleted)
            {
                errors.Add(await _localizationService.GetResourceAsync("Shipping.EstimateShippingPopUp.Product.IsNotFound"));
                return Json(new
                {
                    Success = false,
                    Errors = errors
                });
            }

            var store = await _storeContext.GetCurrentStoreAsync();
            var customer = await _workContext.GetCurrentCustomerAsync();

            var wrappedProduct = new ShoppingCartItem()
            {
                StoreId = store.Id,
                ShoppingCartTypeId = (int)ShoppingCartType.ShoppingCart,
                CustomerId = customer.Id,
                ProductId = product.Id,
                CreatedOnUtc = DateTime.UtcNow
            };

            var addToCartWarnings = new List<string>();
            //customer entered price
            wrappedProduct.CustomerEnteredPrice = await _productAttributeParser.ParseCustomerEnteredPriceAsync(product, form);

            //entered quantity
            wrappedProduct.Quantity = _productAttributeParser.ParseEnteredQuantity(product, form);

            //product and gift card attributes
            wrappedProduct.AttributesXml = await _productAttributeParser.ParseProductAttributesAsync(product, form, addToCartWarnings);

            //rental attributes
            _productAttributeParser.ParseRentalDates(product, form, out var rentalStartDate, out var rentalEndDate);
            wrappedProduct.RentalStartDateUtc = rentalStartDate;
            wrappedProduct.RentalEndDateUtc = rentalEndDate;

            var result = await _shoppingCartModelFactory.PrepareEstimateShippingResultModelAsync(new[] { wrappedProduct }, model, false);

            return Json(result);
        }

        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> GetProductCombinations(int productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                return NotFound();

            var model = await _productModelFactory.PrepareProductCombinationModelsAsync(product);
            return Ok(model);
        }

        #endregion

        #region Recently viewed products

        public virtual async Task<IActionResult> RecentlyViewedProducts()
        {
            if (!_catalogSettings.RecentlyViewedProductsEnabled)
                return Content("");

            var products = await _recentlyViewedProductsService.GetRecentlyViewedProductsAsync(_catalogSettings.RecentlyViewedProductsNumber);

            var model = new List<ProductOverviewModel>();
            model.AddRange(await _productModelFactory.PrepareProductOverviewModelsAsync(products));

            return View(model);
        }

        #endregion

        #region Product reviews

        public virtual async Task<IActionResult> ProductReviews(int productId)
        {


            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || product.Deleted || !product.Published || !product.AllowCustomerReviews)
                return RedirectToRoute("Homepage");

            var model = new ProductReviewsModel();
            model = await _productModelFactory.PrepareProductReviewsModelAsync(model, product);

            await ValidateProductReviewAvailabilityAsync(product);

            //default value
            model.AddProductReview.Rating = _catalogSettings.DefaultProductRatingValue;

            //default value for all additional review types
            if (model.ReviewTypeList.Count > 0)
                foreach (var additionalProductReview in model.AddAdditionalProductReviewList)
                {
                    additionalProductReview.Rating = additionalProductReview.IsRequired ? _catalogSettings.DefaultProductRatingValue : 0;
                }


            return View(model);
        }

        [HttpPost, ActionName("ProductReviews")]
        [FormValueRequired("add-review")]
        [ValidateCaptcha]
        public virtual async Task<IActionResult> ProductReviewsAdd(int productId, ProductReviewsModel model, bool captchaValid)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            var currentStore = await _storeContext.GetCurrentStoreAsync();

            if (product == null || product.Deleted || !product.Published || !product.AllowCustomerReviews ||
                !await _productService.CanAddReviewAsync(product.Id, _catalogSettings.ShowProductReviewsPerStore ? currentStore.Id : 0))
                return RedirectToRoute("Homepage");

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnProductReviewPage && !captchaValid)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
            }

            await ValidateProductReviewAvailabilityAsync(product);

            if (ModelState.IsValid)
            {
                //save review
                var rating = model.AddProductReview.Rating;
                if (rating < 1 || rating > 5)
                    rating = _catalogSettings.DefaultProductRatingValue;
                var isApproved = !_catalogSettings.ProductReviewsMustBeApproved;
                var customer = await _workContext.GetCurrentCustomerAsync();

                var productReview = new ProductReview
                {
                    ProductId = product.Id,
                    CustomerId = customer.Id,
                    Title = model.AddProductReview.Title,
                    ReviewText = model.AddProductReview.ReviewText,
                    Rating = rating,
                    HelpfulYesTotal = 0,
                    HelpfulNoTotal = 0,
                    IsApproved = model.AddProductReview.Rating >= 4 ? true : false,
                    CreatedOnUtc = DateTime.UtcNow,
                    StoreId = currentStore.Id,
                };

                await _productService.InsertProductReviewAsync(productReview);

                //add product review and review type mapping                
                foreach (var additionalReview in model.AddAdditionalProductReviewList)
                {
                    var additionalProductReview = new ProductReviewReviewTypeMapping
                    {
                        ProductReviewId = productReview.Id,
                        ReviewTypeId = additionalReview.ReviewTypeId,
                        Rating = additionalReview.Rating
                    };

                    await _reviewTypeService.InsertProductReviewReviewTypeMappingsAsync(additionalProductReview);
                }

                //update product totals
                await _productService.UpdateProductReviewTotalsAsync(product);

                //notify store owner
                if (_catalogSettings.NotifyStoreOwnerAboutNewProductReviews)
                    await _workflowMessageService.SendProductReviewStoreOwnerNotificationMessageAsync(productReview, _localizationSettings.DefaultAdminLanguageId);

                //activity log
                await _customerActivityService.InsertActivityAsync("PublicStore.AddProductReview",
                    string.Format(await _localizationService.GetResourceAsync("ActivityLog.PublicStore.AddProductReview"), product.Name), product);

                //raise event
                if (productReview.IsApproved)
                    await _eventPublisher.PublishAsync(new ProductReviewApprovedEvent(productReview));

                model = await _productModelFactory.PrepareProductReviewsModelAsync(model, product);
                model.AddProductReview.Title = null;
                model.AddProductReview.ReviewText = null;

                model.AddProductReview.SuccessfullyAdded = true;
                if (!isApproved)
                    model.AddProductReview.Result = await _localizationService.GetResourceAsync("Reviews.SeeAfterApproving");
                else
                    model.AddProductReview.Result = await _localizationService.GetResourceAsync("Reviews.SuccessfullyAdded");

                return View(model);
            }

            //if we got this far, something failed, redisplay form
            model = await _productModelFactory.PrepareProductReviewsModelAsync(model, product);
            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> SetProductReviewHelpfulness(int productReviewId, bool washelpful)
        {
            var productReview = await _productService.GetProductReviewByIdAsync(productReviewId);
            if (productReview == null)
                throw new ArgumentException("No product review found with the specified id");

            var customer = await _workContext.GetCurrentCustomerAsync();
            if (await _customerService.IsGuestAsync(customer) && !_catalogSettings.AllowAnonymousUsersToReviewProduct)
            {
                return Json(new
                {
                    Result = await _localizationService.GetResourceAsync("Reviews.Helpfulness.OnlyRegistered"),
                    TotalYes = productReview.HelpfulYesTotal,
                    TotalNo = productReview.HelpfulNoTotal
                });
            }

            //customers aren't allowed to vote for their own reviews
            if (productReview.CustomerId == customer.Id)
            {
                return Json(new
                {
                    Result = await _localizationService.GetResourceAsync("Reviews.Helpfulness.YourOwnReview"),
                    TotalYes = productReview.HelpfulYesTotal,
                    TotalNo = productReview.HelpfulNoTotal
                });
            }

            await _productService.SetProductReviewHelpfulnessAsync(productReview, washelpful);

            //new totals
            await _productService.UpdateProductReviewHelpfulnessTotalsAsync(productReview);

            return Json(new
            {
                Result = await _localizationService.GetResourceAsync("Reviews.Helpfulness.SuccessfullyVoted"),
                TotalYes = productReview.HelpfulYesTotal,
                TotalNo = productReview.HelpfulNoTotal
            });
        }

        public virtual async Task<IActionResult> CustomerProductReviews(int? pageNumber)
        {
            if (await _customerService.IsGuestAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            if (!_catalogSettings.ShowProductReviewsTabOnAccountPage)
            {
                return RedirectToRoute("CustomerInfo");
            }

            var model = await _productModelFactory.PrepareCustomerProductReviewsModelAsync(pageNumber);

            return View(model);
        }

        #endregion

        #region Email a friend

        public virtual async Task<IActionResult> ProductEmailAFriend(int productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || product.Deleted || !product.Published || !_catalogSettings.EmailAFriendEnabled)
                return RedirectToRoute("Homepage");

            var model = new ProductEmailAFriendModel();
            model = await _productModelFactory.PrepareProductEmailAFriendModelAsync(model, product, false);
            return View(model);
        }

        [HttpPost, ActionName("ProductEmailAFriend")]
        [FormValueRequired("send-email")]
        [ValidateCaptcha]
        public virtual async Task<IActionResult> ProductEmailAFriendSend(ProductEmailAFriendModel model, bool captchaValid)
        {
            var product = await _productService.GetProductByIdAsync(model.ProductId);
            if (product == null || product.Deleted || !product.Published || !_catalogSettings.EmailAFriendEnabled)
                return RedirectToRoute("Homepage");

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnEmailProductToFriendPage && !captchaValid)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
            }

            //check whether the current customer is guest and ia allowed to email a friend
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (await _customerService.IsGuestAsync(customer) && !_catalogSettings.AllowAnonymousUsersToEmailAFriend)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Products.EmailAFriend.OnlyRegisteredUsers"));
            }

            if (ModelState.IsValid)
            {
                //email
                await _workflowMessageService.SendProductEmailAFriendMessageAsync(customer,
                        (await _workContext.GetWorkingLanguageAsync()).Id, product,
                        model.YourEmailAddress, model.FriendEmail,
                        _htmlFormatter.FormatText(model.PersonalMessage, false, true, false, false, false, false));

                model = await _productModelFactory.PrepareProductEmailAFriendModelAsync(model, product, true);
                model.SuccessfullySent = true;
                model.Result = await _localizationService.GetResourceAsync("Products.EmailAFriend.SuccessfullySent");

                return View(model);
            }

            //If we got this far, something failed, redisplay form
            model = await _productModelFactory.PrepareProductEmailAFriendModelAsync(model, product, true);
            return View(model);
        }

        #endregion

        #region Comparing products

        [HttpPost]
        public virtual async Task<IActionResult> AddProductToCompareList(int productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || product.Deleted || !product.Published)
                return Json(new
                {
                    success = false,
                    message = "No product found with the specified ID"
                });

            if (!_catalogSettings.CompareProductsEnabled)
                return Json(new
                {
                    success = false,
                    message = "Product comparison is disabled"
                });

            await _compareProductsService.AddProductToCompareListAsync(productId);

            //activity log
            await _customerActivityService.InsertActivityAsync("PublicStore.AddToCompareList",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.PublicStore.AddToCompareList"), product.Name), product);

            return Json(new
            {
                success = true,
                message = string.Format(await _localizationService.GetResourceAsync("Products.ProductHasBeenAddedToCompareList.Link"), Url.RouteUrl("CompareProducts"))
                //use the code below (commented) if you want a customer to be automatically redirected to the compare products page
                //redirect = Url.RouteUrl("CompareProducts"),
            });
        }

        public virtual async Task<IActionResult> RemoveProductFromCompareList(int productId, string data)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                return RedirectToRoute("Homepage");

            if (!_catalogSettings.CompareProductsEnabled)
                return RedirectToRoute("Homepage");

            await _compareProductsService.RemoveProductFromCompareListAsync(productId);

            if (data == "Partial")
            {
                //return RedirectToRoute("Customer-Portal");
                return RedirectToAction("CustomerOrdersGraph", "Customer");
            }
            else
            {
                return RedirectToRoute("CompareProducts");
            }
        }

        public virtual async Task<IActionResult> CompareProducts(string data = "")
        {


            var model = new CompareProductsModel
            {
                IncludeShortDescriptionInCompareProducts = _catalogSettings.IncludeShortDescriptionInCompareProducts,
                IncludeFullDescriptionInCompareProducts = _catalogSettings.IncludeFullDescriptionInCompareProducts,
            };

            var products = await (await _compareProductsService.GetComparedProductsAsync())
            //ACL and store mapping
            .WhereAwait(async p => await _aclService.AuthorizeAsync(p) && await _storeMappingService.AuthorizeAsync(p))
            //availability dates
            .Where(p => _productService.ProductIsAvailable(p)).ToListAsync();

            //prepare model
            var poModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(products, prepareSpecificationAttributes: true))
                .ToList();
            foreach (var poModel in poModels)
            {
                model.Products.Add(poModel);
            }
            if (data == "partial")
            {
                return PartialView("~/Views/Customer/_CompareProductsPartial.cshtml", model);
            }
            else
            {
                return View(model);
            }
            return View(model);
        }

        public virtual IActionResult ClearCompareList(string data = "")
        {
            if (!_catalogSettings.CompareProductsEnabled)
                return RedirectToRoute("Homepage");

            _compareProductsService.ClearCompareProducts();
            if (data == "Partial")
            {
                return RedirectToRoute("Customer-Portal");
            }
            else
            {
                return RedirectToRoute("CompareProducts");
            }
        }

        #endregion

        #region Discount for Mega Menu Tab 

        // Changes date 16-06-2026
        // Commented by tpss sh for getting product with role requirement by HJ in the discount tab.
        // (i.e not with the discount group or with the below code) 

        //[HttpGet]
        //[RequireRegisteredCustomer]
        //public async Task<IActionResult> GetDiscountPriceProducts()
        //{
        //    // Check for the menu item is enabled
        //    if (!await IsCustomMegaMenuEnabledAsync("CustomPromotion.IsEnabledCustomMegaMenuItem"))
        //        return RedirectToRoute("Homepage");

        //    // Check discount by discountIds
        //    var (_, discountIds) = await _discountService.GetDiscountsForCustomerRolesAsync("CustomDiscountlistfortab", true);
        //    if (string.IsNullOrWhiteSpace(discountIds))
        //        return RedirectToRoute("Homepage");

        //    // Get the current cutomer
        //    var customer = await _workContext.GetCurrentCustomerAsync();            

        //    // Get the roles and the label text from local string 
        //    var customerRoles = await _customerService.GetCustomerRolesAsync(customer);
        //    var customerRoleIds = customerRoles?.Select(r => r.Id).ToList() ?? new List<int>();
        //    var discountRoleItems = await _discountService.GetDiscountRoleLabelItemsAsync();

        //    var discountModel = await GetDiscountAndSpecialProductsAsync("discount", discountIds: discountIds);
        //    var hasProducts = discountModel?.CatalogProducts?.Products?.Any() == true;
        //    if (!hasProducts)
        //        return RedirectToRoute("Homepage");

        //    discountModel.DiscountIds = discountIds;

        //    // Find the first matching label where the customer's role matches any in the list
        //    var disabledCartButton = await _discountService
        //        .GetCartButtonSettingForCustomerRolesAsync("DisableCartButtonForDiscountRoles");

        //    var matchingItem = discountRoleItems
        //        .FirstOrDefault(item =>
        //            item?.DiscountCustomerRoleId != null &&
        //            item.DiscountCustomerRoleId.Any(roleId => customerRoleIds.Contains(roleId))
        //        );

        //    // If found, use that label
        //    var discountPriceLabel = disabledCartButton
        //        ? await _localizationService.GetResourceAsync("Admin.Custom.Discount.DiscountHeaderText")
        //        : matchingItem?.DiscountLabelText ?? "Discounted Products";

        //    ViewBag.PageTitle = discountPriceLabel;

        //    // For discount price scenario
        //    return View("~/Views/Product/DiscountAndSpecialPriceProducts.cshtml", discountModel);
        //}

        [HttpGet]
        [RequireRegisteredCustomer]
        public async Task<IActionResult> GetDiscountPriceProducts()
        {
            // Check for the menu item is enabled
            if (!await IsCustomMegaMenuEnabledAsync("CustomPromotion.IsEnabledCustomMegaMenuItem"))
                return RedirectToRoute("Homepage");

            // Check discount by discountIds
            var customerRoleIds = await _discountService.GetCustomerRoleIdsFromLocalizationResourceAsync("CustomDiscountlistfortab");

            if (!customerRoleIds.Any())
                return RedirectToRoute("Homepage");

            // label for discount tab with customer role
            var discountRoleItems = await _discountService.GetDiscountRoleLabelItemsAsync();

            var matchedRoleIds = string.Join(",", customerRoleIds);

            var discountModel = await GetDiscountProductsWithCustomerRoleAsync(matchedRoleIds);
            var hasProducts = discountModel?.CatalogProducts?.Products?.Any() == true;

            if (!hasProducts)
                return RedirectToRoute("Homepage");

            discountModel.DiscountIds = string.Empty;
            discountModel.CustomerRoleIds = matchedRoleIds;

            // Find the first matching label where the customer's role matches any in the list
            var disabledCartButton = await _discountService
                .GetCartButtonSettingForCustomerRolesAsync("DisableCartButtonForDiscountRoles");

            var matchingItem = discountRoleItems
                .FirstOrDefault(item =>
                    item?.DiscountCustomerRoleId != null &&
                    item.DiscountCustomerRoleId.Any(roleId => customerRoleIds.Contains(roleId))
                );

            // If found, use that label
            var discountPriceLabel = disabledCartButton
                ? await _localizationService.GetResourceAsync("Admin.Custom.Discount.DiscountHeaderText")
                : matchingItem?.DiscountLabelText ?? "Discounted Products";

            ViewBag.PageTitle = discountPriceLabel;

            // For discount price scenario
            return View("~/Views/Product/DiscountAndSpecialPriceProducts.cshtml", discountModel);
        }

        [HttpGet]
        [RequireRegisteredCustomer]
        public async Task<IActionResult> GetSpecialPriceProducts()
        {
            // Check for the menu item is enabled
            if (!await IsCustomMegaMenuEnabledAsync("CustomPromotion.IsEnabledCustomMegaMenuItem"))
                return RedirectToRoute("Homepage");

            // Get the current cutomer
            var customer = await _workContext.GetCurrentCustomerAsync();

            // Get valid Bp code for customer
            var bpCode = customer.BPCode ?? "0";

            if (string.IsNullOrWhiteSpace(bpCode) || bpCode == "0")
                return RedirectToRoute("Homepage");

            // Get special-price products for the given BP code and ensure products exist
            var specialModel = await GetDiscountAndSpecialProductsAsync("special", bpCode: bpCode);
            var hasProducts = specialModel?.CatalogProducts?.Products?.Any() == true;

            if (!hasProducts)
                return RedirectToRoute("Homepage");

            specialModel.BpCode = bpCode;

            var specialHeaderText = await _localizationService.GetResourceAsync("Admin.Custom.Discount.MegaMenu.SpecialPriceLabelTexts");
            ViewBag.PageTitle = specialHeaderText ?? "Special Price Products";

            // For special price scenario
            return View("~/Views/Product/DiscountAndSpecialPriceProducts.cshtml", specialModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductsByCategory(string type, int? categoryId, string roleIds = null,
            string discountIds = null, string bpCode = null, int pageNumber = 1, int pageSize = 15)
        {
            // Get the current customer
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer == null || categoryId == 0)
                return Json(new { success = 0, content = string.Empty });

            IList<Product> products;
            int totalCount;

            if (string.Equals(type, "discount", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(discountIds) && !string.IsNullOrEmpty(roleIds))
            {
                // get all products list
                products = await GetPagedProductsWithCustomerRoleAsync(roleIds, categoryId);

                // get all product count
                totalCount = products.Count;

                // pagination on all products
                products = products
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                // get all products list
                (products, totalCount) = await GetPagedDiscountOrSpecialProductsAsync(type,
                    discountIds, bpCode, categoryId);

                // pagination on all products
                products = products
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }

            if (!products.Any())
                return Json(new { success = 0, content = string.Empty });

            // prepare product overview models for catalog display
            var productModels = await _productModelFactory.PrepareProductOverviewModelsAsync(products);

            // prepare category models 
            var catalogModel = new CatalogProductsModel
            {
                Products = productModels.ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalCount
            };

            return PartialView("~/Views/Product/_DiscountAndSpecialProducts.cshtml", catalogModel);
        }

        public async Task<DiscountAndSpecialProductsModel> GetDiscountAndSpecialProductsAsync(
            string type, string discountIds = null, string bpCode = null)
        {
            var pageNumber = 1;
            var pageSize = 15;

            var model = new DiscountAndSpecialProductsModel();

            // Decide the type (fallback to Discount if unclear)
            type = string.IsNullOrEmpty(type)
                ? (!string.IsNullOrEmpty(bpCode) ? "special" : "discount")
                : type;

            // Use paging logic
            var (allProducts, totalCount) = await GetPagedDiscountOrSpecialProductsAsync(type,
                discountIds, bpCode, null);

            if (!allProducts.Any())
                return model;

            // pagination on all products
            var pagedProducts = allProducts
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var productIds = string.Join(",", allProducts.Select(p => p.Id));

            // get categories by product ids
            var categories = await GetCategoryNavigationModelByProductsAsync(productIds);

            // prepare product overview model
            var productModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(pagedProducts)).ToList();

            // prepare category product model
            var catalogModel = new CatalogProductsModel
            {
                Products = productModels,
                UseAjaxLoading = _catalogSettings.UseAjaxCatalogProductsLoading,
                PageSize = pageSize,
                TotalItems = totalCount,
                PageNumber = pageNumber
            };

            catalogModel.LoadPagedList(new PagedList<Product>(pagedProducts, pageNumber - 1, pageSize, totalCount));

            model.Type = type;
            model.CategoryNavigation = categories;
            model.CatalogProducts = catalogModel;

            return model;
        }

        private async Task<(IList<Product> PagedProducts, int TotalCount)> GetPagedDiscountOrSpecialProductsAsync(string type, string discountIds,
            string bpCode, int? categoryId)
        {
            // Fetch all products first
            IList<Product> allProducts;

            if (string.IsNullOrWhiteSpace(type))
                return (Array.Empty<Product>(), 0);

            // get the current customer
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer == null || customer.Id <= 0)
                return (Array.Empty<Product>(), 0);

            if (type.Equals("special", StringComparison.OrdinalIgnoreCase))
            {
                allProducts = (await _discountService
                    .GetDiscountAndSpecialPriceProductsAsync("special", bpCode: bpCode, categoryId: categoryId))
                    .ToList();
            }
            else
            {
                allProducts = (await _discountService
                    .GetDiscountAndSpecialPriceProductsAsync("discount", discountIds: discountIds, categoryId: categoryId))
                    .ToList();
            }

            if (allProducts == null || allProducts.Count == 0)
                return (Array.Empty<Product>(), 0);

            #region product with bp code filter tpss sh

            // get products from the table
            var blockedIds = await _productsWithBPCodeService
                .GetBlockedProductIdsForCustomerAsync(customer?.BPCode);

            var blockedSet = blockedIds.ToHashSet();

            // remove the product from list
            allProducts = allProducts.Where(p => !blockedSet.Contains(p.Id)).ToList();

            #endregion

            // get the full product count
            var totalCount = allProducts.Count;

            return (allProducts, totalCount);
        }

        public async Task<CategoryNavigationModel> GetCategoryNavigationModelByProductsAsync(string productIds, int currentCategoryId = 0)
        {
            var flatCategories = await _discountService.GetProductCategoriesByProductsIdsAsync(productIds);
            var navigationModel = new CategoryNavigationModel
            {
                CurrentCategoryId = currentCategoryId,
                Categories = BuildTreeFromFlatData(flatCategories)
            };

            return navigationModel;
        }

        private static List<CategorySimpleModel> BuildTreeFromFlatData(List<CategoryNodeModel> flatData)
        {
            var groupedCategories = flatData
             .GroupBy(c => new { c.ParentCategoryId, c.ParentCategory })
             .Select(parent => new CategorySimpleModel
             {
                 Id = parent.Key.ParentCategoryId ?? 0,
                 Name = parent.Key.ParentCategory,
                 SeName = parent.FirstOrDefault()?.SeName,
                 SubCategories = parent
                     .Where(x => !string.IsNullOrEmpty(x.SubCategory))
                     .GroupBy(x => new { x.SubCategoryId, x.SubCategory })
                     .Select(sub => new CategorySimpleModel
                     {
                         Id = sub.Key.SubCategoryId ?? 0,
                         Name = sub.Key.SubCategory,
                         SeName = sub.FirstOrDefault()?.SeName,
                         SubCategories = sub
                             .Where(x => !string.IsNullOrEmpty(x.ChildCategory))
                             .GroupBy(x => new { x.ChildCategoryId, x.ChildCategory })
                             .Select(child => new CategorySimpleModel
                             {
                                 Id = child.Key.ChildCategoryId ?? 0,
                                 Name = child.Key.ChildCategory,
                                 SeName = child.FirstOrDefault()?.SeName
                             })
                             .ToList()
                     })
                     .ToList()
             })
             .ToList();

            return groupedCategories;
        }

        public async Task<DiscountAndSpecialProductsModel> GetDiscountProductsWithCustomerRoleAsync(string roleIds)
        {
            var pageNumber = 1;
            var pageSize = 15;

            var model = new DiscountAndSpecialProductsModel();

            // get all product list 
            var totalProducts = await GetPagedProductsWithCustomerRoleAsync(roleIds, null);

            if (totalProducts is null || !totalProducts.Any())
                return model;

            // get all product count
            var totalCount = totalProducts.Count;

            // Apply pagination to the full product list
            var pagedProducts = totalProducts
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var productIds = string.Join(",", totalProducts.Select(p => p.Id));

            // get categories by product ids
            var categories = await GetCategoryNavigationModelByProductsAsync(productIds);

            // prepare product overview model list
            var productModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(pagedProducts)).ToList();

            // prepare category product 
            var catalogModel = new CatalogProductsModel
            {
                Products = productModels,
                UseAjaxLoading = _catalogSettings.UseAjaxCatalogProductsLoading,
                PageSize = pageSize,
                TotalItems = totalCount,
                PageNumber = pageNumber
            };

            catalogModel.LoadPagedList(new PagedList<Product>(pagedProducts, pageNumber - 1, pageSize, totalCount));

            model.Type = "discount";
            model.CategoryNavigation = categories;
            model.CatalogProducts = catalogModel;

            return model;
        }

        private async Task<IList<Product>> GetPagedProductsWithCustomerRoleAsync(string customerRoleIds, int? categoryId)
        {
            // Fetch all products first
            IList<Product> allProducts;

            // get the current customer
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer == null || customer.Id <= 0)
                return Array.Empty<Product>();

            allProducts = await _discountService
                .GetDiscountProductsByCustomerRolesAsync(categoryId, customerRoleIds);

            if (allProducts == null || allProducts.Count == 0)
                return Array.Empty<Product>();

            #region product with bp code filter tpss sh

            // get products from the table
            var blockedIds = await _productsWithBPCodeService
                .GetBlockedProductIdsForCustomerAsync(customer?.BPCode);

            var blockedSet = blockedIds.ToHashSet();

            // remove the product from list
            allProducts = allProducts.Where(p => !blockedSet.Contains(p.Id)).ToList();

            #endregion           

            return allProducts;
        }

        protected async Task<bool> IsCustomMegaMenuEnabledAsync(string settingKey, bool defaultValue = false)
        {
            var setting = await _settingService.GetSettingAsync(settingKey);
            var rawValue = setting?.Value?.Trim('"');
            return bool.TryParse(rawValue, out var isActive) ? isActive : defaultValue;
        }

        #endregion

        #region PreOrder       

        private async Task PreparePreOrderSettingsAsync(ProductDetailsModel model)
        {          
            // get preorder settings
            var settings = (await _preOrderService.GetAllPreOrdersAsync())?
                .FirstOrDefault();

            if (settings != null)
            {
                model.MinimumLeadTime = settings.MinimumLeadTime;
                model.AllowedDeliveryDays = settings.AllowedDeliveryDays;
                model.DeliveryEndDate = settings.EndDate;
                model.ProductMinimumQuantity = settings.ProductMinimumQuantity;
                model.ProductMaximumQuantity = settings.ProductMaximumQuantity;
            }

            // get blackout dates 
            var blackoutDates = await _preOrderService.GetAllBlackoutDatesAsync();

            model.BlackoutDates = new List<BlackOutDateModel>();

            if (blackoutDates?.Any() == true)
            {
                foreach (var item in blackoutDates)
                {
                    if (!item.IsActive)
                        continue;

                    model.BlackoutDates.Add(new BlackOutDateModel
                    {
                        StartDate = item.StartDate.ToString("yyyy-MM-dd"),
                        EndDate = item.EndDate.ToString("yyyy-MM-dd"),
                        Reason = string.IsNullOrWhiteSpace(item.Reason)
                            ? "Blackout Day / Holiday"
                            : item.Reason
                    });
                }
            }
        }

        #endregion
    }
}