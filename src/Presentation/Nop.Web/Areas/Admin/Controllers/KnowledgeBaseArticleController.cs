using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System;
using Nop.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Nop.Core.Domain.KnowledgeBaseArticle;
using Nop.Web.Models.KnowledgeBaseArticle;
using Microsoft.AspNetCore.Hosting;
using Nop.Services.KnowledgeBaseArticle;
using System.Collections.Generic;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Services.Configuration;

namespace Nop.Web.Areas.Admin.Controllers
{
    public class KnowledgeBaseArticleController : BaseAdminController
    {
        private readonly IKnowledgeBaseArticleService _KnowledgeBaseArticleService;
        private readonly IHostingEnvironment _env;
        private readonly ISettingService _settingService;
        public KnowledgeBaseArticleController(IKnowledgeBaseArticleService KnowledgeBaseArticleService, ISettingService settingService, IHostingEnvironment env)
        {

            _KnowledgeBaseArticleService = KnowledgeBaseArticleService;
            _settingService = settingService;
            _env = env;
        }
        public IActionResult Index()
        {
            var model = new KnowledgeBaseArticleModel();            
            model.ModelList = new List<KnowledgeBaseArticleModel>(); // Initialize the list

            var obj = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle();
            foreach (var i in obj)
            {
                var articleModel = new KnowledgeBaseArticleModel
                {
                    Id = i.Id,
                    Category = i.Category,
                    CategoryDescription = i.CategoryDescription,
                    SubCategory = i.SubCategory,
                    SubCategoryDescription = i.SubCategoryDescription,
                    Brand = i.Brand,
                    Product = i.Product,
                    ArticleTitle = i.ArticleTitle,
                    ArticleBody = i.ArticleBody,
                    Active = i.Active,
                    CustomerRoles=i.CustomerRoles,
                    CategoryIcon=i.CategoryIcon,
                    ColorCode=i.ColorCode


                };

                
                model.ModelList.Add(articleModel);
            }
            model.PagenameBanner = _settingService.GetSetting("BannerImagepath").Value;
            ViewBag.KnowledgeBasePagenameBanner = _settingService.GetSetting("KnowledgeBaseBannerImagepath").Value;
            model.KnowledgeBaseLabel = _settingService.GetSetting("KnowledgeBaseLabel").Value;
            ViewBag.KnowledgePointActive = Convert.ToBoolean(_settingService.GetSetting("KnowledgeBAseActive/Inactive").Value);

            return View(model);
        }
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<string> ImportExcelFileAsync(IFormFile file)
        {
            string message = "";
            bool hasHeader = true;
            int count = 0;

            if (file == null || file.Length <= 0)
            {
                return "No file uploaded.";
            }

            string fileExt = Path.GetExtension(file.FileName).ToLower();

            if (fileExt == ".xlsx" || fileExt == ".xls" || fileExt == ".csv")
            {
                try
                {
                    // Set the license context before using EPPlus
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (var stream = file.OpenReadStream())
                    using (var xlPackage = new ExcelPackage(stream))
                    {   
                        var worksheet = xlPackage.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            return "No worksheet found in the file.";
                        }

                        var columnCount = worksheet.Dimension.End.Column;
                        var rowCount = worksheet.Dimension.End.Row;
                        DataTable dt = new DataTable();

                        // Add columns to DataTable
                        foreach (var firstRowCell in worksheet.Cells[1, 1, 1, columnCount])
                        {
                            if (!string.IsNullOrEmpty(firstRowCell.Text))
                            {
                                dt.Columns.Add(hasHeader ? firstRowCell.Text : $"Column {firstRowCell.Start.Column}");
                                count++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        var startRow = hasHeader ? 2 : 1;

                        // Add rows to DataTable
                        for (int rowNum = startRow; rowNum <= rowCount; rowNum++)
                        {
                            var wsRow = worksheet.Cells[rowNum, 1, rowNum, count];
                            DataRow row = dt.NewRow();
                            foreach (var cell in wsRow)
                            {
                                if (!string.IsNullOrEmpty(cell.Text))
                                {
                                    row[cell.Start.Column - 1] = cell.Text;
                                }
                            }
                            dt.Rows.Add(row);
                        }

                        if (dt.Columns.Contains("Name"))
                        {
                            dt.Columns.Remove("Name");
                        }
                        if (dt.Rows.Count > 0)
                        {
                            var connection = DataSettingsManager.LoadSettings().ConnectionString;
                            var conn = new SqlConnection(connection);

                            using (var conn1 = new SqlConnection(connection))
                            {

                                {
                                    conn1.Open();
                                    var query = "truncate table [KnowledgeBaseArticle]";
                                    var result = conn.Query<string>(query);
                                    conn1.Close();
                                }
                            }

                            SqlDataAdapter da = new SqlDataAdapter();
                            conn.Open();
                            using (IDbConnection conn1 = new SqlConnection(connection))
                            {
                                //foreach (var i in dt.Rows)
                                for (var i = 0; i < dt.Rows.Count; i++)
                                {


                                    DynamicParameters ds = new DynamicParameters();
                                    ds.Add("@Category", dt.Rows[i].ItemArray[0].ToString() == "" ? null : dt.Rows[i].ItemArray[0].ToString());
                                    ds.Add("@CategoryDescription", dt.Rows[i].ItemArray[1].ToString() == "" ? null : dt.Rows[i].ItemArray[1].ToString());
                                    ds.Add("@SubCategory", dt.Rows[i].ItemArray[2].ToString() == "" ? null : dt.Rows[i].ItemArray[2].ToString());
                                    ds.Add("@SubCategoryDescription", dt.Rows[i].ItemArray[3].ToString() == "" ? null : dt.Rows[i].ItemArray[3].ToString());
                                    ds.Add("@Brand", dt.Rows[i].ItemArray[4].ToString() == "" ? null : dt.Rows[i].ItemArray[4].ToString());
                                    ds.Add("@Product", dt.Rows[i].ItemArray[5].ToString() == "" ? null : dt.Rows[i].ItemArray[5].ToString());
                                    ds.Add("@ArticleTitle", dt.Rows[i].ItemArray[6].ToString() == "" ? null : dt.Rows[i].ItemArray[6].ToString());
                                    ds.Add("@ArticleBody", dt.Rows[i].ItemArray[7].ToString() == "" ? null : dt.Rows[i].ItemArray[7].ToString());
                                    ds.Add("@CustomerRoles", dt.Rows[i].ItemArray[8].ToString() == "" ? null : dt.Rows[i].ItemArray[8].ToString());
                                    ds.Add("@CategoryIcon", dt.Rows[i].ItemArray[9].ToString() == "" ? null : dt.Rows[i].ItemArray[9].ToString());
                                    ds.Add("@ColorCode", dt.Rows[i].ItemArray[10].ToString() == "" ? null : dt.Rows[i].ItemArray[10].ToString());
                                    ds.Add("@Active", 1);
                                    var result = conn1.Query("SP_KnowledgeBaseArticle", param: ds, commandType: CommandType.StoredProcedure);
                                     


                                }
                            }
                            conn.Close();
                            message = "File uploaded successfully.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    message = $"An error occurred: {ex.Message}";
                }
            }
            else
            {
                message = "Upload failed, Only .xls/.xlsx/.csv files are supported.";
            }

            return message;
        }
        public IActionResult editKnowledgeBaseArticle(int id)
        {
            var obj = _KnowledgeBaseArticleService.GetKnowledgeBaseArticleById(id);

            var model = new Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle() ;
            
                model.Id = obj.Id;
                model.Category = obj.Category;
                model.CategoryDescription = obj.CategoryDescription;
                model.SubCategory = obj.SubCategory;
                model.SubCategoryDescription = obj.SubCategoryDescription;
                model.Brand = obj.Brand;
                model.Product = obj.Product;
                model.ArticleTitle = obj.ArticleTitle;
                model.ArticleBody = obj.ArticleBody;
                model.Active = obj.Active;
                model.CategoryIcon = obj.CategoryIcon;
                model.ColorCode = obj.ColorCode;
                model.CustomerRoles = obj.CustomerRoles;
            

            return View(model);
        }
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public IActionResult editKnowledgeBaseArticle(KnowledgeBaseArticleModel model, bool continueEditing)
        {   
                if (ModelState.IsValid)
                {

                    var rewards = new Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle
                    {
                        Id = model.Id,
                        Category = model.Category,
                        SubCategory= model.SubCategory,
                        CategoryDescription= model.CategoryDescription,
                        SubCategoryDescription = model.SubCategoryDescription,
                        Brand = model.Brand,
                        Product = model.Product,
                        ArticleTitle = model.ArticleTitle,
                        ArticleBody = model.ArticleBody,
                        Active = model.Active,
                        CategoryIcon=model.CategoryIcon,
                        ColorCode=model.ColorCode,
                        CustomerRoles =model.CustomerRoles
                    };

                    _KnowledgeBaseArticleService.UpdateKnowledgeBaseArticle(rewards);                   

                    if (continueEditing)
                        return RedirectToAction("editKnowledgeBaseArticle", new { id = rewards.Id });

                    return RedirectToAction("Index");
                }
                return RedirectToAction("Index");
            
        }
        public IActionResult Delete(int id)
        {
           

            var KnowledgeBase = _KnowledgeBaseArticleService.GetKnowledgeBaseArticleById(id);

            if (KnowledgeBase == null)
                //No promotion found with the specified id
                return RedirectToAction("Index");

            _KnowledgeBaseArticleService.DeleteKnowledgeBaseArticle(KnowledgeBase);
            

            return RedirectToAction("Index");
        }
        public ActionResult DownloadVDBfile()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var data = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle(); // Replace with your method to get data

            // Step 3: Convert Data to Excel File
            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                // Add headers (Assuming data is a list of objects)
                worksheet.Cells[1, 1].Value = "Category";
                worksheet.Cells[1, 2].Value = "CategoryDescription";
                worksheet.Cells[1, 3].Value = "SubCategory";
                worksheet.Cells[1, 4].Value = "SubCategoryDescription";
                worksheet.Cells[1, 5].Value = "Brand";
                worksheet.Cells[1, 6].Value = "Product";
                worksheet.Cells[1, 7].Value = "ArticleTitle";
                worksheet.Cells[1, 8].Value = "ArticleBody";
                worksheet.Cells[1, 9].Value = "CustomerRoles";
                worksheet.Cells[1, 10].Value = "CategoryIcon";
                worksheet.Cells[1, 11].Value = "ColorCode";
                // Add more columns as needed

                // Add data
                for (int i = 0; i < data.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = data[i].Category;
                    worksheet.Cells[i + 2, 2].Value = data[i].CategoryDescription;
                    worksheet.Cells[i + 2, 3].Value = data[i].SubCategory;
                    worksheet.Cells[i + 2, 4].Value = data[i].SubCategoryDescription;
                    worksheet.Cells[i + 2, 5].Value = data[i].Brand;
                    worksheet.Cells[i + 2, 6].Value = data[i].Product;
                    worksheet.Cells[i + 2, 7].Value = data[i].ArticleTitle;
                    worksheet.Cells[i + 2, 8].Value = data[i].ArticleBody;
                    worksheet.Cells[i + 2, 9].Value = data[i].CustomerRoles;
                    worksheet.Cells[i + 2, 10].Value = data[i].CategoryIcon;
                    worksheet.Cells[i + 2, 11].Value = data[i].ColorCode;
                    // Add more columns as needed
                }

                package.Save();
            }
            stream.Position = 0;

            // Step 4: Return the Excel File as a Download
            string excelName = $"Data-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            //return Json(new { FilePathname = filePath });
        }
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BannerImage(IFormFile BannerImage)
        {
            if (BannerImage != null)
            {
                var fileName = this.Request.Form.Files[0].FileName.Split('\\').LastOrDefault().Split('/').LastOrDefault();
                var uploads = Path.Combine(_env.WebRootPath, "uploads");

                foreach (var formFile in this.Request.Form.Files)
                {
                    if (formFile.Length > 0)
                    {
                        var filePath = Path.Combine(uploads, formFile.FileName);
                        using (var fileStream = new FileStream(Path.Combine(uploads, formFile.FileName), FileMode.Create))
                        {
                            await formFile.CopyToAsync(fileStream);
                            fileStream.Flush();
                        }
                    }
                }
                
                var setting = _settingService.GetSetting("BannerImagepath");
                setting.Value = Convert.ToString(fileName);
                _settingService.UpdateSettingAsync(setting);
                //obj.Image = fileName;
                //hasImage = true;
            }
            return RedirectToAction("Index");
        }
        [ValidateAntiForgeryToken]
        public ActionResult SetActiveInactivePriceRange(bool priceStatus)
        {
            _settingService.SetSetting("KnowledgeBAseActive/Inactive", priceStatus);
            return Json(new { Status = priceStatus });
        }
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> KnowledgeBaseBanner(IFormFile KnowledgeBaseBannerImage, string KnowledgeBaseLabel)
        {
            if (KnowledgeBaseBannerImage != null)
            {
                var fileName = this.Request.Form.Files[0].FileName.Split('\\').LastOrDefault().Split('/').LastOrDefault();
                var uploads = Path.Combine(_env.WebRootPath, "uploads");

                foreach (var formFile in this.Request.Form.Files)
                {
                    if (formFile.Length > 0)
                    {
                        var filePath = Path.Combine(uploads, formFile.FileName);
                        using (var fileStream = new FileStream(Path.Combine(uploads, formFile.FileName), FileMode.Create))
                        {
                            await formFile.CopyToAsync(fileStream);
                            fileStream.Flush();
                        }
                    }
                }

                var setting = _settingService.GetSetting("KnowledgeBaseBannerImagepath");
                setting.Value = Convert.ToString(fileName);
                _settingService.UpdateSettingAsync(setting);
                //_settingService.SetSetting("KnowledgeBaseLabel", KnowledgeBaseLabel);
               
                //obj.Image = fileName;
                //hasImage = true;
            }
            var setting1 = _settingService.GetSetting("KnowledgeBaseLabel");
            setting1.Value = Convert.ToString(KnowledgeBaseLabel);
            _settingService.UpdateSettingAsync(setting1);
            return RedirectToAction("Index");
        }

        //public async Task<IActionResult> DistributorRequestForm()
        //{
        //    var model = new KnowledgeBaseArticleModel();


        //    ViewBag.DistributorRequestBannerImage1 = _settingService.GetSetting("DistributorRequestBannerImagepath").Value;
        //    model.DistributorRequestLabel = _settingService.GetSetting("DistributorRequestBannerLabel").Value;
        //    ViewBag.DistributorRequesActive = _settingService.GetSetting("DistributorRequestActive/Inactive").Value.ToLower();

        //    return View(model);
        //}
        //[IgnoreAntiforgeryToken]
        //public async Task<IActionResult> DistributorRequestBanner(IFormFile DistributorRequestBannerImage, string DistributorRequestLabel)
        //{
        //    if (DistributorRequestBannerImage != null)
        //    {
        //        var fileName = this.Request.Form.Files[0].FileName.Split('\\').LastOrDefault().Split('/').LastOrDefault();
        //        var uploads = Path.Combine(_env.WebRootPath, "uploads");

        //        foreach (var formFile in this.Request.Form.Files)
        //        {
        //            if (formFile.Length > 0)
        //            {
        //                var filePath = Path.Combine(uploads, formFile.FileName);
        //                using (var fileStream = new FileStream(Path.Combine(uploads, formFile.FileName), FileMode.Create))
        //                {
        //                    await formFile.CopyToAsync(fileStream);
        //                    fileStream.Flush();
        //                }
        //            }
        //        }

        //        var setting = _settingService.GetSetting("DistributorRequestBannerImagepath");
        //        setting.Value = Convert.ToString(fileName);
        //        _settingService.UpdateSettingAsync(setting);
        //        //_settingService.SetSetting("KnowledgeBaseLabel", KnowledgeBaseLabel);

        //        //obj.Image = fileName;
        //        //hasImage = true;
        //    }
        //    var setting1 = _settingService.GetSetting("DistributorRequestBannerLabel");
        //    setting1.Value = Convert.ToString(DistributorRequestLabel);
        //    _settingService.UpdateSettingAsync(setting1);
        //    return RedirectToAction("Index");
        //}

        public async Task<IActionResult> DistributorRequestForm()
        {
            var model = new KnowledgeBaseArticleModel();


            ViewBag.DistributorRequestBannerImage1 = _settingService.GetSetting("DistributorRequestBannerImagepath").Value;
            model.DistributorRequestLabel = _settingService.GetSetting("DistributorRequestBannerLabel").Value;
            model.DistributorRequesturl = _settingService.GetSetting("DistributorRequesturl").Value;
            ViewBag.DistributorRequesActive = _settingService.GetSetting("DistributorRequestActive/Inactive").Value.ToLower();

            ViewBag.DistributorRequestBannerusdImage1 = _settingService.GetSetting("DistributorRequestBannerusdImagepath").Value;
            model.DistributorRequestusdLabel = _settingService.GetSetting("DistributorRequestBannerusdLabel").Value;
            model.DistributorRequestusdurl = _settingService.GetSetting("DistributorRequestusdurl").Value;
            ViewBag.DistributorRequestusdActive = _settingService.GetSetting("DistributorRequestusdActive/Inactive").Value.ToLower();

            return View(model);
        }
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DistributorRequestBanner(IFormFile DistributorRequestBannerImage, string DistributorRequestLabel, string DistributorRequesturl)
        {
            if (DistributorRequestBannerImage != null)
            {
                var fileName = this.Request.Form.Files[0].FileName.Split('\\').LastOrDefault().Split('/').LastOrDefault();
                var uploads = Path.Combine(_env.WebRootPath, "uploads");

                foreach (var formFile in this.Request.Form.Files)
                {
                    if (formFile.Length > 0)
                    {
                        var filePath = Path.Combine(uploads, formFile.FileName);
                        using (var fileStream = new FileStream(Path.Combine(uploads, formFile.FileName), FileMode.Create))
                        {
                            await formFile.CopyToAsync(fileStream);
                            fileStream.Flush();
                        }
                    }
                }

                var setting = _settingService.GetSetting("DistributorRequestBannerImagepath");
                setting.Value = Convert.ToString(fileName);
                _settingService.UpdateSettingAsync(setting);
                //_settingService.SetSetting("KnowledgeBaseLabel", KnowledgeBaseLabel);

                //obj.Image = fileName;
                //hasImage = true;
            }
            var setting1 = _settingService.GetSetting("DistributorRequestBannerLabel");
            setting1.Value = Convert.ToString(DistributorRequestLabel);
            _settingService.UpdateSettingAsync(setting1);
            var setting2 = _settingService.GetSetting("DistributorRequesturl");
            setting2.Value = Convert.ToString(DistributorRequesturl);
            _settingService.UpdateSettingAsync(setting2);
            return RedirectToAction("DistributorRequestForm");
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DistributorRequestBannerforusd(IFormFile DistributorRequestBannerusdImage, string DistributorRequestusdLabel, string DistributorRequestusdurl)
        {
            if (DistributorRequestBannerusdImage != null)
            {
                var fileName = this.Request.Form.Files[0].FileName.Split('\\').LastOrDefault().Split('/').LastOrDefault();
                var uploads = Path.Combine(_env.WebRootPath, "uploads");

                foreach (var formFile in this.Request.Form.Files)
                {
                    if (formFile.Length > 0)
                    {
                        var filePath = Path.Combine(uploads, formFile.FileName);
                        using (var fileStream = new FileStream(Path.Combine(uploads, formFile.FileName), FileMode.Create))
                        {
                            await formFile.CopyToAsync(fileStream);
                            fileStream.Flush();
                        }
                    }
                }

                var setting = _settingService.GetSetting("DistributorRequestBannerusdImagepath");
                setting.Value = Convert.ToString(fileName);
                _settingService.UpdateSettingAsync(setting);
                //_settingService.SetSetting("KnowledgeBaseLabel", KnowledgeBaseLabel);

                //obj.Image = fileName;
                //hasImage = true;
            }
            var setting1 = _settingService.GetSetting("DistributorRequestBannerusdLabel");
            setting1.Value = Convert.ToString(DistributorRequestusdLabel);
            _settingService.UpdateSettingAsync(setting1);
            var setting2 = _settingService.GetSetting("DistributorRequestusdurl");
            setting2.Value = Convert.ToString(DistributorRequestusdurl);
            _settingService.UpdateSettingAsync(setting2);
            return RedirectToAction("DistributorRequestForm");
        }

        //[ValidateAntiForgeryToken]
        //public ActionResult SetActiveInactiveDistributorRequest(bool priceStatus)
        //{
        //    //_settingService.SetSetting("DistributorRequestActive/Inactive", priceStatus);
        //    var setting1 = _settingService.GetSetting("DistributorRequestActive/Inactive");
        //    setting1.Value = Convert.ToString(priceStatus);
        //    _settingService.UpdateSettingAsync(setting1);
        //    return Json(new { Status = priceStatus });
        //}
        [ValidateAntiForgeryToken]
        public ActionResult SetActiveInactiveDistributorRequest(bool priceStatus, bool usdstatus)
        {
            //_settingService.SetSetting("DistributorRequestActive/Inactive", priceStatus);
            var finalstatus = false;
            var setting1 = _settingService.GetSetting("DistributorRequestActive/Inactive");
            setting1.Value = Convert.ToString(priceStatus);
            _settingService.UpdateSettingAsync(setting1);
            var setting2 = _settingService.GetSetting("DistributorRequestusdActive/Inactive");
            setting2.Value = Convert.ToString(usdstatus);
            _settingService.UpdateSettingAsync(setting2);
            if (priceStatus == true || usdstatus == true)
            {
                finalstatus = true;
            }
            return Json(new { Status = finalstatus });
        }
    }
}
