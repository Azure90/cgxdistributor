using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders.PreOrders;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Orders.PreOrders;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.ModelBinding;
using OfficeOpenXml;
using Nop.Core.Domain.Customers;
using Newtonsoft.Json;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class PreOrderController : BaseAdminController
    {

        #region Fields

        private readonly ILogger _logger;
        private readonly IStoreContext _storeContext;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IPreOrderService _preOrderService;
        private readonly ICustomerService _customerService;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IProductService _productService;

        #endregion

        #region Ctor

        public PreOrderController(ILogger logger,
            IStoreContext storeContext,
            IDateTimeHelper dateTimeHelper,
            ICustomerService customerService,
            IPreOrderService preOrderService,
            IPermissionService permissionService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IMessageTemplateService messageTemplateService,
            IProductService productService)
        {
            _logger = logger;
            _storeContext = storeContext;
            _dateTimeHelper = dateTimeHelper;
            _customerService = customerService;
            _preOrderService = preOrderService;
            _permissionService = permissionService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _messageTemplateService = messageTemplateService;
            _productService = productService;
        }

        #endregion

        #region Utilities 

        public async Task<PreOrderModel> PreparePreOrderModelAsync()
        {
            var preOrder = (await _preOrderService.GetAllPreOrdersAsync())
                .FirstOrDefault();

            if (preOrder == null)
                return new PreOrderModel();

            var createdOn = await _dateTimeHelper.ConvertToUserTimeAsync(
                preOrder.CreatedOnUtc, DateTimeKind.Utc);

            return new PreOrderModel
            {
                Id = preOrder.Id,

                SelectedCustomerRoleIds = string.IsNullOrWhiteSpace(preOrder.CustomerRoleIds)
                     ? new List<int>() : preOrder.CustomerRoleIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(int.Parse).ToList(),

                ProductIds = preOrder.ProductIds,
                ProductSkus = preOrder.ProductSkus,
                CategoryIds = preOrder.CategoryIds,
                MinimumLeadTime = preOrder.MinimumLeadTime,
                AllowedDeliveryDays = preOrder.AllowedDeliveryDays,
                DeliveryEndDate = preOrder.DeliveryEndDate,
                EndDate = preOrder.EndDate,
                ProductMinimumQuantity = preOrder.ProductMinimumQuantity,
                ProductMaximumQuantity = preOrder.ProductMaximumQuantity,
                CreatedOnUtc = createdOn
            };
        }

        public virtual async Task<BlackoutDateListModel> PrepareBlackoutDateListModelAsync(BlackoutDateSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            // Get blackout dates
            var blackoutDates = await _preOrderService.GetAllBlackoutDatesAsync(searchModel.Page - 1,
                searchModel.PageSize);

            // Prepare list model
            var model = new BlackoutDateListModel().PrepareToGrid(searchModel, blackoutDates, () =>
            {
                return blackoutDates.Select(blackoutDate =>
                {
                    return new BlackoutDateModel
                    {
                        Id = blackoutDate.Id,
                        StartDate = blackoutDate.StartDate.Date,
                        EndDate = blackoutDate.EndDate.Date,
                        Reason = blackoutDate.Reason,
                        IsActive = blackoutDate.IsActive,
                        CreatedOnUtc = blackoutDate.CreatedOnUtc
                    };
                });
            });

            return model;
        }

        public async Task<PreOrderReminderSettingsModel> PreparePreOrderReminderSettingsModelAsync()
        {
            var model = new PreOrderReminderSettingsModel();

            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();

            // Get reminder configuration
            var reminderSettings = await _preOrderService.GetPreOrderReminderSettingsAsync();

            // Prepare available message templates
            model.AvailableMessageTemplates = await PrepareAvailableMessageTemplatesAsync(storeId);

            // No configuration exists yet
            if (reminderSettings == null)
                return model;

            // Populate the model
            model.Id = reminderSettings.Id;

            model.InitialReminderTemplateId = reminderSettings.InitialReminderTemplateId;
            model.InitialReminderDays = reminderSettings.InitialReminderDays;
            model.InitialReminderActive = reminderSettings.InitialReminderActive;

            model.EditableReminderTemplateId = reminderSettings.EditableReminderTemplateId;
            model.EditableReminderDays = reminderSettings.EditableReminderDays;
            model.EditableReminderActive = reminderSettings.EditableReminderActive;

            model.FinalReminderTemplateId = reminderSettings.FinalReminderTemplateId;
            model.FinalReminderDays = reminderSettings.FinalReminderDays;
            model.FinalReminderActive = reminderSettings.FinalReminderActive;

            model.DeliveryReminderTemplateId = reminderSettings.DeliveryReminderTemplateId;
            model.DeliveryReminderDays = reminderSettings.DeliveryReminderDays;
            model.DeliveryReminderActive = reminderSettings.DeliveryReminderActive;

            return model;
        }

        private async Task PreparePreOrderResourcesAsync(PreOrderResourcesModel model)
        {
            model.RegularOrderTitle = await _localizationService
                .GetResourceAsync("Account.PreOrders.RegularOrder.Title");

            model.RegularOrderShortDescription = await _localizationService
                .GetResourceAsync("Account.PreOrders.RegularOrder.ShortDescription");

            model.PreOrderTitle = await _localizationService
                .GetResourceAsync("Account.PreOrders.PreOrder.Title");

            model.PreOrderShortDescription = await _localizationService
                .GetResourceAsync("Account.PreOrders.PreOrder.ShortDescription");

            model.PreOrderDescription = await _localizationService
                .GetResourceAsync("Account.PreOrders.PreOrder.Description");

            model.PreOrderNotAvailableMessage = await _localizationService
                .GetResourceAsync("ShoppingCart.PreOrder.NotAvailable");

            model.PreOrderNoLongerAvailableMessage = await _localizationService
                .GetResourceAsync("ShoppingCart.PreOrder.NoLongerAvailable");

            model.RegularToPreOrderCartMessage = await _localizationService
                .GetResourceAsync("ShoppingCart.PreOrder.CannotAddRegularToPreOrderCart");

            model.PreOrderToRegularCartMessage = await _localizationService
                .GetResourceAsync("ShoppingCart.Preorder.CannotAddPreOrderToRegularCart");

            model.PreOrderPreferredDateMessage = await _localizationService
                .GetResourceAsync("Shoppingcart.Preorder.PreferredDeliveryDateRequired");
        }

        protected virtual async Task<IList<SelectListItem>> PrepareAvailableCustomerRolesAsync()
        {
            var customerRoles = await _customerService.GetAllCustomerRolesAsync(true);

            return customerRoles.Select(role => new SelectListItem
            {
                Text = role.Name,
                Value = role.Id.ToString()
            }).ToList();
        }

        protected virtual async Task<IList<SelectListItem>> PrepareAvailableMessageTemplatesAsync(int storeId)
        {
            var availableMessageTemplates = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = "Select Message Template",
                    Value = "0"
                }
            };

            var messageTemplates = await _messageTemplateService.GetAllMessageTemplatesAsync(storeId, isActive: true);

            availableMessageTemplates.AddRange(messageTemplates.Select(messageTemplate => new SelectListItem
            {
                Text = messageTemplate.Name,
                Value = messageTemplate.Id.ToString()
            }));

            return availableMessageTemplates;
        }

        private async Task PreparePreOrderPageModelAsync(PreOrderModel model)
        {
            model.AvailableCustomerRoles = await PrepareAvailableCustomerRolesAsync();

            // Prepare reminder model
            model.ReminderSettings = await PreparePreOrderReminderSettingsModelAsync();

            // Prepare blackout date search model
            var searchModel = new BlackoutDateSearchModel();
            searchModel.SetGridPageSize();

            model.BlackoutDateSearchModel = searchModel;
        }

        #endregion

        #region Methods

        #region Pre Order list

        public IActionResult Index()
        {
            return RedirectToAction("AddOrUpdatePreOrderDetails");
        }

        public virtual async Task<IActionResult> AddOrUpdatePreOrderDetails()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            // get localize string resource values
            var activeMenuResource = await _localizationService
                .GetResourceAsync("Admin.Catalog.PreOrders.PreOrderActiveInActive");

            bool.TryParse(activeMenuResource, out var activeMenu);

            // prepare pre order model
            var model = await PreparePreOrderModelAsync();

            model.IsPreOrderEnabled = activeMenu;

            await PreparePreOrderPageModelAsync(model);

            // prepare resource model
            await PreparePreOrderResourcesAsync(model.ResourcesModel);

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddOrUpdatePreOrderDetails(PreOrderModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            if (!ModelState.IsValid)
            {
                await PreparePreOrderPageModelAsync(model);

                return View(model);
            }

            // Update localization resource
            await _localizationService.AddOrUpdateLocaleResourceAsync("Admin.Catalog.PreOrders.PreOrderActiveInActive",
                model.IsPreOrderEnabled.ToString());            

            var preOrder = await _preOrderService.GetPreOrderByIdAsync(model.Id);

            IList<Product> products = new List<Product>();

            // Only reload products if this is a new record
            if (preOrder == null || !AreSkuSetsEqual(preOrder.ProductSkus, model.ProductSkus))
            {
                var skuArray = (model.ProductSkus ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                products = (await _productService.GetProductsBySkuAsync(skuArray))
                    .Where(p => p.Published && !p.Deleted)
                    .ToList();

                // Validate SKUs
                if (products.Count != skuArray.Length)
                {
                    ModelState.AddModelError(nameof(model.ProductSkus),
                        "One or more product SKUs are invalid.");

                    await PreparePreOrderPageModelAsync(model);
                    return View(model);
                }
            }

            if (preOrder == null)
            {
                preOrder = new PreOrder
                {
                    CreatedOnUtc = DateTime.UtcNow
                };

                MapPreOrderAsync(preOrder, model, products);

                await _preOrderService.InsertPreOrderAsync(preOrder);
            }
            else
            {
                MapPreOrderAsync(preOrder, model, products);

                preOrder.UpdatedOnUtc = DateTime.UtcNow;

                await _preOrderService.UpdatePreOrderAsync(preOrder);
            }

            _notificationService.SuccessNotification("PreOrder details saved successfully.");

            return RedirectToAction(nameof(AddOrUpdatePreOrderDetails));
        }

        private static void MapPreOrderAsync(PreOrder preOrder, PreOrderModel model, IList<Product> products)
        {
            preOrder.CustomerRoleIds = model.SelectedCustomerRoleIds != null
                ? string.Join(",", model.SelectedCustomerRoleIds)
                : string.Empty;

            var finalEnddate = model.DeliveryEndDate.Value
                .AddDays(-model.MinimumLeadTime);

            if (products != null && products.Any())
            {
                preOrder.ProductIds = string.Join(",", products.Select(p => p.Id));
                preOrder.ProductSkus = string.Join(",", products.Select(p => p.Sku));
            }

            preOrder.CategoryIds = model.CategoryIds;
            preOrder.MinimumLeadTime = model.MinimumLeadTime;
            preOrder.AllowedDeliveryDays = model.AllowedDeliveryDays;
            preOrder.DeliveryEndDate = model.DeliveryEndDate.Value;
            preOrder.EndDate = finalEnddate;
            preOrder.ProductMinimumQuantity = model.ProductMinimumQuantity;
            preOrder.ProductMaximumQuantity = model.ProductMaximumQuantity;
        }

        #endregion

        #region Blackout Dates


        [HttpPost]
        public virtual async Task<IActionResult> BlackoutDateList(BlackoutDateSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return await AccessDeniedDataTablesJson();

            var model = await PrepareBlackoutDateListModelAsync(searchModel);

            return Json(model);
        }


        [HttpPost]
        public virtual async Task<IActionResult> CreateBlackoutDate(BlackoutDateModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            // Validate required fields
            if (!model.StartDate.HasValue)
            {
                ModelState.AddModelError(nameof(model.StartDate),
                    "Please select a start date.");
            }

            // Validate date range
            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                var startDate = model.StartDate.Value.Date;
                var endDate = model.EndDate.Value.Date;

                if (startDate > endDate)
                {
                    ModelState.AddModelError(nameof(model.StartDate),
                        "Start date must be less than or equal to the end date.");

                    ModelState.AddModelError(nameof(model.EndDate),
                        "End date must be greater than or equal to the start date.");
                }
            }

            if (!ModelState.IsValid)
                return ErrorJson(ModelState.SerializeErrors());

            // Use the current UTC date as a fallback value
            var currentDate = DateTime.UtcNow.Date;

            // Create the blackout date entity
            var blackoutDate = new BlackoutDates
            {
                StartDate = model.StartDate ?? currentDate,
                EndDate = model.EndDate ?? currentDate,
                Reason = model.Reason,
                IsActive = true,
                CreatedOnUtc = DateTime.UtcNow
            };

            // Save the blackout date
            await _preOrderService.InsertBlackoutDateAsync(blackoutDate);

            return Json(new { Result = true });
        }

        [HttpPost]
        public virtual async Task<IActionResult> DeleteBlackoutDate(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var blackoutDate = await _preOrderService.GetBlackoutDateByIdAsync(id);

            if (blackoutDate == null)
                return RedirectToAction(nameof(AddOrUpdatePreOrderDetails));

            await _preOrderService.DeleteBlackoutDateAsync(blackoutDate);

            return new NullJsonResult();
        }

        #endregion

        #region Pre Order Reminder Templates

        [HttpPost]
        public virtual async Task<IActionResult> UpdatePreOrderReminderSettings(PreOrderReminderSettingsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return ErrorJson(ModelState.SerializeErrors());

            // Get existing reminder settings
            var reminderSettings = await _preOrderService.GetPreOrderReminderSettingsAsync();

            if (reminderSettings == null)
            {
                // Create a new reminder settings record
                reminderSettings = new PreOrderReminderSettings
                {
                    InitialReminderTemplateId = model.InitialReminderTemplateId,
                    InitialReminderDays = model.InitialReminderDays,
                    InitialReminderActive = model.InitialReminderActive,

                    EditableReminderTemplateId = model.EditableReminderTemplateId,
                    EditableReminderDays = model.EditableReminderDays,
                    EditableReminderActive = model.EditableReminderActive,

                    FinalReminderTemplateId = model.FinalReminderTemplateId,
                    FinalReminderDays = model.FinalReminderDays,
                    FinalReminderActive = model.FinalReminderActive,

                    DeliveryReminderTemplateId = model.DeliveryReminderTemplateId,
                    DeliveryReminderDays = model.DeliveryReminderDays,
                    DeliveryReminderActive = model.DeliveryReminderActive,

                    CreatedOnUtc = DateTime.UtcNow
                };

                await _preOrderService.InsertPreOrderReminderSettingsAsync(reminderSettings);
            }
            else
            {
                // Update reminder settings
                reminderSettings.InitialReminderTemplateId = model.InitialReminderTemplateId;
                reminderSettings.InitialReminderDays = model.InitialReminderDays;
                reminderSettings.InitialReminderActive = model.InitialReminderActive;

                reminderSettings.EditableReminderTemplateId = model.EditableReminderTemplateId;
                reminderSettings.EditableReminderDays = model.EditableReminderDays;
                reminderSettings.EditableReminderActive = model.EditableReminderActive;

                reminderSettings.FinalReminderTemplateId = model.FinalReminderTemplateId;
                reminderSettings.FinalReminderDays = model.FinalReminderDays;
                reminderSettings.FinalReminderActive = model.FinalReminderActive;

                reminderSettings.DeliveryReminderTemplateId = model.DeliveryReminderTemplateId;
                reminderSettings.DeliveryReminderDays = model.DeliveryReminderDays;
                reminderSettings.DeliveryReminderActive = model.DeliveryReminderActive;

                reminderSettings.UpdatedOnUtc = DateTime.UtcNow;

                await _preOrderService.UpdatePreOrderReminderSettingsAsync(reminderSettings);
            }

            return Json(new { Result = true });
        }

        #endregion

        #region PreOrder Resources '

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePreOrderResources(PreOrderResourcesModel model)
        {  
            await UpdateLocaleResourceAsync("Account.PreOrders.RegularOrder.Title",
                model.RegularOrderTitle);

            await UpdateLocaleResourceAsync("Account.PreOrders.RegularOrder.ShortDescription",
                model.RegularOrderShortDescription);

            await UpdateLocaleResourceAsync("Account.PreOrders.PreOrder.Title",
                model.PreOrderTitle);

            await UpdateLocaleResourceAsync("Account.PreOrders.PreOrder.ShortDescription",
                model.PreOrderShortDescription);

            await UpdateLocaleResourceAsync("Account.PreOrders.PreOrder.Description",
                model.PreOrderDescription);

            await UpdateLocaleResourceAsync("ShoppingCart.PreOrder.NotAvailable",
                model.PreOrderNotAvailableMessage);

            await UpdateLocaleResourceAsync("ShoppingCart.PreOrder.NoLongerAvailable",
                model.PreOrderNoLongerAvailableMessage);

            await UpdateLocaleResourceAsync("ShoppingCart.PreOrder.CannotAddRegularToPreOrderCart",
                model.RegularToPreOrderCartMessage);

            await UpdateLocaleResourceAsync("ShoppingCart.Preorder.CannotAddPreOrderToRegularCart",
                model.PreOrderToRegularCartMessage);

            await UpdateLocaleResourceAsync("Shoppingcart.Preorder.PreferredDeliveryDateRequired",
                model.PreOrderPreferredDateMessage);

            return Json(new
            {
                success = true
            });
        }

        private async Task UpdateLocaleResourceAsync(string resourceKey, string value)
        {
            await _localizationService.AddOrUpdateLocaleResourceAsync(resourceKey,
                value?.Trim() ?? string.Empty);
        }

        #endregion

        #region Import / Export documents

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<string> ImportExcelDocumentAsync(IFormFile file, bool includeProducts)
        {
            if (file == null || file.Length <= 0)
                return "No file uploaded.";

            var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (fileExt != ".xlsx" && fileExt != ".xls" && fileExt != ".csv")
                return "Upload failed. Only .xls/.xlsx/.csv files are supported.";

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var orders = new List<PreOrder>();

                using (var stream = file.OpenReadStream())
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                    if (worksheet == null || worksheet.Dimension == null)
                        throw new Exception("The uploaded Excel file is empty.");

                    var columns = Enumerable.Range(1, worksheet.Dimension.Columns)
                        .ToDictionary(col => NormalizeHeader(worksheet.Cells[1, col].Text), col => col);

                    var rowCount = worksheet.Dimension.End.Row;

                    // Load all published, non-deleted products once
                    var allProducts = (await _productService.SearchProductsAsync(showHidden: false))
                        .Where(p => p.Published && !p.Deleted)
                        .ToList();

                    // Load all customer roles once
                    var allRoles = await _customerService.GetAllCustomerRolesAsync(showHidden: false);

                    // Load preorder settings once
                    var settings = await _preOrderService.GetAllPreOrdersAsync();
                    var deliveryEndDate = settings.FirstOrDefault()?.DeliveryEndDate;

                    for (var row = 2; row <= rowCount; row++)
                    {
                        var skuText = worksheet.Cells[row, columns[NormalizeHeader(nameof(PreOrderModel.ProductSkus))]].Text;

                        var excelSkus = (skuText ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        if (!excelSkus.Any())
                            continue;

                        IList<Product> products;

                        if (includeProducts)
                        {
                            // create json object
                            var skuJson = JsonConvert.SerializeObject(excelSkus);

                            // Include only the products listed in this Excel row
                            //products = allProducts
                            //    .Where(p => excelSkus.Contains(p.Sku))
                            //    .ToList();


                            products = await _productService.GetPreOrderProductsBySkusAsync(skuJson);
                        }
                        else
                        {
                            // Include all published products except those listed in this Excel row
                            products = allProducts
                                .Where(p => !excelSkus.Contains(p.Sku))
                                .ToList();
                        }

                        if (!products.Any())
                            continue;

                        orders.Add(new PreOrder
                        {
                            CustomerRoleIds = GetCustomerRoleIds(worksheet.Cells[row, columns[NormalizeHeader(nameof(PreOrderModel.CustomerRoles))]].Text, allRoles),

                            ProductIds = string.Join(",", products.Select(p => p.Id)),
                            ProductSkus = string.Join(",", products.Select(p => p.Sku)),

                            MinimumLeadTime = GetIntValue(worksheet.Cells[row, columns[NormalizeHeader(nameof(PreOrderModel.MinimumLeadTime))]].Text),
                            ProductMinimumQuantity = GetIntValue(worksheet.Cells[row, columns[NormalizeHeader(nameof(PreOrderModel.ProductMinimumQuantity))]].Text),
                            ProductMaximumQuantity = GetIntValue(worksheet.Cells[row, columns[NormalizeHeader(nameof(PreOrderModel.ProductMaximumQuantity))]].Text),

                            DeliveryEndDate = deliveryEndDate ?? new DateTime(DateTime.UtcNow.Year, 12, 31),
                            CreatedOnUtc = DateTime.UtcNow
                        });
                    }

                    if (!orders.Any())
                        return "No matching products were found.";

                    await _preOrderService.ClearPreOrdersAsync();
                    await _preOrderService.InsertPreOrdersAsync(orders);

                    return "File uploaded successfully.";
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Excel import failed", ex);
                return "An error occurred while importing the file.";
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadExcelDocument()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var data = (await _preOrderService.GetAllPreOrdersAsync()).ToList();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("PreOrderDocument");

                    // Headers
                    worksheet.Cells[1, 1].Value = "CustomerRoles";
                    worksheet.Cells[1, 2].Value = "ProductSkus";
                    worksheet.Cells[1, 3].Value = "MinimumLeadTime";
                    worksheet.Cells[1, 4].Value = "ProductMinimumQuantity";
                    worksheet.Cells[1, 5].Value = "ProductMaximumQuantity";

                    // Style header
                    using (var range = worksheet.Cells[1, 1, 1, 5])
                    {
                        range.Style.Font.Bold = true;
                    }

                    // Load roles once
                    var allRoles = await _customerService.GetAllCustomerRolesAsync(showHidden: false);

                    // Data
                    if (data != null && data.Any())
                    {
                        for (var i = 0; i < data.Count; i++)
                        {
                            worksheet.Cells[i + 2, 1].Value = GetCustomerRoleNames(data[i].CustomerRoleIds, allRoles);
                            worksheet.Cells[i + 2, 2].Value = data[i].ProductSkus ?? "";

                            worksheet.Cells[i + 2, 3].Value = data[i].MinimumLeadTime;
                            worksheet.Cells[i + 2, 4].Value = data[i].ProductMinimumQuantity;
                            worksheet.Cells[i + 2, 5].Value = data[i].ProductMaximumQuantity;
                        }

                        // Apply wrapping once after writing all rows
                        worksheet.Cells[2, 1, data.Count + 1, 2].Style.WrapText = true;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    var fileName = $"PreOrder-{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                // log the exception
                await _logger.ErrorAsync("An error occurred while downloading the Excel document.", ex);

                // error notification
                _notificationService.ErrorNotification("An error occurred while downloading the excel(xls) document.");

                // retunr to list
                return RedirectToAction("CustomDocumentsList");
            }
        }

        private static bool AreSkuSetsEqual(string existingSkus, string newSkus)
        {
            var existing = (existingSkus ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var incoming = (newSkus ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return existing.SetEquals(incoming);
        }

        private static string GetCustomerRoleIds(string roleNames, IList<CustomerRole> allRoles)
        {
            if (string.IsNullOrWhiteSpace(roleNames))
                return string.Empty;

            var names = roleNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var roleIds = allRoles
                .Where(r => names.Contains(r.Name))
                .Select(r => r.Id);

            return string.Join(",", roleIds);
        }

        private static string GetCustomerRoleNames(string roleIds, IList<CustomerRole> allRoles)
        {
            if (string.IsNullOrWhiteSpace(roleIds))
                return string.Empty;

            var ids = roleIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var id) ? id : (int?)null)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToHashSet();

            var roleNames = allRoles
                .Where(r => ids.Contains(r.Id))
                .Select(r => r.Name);

            return string.Join(",", roleNames);
        }

        private static string NormalizeHeader(string header)
        {
            return new string(header
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray())
                .ToLowerInvariant();
        }

        public static int GetIntValue(string value, int defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            return int.TryParse(value.Trim(), out var result)
                ? result
                : defaultValue;
        }

        #endregion

        #endregion
    }
}
