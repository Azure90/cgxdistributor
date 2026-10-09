using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.CustomPages;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.KnowledgeBaseArticle;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.CustomPage;
using Nop.Web.Framework.Models.Extensions;
using OfficeOpenXml;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Controllers
{
    public class CustomDocumentsController : BaseAdminController
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly ICustomDocumentService _customDocumentService;
        private readonly IPermissionService _permissionService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;

        #endregion

        #region Ctor
        public CustomDocumentsController(ILogger logger,
            IPermissionService permissionService,
            ICustomDocumentService customDocumentService,
            IDateTimeHelper dateTimeHelper,
            ICustomerService customerService,
            ILocalizationService localizationService,
            INotificationService notificationService)
        {
            _logger = logger;
            _permissionService = permissionService;
            _customDocumentService = customDocumentService;
            _dateTimeHelper = dateTimeHelper;
            _customerService = customerService;
            _localizationService = localizationService;
            _notificationService = notificationService;
        }

        #endregion

        #region Utilities

        public async Task<CustomDocumentsListModel> PrepareCustomDocumentListModelAsync(CustomDocumentSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            // Get all documents
            var documents = await _customDocumentService.GetAllCustomDocumentsAsync(searchModel.SearchText,
                searchModel.Page - 1, searchModel.PageSize);

            //prepare list model
            var model = await new CustomDocumentsListModel().PrepareToGridAsync(searchModel, documents, () =>
            {
                return documents.SelectAwait(async document =>
                {
                    // convert UTC created date to the current user's time zone
                    var createdDate = await _dateTimeHelper.ConvertToUserTimeAsync(
                        document.CreatedOnUtc,
                        DateTimeKind.Utc);

                    // fill in model values from the entity        
                    var docModel = new CustomDocumentsModel
                    {
                        Id = document.Id,
                        MainCategory = document.MainCategory,
                        Category = document.Category,
                        SubCategory = document.SubCategory,
                        FileName = document.FileName,
                        DownloadLink = document.DownloadLink,
                        VideoLink = document.VideoLink,
                        BPCode = document.BPCode,
                        DisplayOrder = document.DisplayOrder,
                        CreatedOnUtc = createdDate
                    };

                    return docModel;
                });
            });

            return model;
        }

        #endregion

        #region Methods

        public IActionResult Index()
        {
            return RedirectToAction("CustomDocumentsList");
        }

        public async Task<IActionResult> CustomDocumentsList()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTopics))
                return AccessDeniedView();

            // get localize string resource values
            var activeMenuResource = await _localizationService.GetResourceAsync("Admin.ContentManagement.CustomDocument.ActiveMenuTab");
            var megaMenuEnabledResource = await _localizationService.GetResourceAsync("Admin.ContentManagement.CustomDocument.MegaMenuEnabled");

            bool.TryParse(activeMenuResource, out var activeMenu);
            bool.TryParse(megaMenuEnabledResource, out var megaMenuEnabled);

            var resourceKey = "Admin.ContentManagement.CustomDocument.MegaMenuLabel";
            var megaMenuLabel = await _localizationService.GetResourceAsync(resourceKey);

            // assign the label value
            megaMenuLabel = string.Equals(megaMenuLabel, resourceKey, StringComparison.OrdinalIgnoreCase) ? string.Empty : megaMenuLabel;

            // prepare document model
            var searchModel = new CustomDocumentSearchModel
            {
                ActiveAccountMenu = activeMenu,
                ActiveMegaMenu = megaMenuEnabled,
                MegaMenuLabel = megaMenuLabel
            };

            //prepare page parameters
            searchModel.SetGridPageSize();

            return View(searchModel);
        }

        [HttpPost]
        public virtual async Task<IActionResult> CustomDocumentsList(CustomDocumentSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTopics))
                return await AccessDeniedDataTablesJson();

            // Get DataTables global search value
            searchModel.SearchText = Request.Form["search[value]"];

            //prepare model
            var model = await PrepareCustomDocumentListModelAsync(searchModel);

            return Json(model);
        }

        public virtual async Task<IActionResult> EditCustomDocument(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTopics))
                return AccessDeniedView();

            // Retrieve the document by id
            var document = await _customDocumentService.GetCustomDocumentByIdAsync(id);

            // Redirect back to the list if the document no longer exists
            if (document == null)
                return RedirectToAction(nameof(CustomDocumentsList));

            // Prepare the view model
            var model = new CustomDocumentsModel
            {
                Id = document.Id,
                MainCategory = document.MainCategory,
                SubCategory = document.SubCategory,
                Category = document.Category,
                FileName = document.FileName,
                DownloadLink = document.DownloadLink,
                VideoLink = document.VideoLink,
                BPCode = document.BPCode,
                DisplayOrder = document.DisplayOrder,

                // Populate BP Code dropdown
                AvailableCustomers = await PrepareAvailableBpCustomersAsync()
            };

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> EditCustomDocument(CustomDocumentsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTopics))
                return AccessDeniedView();

            // Retrieve the existing document
            var document = await _customDocumentService.GetCustomDocumentByIdAsync(model.Id);

            // The document may have been deleted by another user
            if (document == null)
                return RedirectToAction(nameof(CustomDocumentsList));

            // Redisplay the form if validation fails
            if (!ModelState.IsValid)
            {
                // Repopulate dropdowns before returning the view
                model.AvailableCustomers = await PrepareAvailableBpCustomersAsync();

                return View(model);
            }

            // Update the entity
            document.MainCategory = model.MainCategory;
            document.SubCategory = model.SubCategory;
            document.Category = model.Category;
            document.FileName = model.FileName;
            document.DownloadLink = model.DownloadLink;
            document.VideoLink = model.VideoLink;
            document.BPCode = model.BPCode;
            document.DisplayOrder = model.DisplayOrder;
            document.UpdatedOnUtc = DateTime.UtcNow;

            // Save changes
            await _customDocumentService.UpdateCustomDocumentAsync(document);

            // Display success notification
            _notificationService.SuccessNotification("Document updated successfully.");

            // Redirect back to the list page
            return RedirectToAction(nameof(CustomDocumentsList));
        }

        [HttpPost]
        public virtual async Task<IActionResult> DeleteCustomDocument(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTopics))
                return AccessDeniedView();

            //try to get a document with the specified id
            var document = await _customDocumentService.GetCustomDocumentByIdAsync(id);
            if (document == null)
                return RedirectToAction("CustomDocumentsList");

            // delete document
            await _customDocumentService.DeleteCustomDocumentAsync(document);

            return new NullJsonResult();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> AddOrUpdateDocumentSettingsAsync(bool activeBar, bool menuBar, string menuLabelText)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTopics))
                return AccessDeniedView();

            // Update localization resources
            await _localizationService.AddOrUpdateLocaleResourceAsync("Admin.ContentManagement.CustomDocument.ActiveMenuTab",
                activeBar.ToString());

            await _localizationService.AddOrUpdateLocaleResourceAsync("Admin.ContentManagement.CustomDocument.MegaMenuEnabled",
                menuBar.ToString());

            await _localizationService.AddOrUpdateLocaleResourceAsync("Admin.ContentManagement.CustomDocument.MegaMenuLabel",
                menuLabelText ?? string.Empty);

            return Json(new
            {
                success = true,
                message = "Document settings updated successfully."
            });
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

        #region Import / Export documents

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<string> ImportExcelDocumentAsync(IFormFile file)
        {
            if (file == null || file.Length <= 0)
                return "No file uploaded.";

            var fileExt = Path.GetExtension(file.FileName).ToLower();

            if (fileExt != ".xlsx" && fileExt != ".xls" && fileExt != ".csv")
                return "Upload failed, Only .xls/.xlsx/.csv files are supported.";

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var list = new List<CustomDocuments>();

                using (var stream = file.OpenReadStream())
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                    if (worksheet == null || worksheet.Dimension == null)
                        throw new Exception("The uploaded Excel file is empty.");

                    var rowCount = worksheet.Dimension.End.Row;

                    var columns = Enumerable.Range(1, worksheet.Dimension.Columns)
                        .ToDictionary(col => NormalizeHeader(worksheet.Cells[1, col].Text), col => col);

                    for (var row = 2; row <= rowCount; row++)
                    {
                        if (!int.TryParse(worksheet.Cells[row, columns[NormalizeHeader(nameof(CustomDocuments.DisplayOrder))]].Text, out var displayOrder))
                            displayOrder = 0;

                        list.Add(new CustomDocuments
                        {
                            MainCategory = worksheet.Cells[row, columns[NormalizeHeader(nameof(CustomDocuments.MainCategory))]].Text,
                            SubCategory = worksheet.Cells[row, columns[NormalizeHeader(nameof(CustomDocuments.SubCategory))]].Text,
                            Category = worksheet.Cells[row, columns[NormalizeHeader(nameof(CustomDocuments.Category))]].Text,
                            FileName = worksheet.Cells[row, columns[NormalizeHeader(nameof(CustomDocuments.FileName))]].Text,
                            DownloadLink = worksheet.Cells[row, columns[NormalizeHeader(nameof(CustomDocuments.DownloadLink))]].Text,
                            VideoLink = string.IsNullOrWhiteSpace(worksheet.Cells[row, columns[NormalizeHeader(nameof(CustomDocuments.VideoLink))]].Text) ? null
                                 : worksheet.Cells[row, columns[NormalizeHeader(nameof(CustomDocuments.VideoLink))]].Text,
                            BPCode = worksheet.Cells[row, columns[NormalizeHeader(nameof(CustomDocuments.BPCode))]].Text,
                            DisplayOrder = displayOrder,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                    }

                    // truncate the table values
                    await _customDocumentService.ClearCustomDocumentsAsync();

                    // Bulk insert values 
                    await _customDocumentService.InsertCustomDocumentsAsync(list);

                    return "File uploaded successfully.";
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Excel import failed", ex);
                return "An error occurred while importing file.";
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadExcelDocument()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var data = (await _customDocumentService.GetAllCustomDocumentsAsync())
                    ?.OrderBy(x => x.DisplayOrder)
                    .ToList();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("CustomDocuments");

                    // Headers
                    worksheet.Cells[1, 1].Value = "MainCategory";
                    worksheet.Cells[1, 2].Value = "FileName";
                    worksheet.Cells[1, 3].Value = "Category";
                    worksheet.Cells[1, 4].Value = "SubCategory";
                    worksheet.Cells[1, 5].Value = "BPCode";
                    worksheet.Cells[1, 6].Value = "DownloadLink";
                    worksheet.Cells[1, 7].Value = "VideoLink";
                    worksheet.Cells[1, 8].Value = "DisplayOrder";

                    // Style header
                    using (var range = worksheet.Cells[1, 1, 1, 8])
                    {
                        range.Style.Font.Bold = true;
                    }

                    // Data
                    if (data != null && data.Any())
                    {
                        for (var i = 0; i < data.Count; i++)
                        {
                            worksheet.Cells[i + 2, 1].Value = data[i].MainCategory ?? "";
                            worksheet.Cells[i + 2, 2].Value = data[i].FileName ?? "";
                            worksheet.Cells[i + 2, 3].Value = data[i].Category ?? "";
                            worksheet.Cells[i + 2, 4].Value = data[i].SubCategory ?? "";
                            worksheet.Cells[i + 2, 5].Value = data[i].BPCode ?? "";
                            worksheet.Cells[i + 2, 6].Value = data[i].DownloadLink ?? "";
                            worksheet.Cells[i + 2, 7].Value = data[i].VideoLink ?? "";
                            worksheet.Cells[i + 2, 8].Value = data[i].DisplayOrder;
                        }
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    var fileName = $"MyDocument-{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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

        private static string NormalizeHeader(string header)
        {
            return new string(header
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray())
                .ToLowerInvariant();
        }

        #endregion

        #endregion
    }
}
