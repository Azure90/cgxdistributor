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
using Nop.Services.DistributorCourses;
using Nop.Web.Models.DistributorPortal;

namespace Nop.Web.Areas.Admin.Controllers
{
    public class DistributorCoursesController : BaseAdminController
    {
        private readonly IDistributorCoursesService _DistributorCoursesService;
        private readonly IHostingEnvironment _env;
        private readonly ISettingService _settingService;
        public DistributorCoursesController(IDistributorCoursesService DistributorCoursesService, ISettingService settingService, IHostingEnvironment env)
        {

            _DistributorCoursesService = DistributorCoursesService;
            _settingService = settingService;
            _env = env;
        }
        public IActionResult DistributorCoursesView()
        {

            var model = new DistributorPortaladminModel();
            model.ModelList = new List<DistributorPortaladminModel>(); // Initialize the list

            var obj = _DistributorCoursesService.GetAllDistributorCourses();
            foreach (var i in obj)
            {
                var articleModel = new DistributorPortaladminModel
                {

                    Id = i.Id,
                    MainCategory = i.MainCategory,
                    SubCategory = i.SubCategory,
                    Sku = i.Sku,
                    Category = i.Category,
                    Name = i.Name,
                    Description = i.Description,
                    ImageLink = i.ImageLink,
                    CustomerRole = i.CustomerRole,
                    ButtonLink = i.ButtonLink
                };



                model.ModelList.Add(articleModel);
            }
            //model.PagenameBanner = _settingService.GetSetting("BannerImagepath").Value;
            //ViewBag.KnowledgeBasePagenameBanner = _settingService.GetSetting("KnowledgeBaseBannerImagepath").Value;
            //model.KnowledgeBaseLabel = _settingService.GetSetting("KnowledgeBaseLabel").Value;
            //ViewBag.KnowledgePointActive = Convert.ToBoolean(_settingService.GetSetting("KnowledgeBAseActive/Inactive").Value);
         
           
            return View(model);
        }


            public IActionResult showhidelefttabView()
            {
               
                ViewBag.SocialMedia = Convert.ToBoolean(_settingService.GetSetting("SocialActive/Inactive").Value);
                ViewBag.Training = Convert.ToBoolean(_settingService.GetSetting("TrainingActive/Inactive").Value);
            ViewBag.products = Convert.ToBoolean(_settingService.GetSetting("productsactive/inactive").Value);
            ViewBag.NonSterile = Convert.ToBoolean(_settingService.GetSetting("Non-SterileActive/Inactive").Value);
            return View();
            }
        
        public ActionResult DownloadVDBfile()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var data = _DistributorCoursesService.GetAllDistributorCourses(); // Replace with your method to get data

            // Step 3: Convert Data to Excel File
            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                // Add headers (Assuming data is a list of objects)

                worksheet.Cells[1, 1].Value = "MainCategory";
                worksheet.Cells[1, 2].Value = "SubCategory";
                worksheet.Cells[1, 3].Value = "Sku";
                worksheet.Cells[1, 4].Value = "Category";
                worksheet.Cells[1, 5].Value = "Name";
                worksheet.Cells[1, 6].Value = "Description";
                worksheet.Cells[1, 7].Value = "ImageLink";
                worksheet.Cells[1, 8].Value = "CustomerRole";
                worksheet.Cells[1, 9].Value = "ButtonLink";
            
                // Add more columns as needed

                // Add data
                for (int i = 0; i < data.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = data[i].MainCategory;
                    worksheet.Cells[i + 2, 2].Value = data[i].SubCategory;
                    worksheet.Cells[i + 2, 3].Value = data[i].Sku;
                    worksheet.Cells[i + 2, 4].Value = data[i].Category;
                    worksheet.Cells[i + 2, 5].Value = data[i].Name;
                    worksheet.Cells[i + 2, 6].Value = data[i].Description;
                    worksheet.Cells[i + 2, 7].Value = data[i].ImageLink;
                    worksheet.Cells[i + 2, 8].Value = data[i].CustomerRole;
                    worksheet.Cells[i + 2, 9].Value = data[i].ButtonLink;
                  
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

                        //if (dt.Columns.Contains("Name"))
                        //{
                        //    dt.Columns.Remove("Name");
                        //}
                        if (dt.Rows.Count > 0)
                        {
                            var connection = DataSettingsManager.LoadSettings().ConnectionString;
                            var conn = new SqlConnection(connection);

                            using (var conn1 = new SqlConnection(connection))
                            {

                                {
                                    conn1.Open();
                                    var query = "truncate table [DistributorCourses]";
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
                                    if (!string.IsNullOrEmpty(dt.Rows[i].ItemArray[0].ToString()) && !string.IsNullOrEmpty(dt.Rows[i].ItemArray[1].ToString()))
                                    {
                                        DynamicParameters ds = new DynamicParameters();
                                        ds.Add("@MainCategory", dt.Rows[i].ItemArray[0].ToString() == "" ? null : dt.Rows[i].ItemArray[0].ToString());
                                        ds.Add("@SubCategory", dt.Rows[i].ItemArray[1].ToString() == "" ? null : dt.Rows[i].ItemArray[1].ToString());
                                        ds.Add("@Sku", dt.Rows[i].ItemArray[2].ToString() == "" ? null : dt.Rows[i].ItemArray[2].ToString());
                                        ds.Add("@Category", dt.Rows[i].ItemArray[3].ToString() == "" ? null : dt.Rows[i].ItemArray[3].ToString());
                                        ds.Add("@Name", dt.Rows[i].ItemArray[4].ToString() == "" ? null : dt.Rows[i].ItemArray[4].ToString());
                                        ds.Add("@Description", dt.Rows[i].ItemArray[5].ToString() == "" ? null : dt.Rows[i].ItemArray[5].ToString());
                                        ds.Add("@ImageLink", dt.Rows[i].ItemArray[6].ToString() == "" ? null : dt.Rows[i].ItemArray[6].ToString());
                                        ds.Add("@CustomerRole", dt.Rows[i].ItemArray[7].ToString() == "" ? null : dt.Rows[i].ItemArray[7].ToString());
                                        ds.Add("@ButtonLink", dt.Rows[i].ItemArray[8].ToString() == "" ? null : dt.Rows[i].ItemArray[8].ToString());

                                        var result = conn1.Query("SP_InsertDistributorCourses", param: ds, commandType: CommandType.StoredProcedure);
                                    }


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
        public IActionResult editDistributorPortal(int id)
        {
            var obj = _DistributorCoursesService.GetDistributorCoursesById(id);

            var model = new DistributorPortaladminModel();
            
            model.Id = obj.Id;
            model.MainCategory = obj.MainCategory;
            model.SubCategory = obj.SubCategory;
            model.Sku = obj.Sku;
            model.Category = obj.Category;
            model.Name = obj.Name;
            model.Description = obj.Description;
            model.ImageLink = obj.ImageLink;
            model.CustomerRole = obj.CustomerRole;
            model.ButtonLink = obj.ButtonLink;
           


            return View(model);
        }
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public IActionResult editDistributorPortal(DistributorPortaladminModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {

                var rewards = new Nop.Core.Domain.DistributorCourses.DistributorCourses
                {
                    Id = model.Id,
                    MainCategory = model.MainCategory,
                    SubCategory = model.SubCategory,
                    Sku = model.Sku,
                    Category = model.Category,
                    Name = model.Name,
                    Description = model.Description,
                    ImageLink = model.ImageLink,
                    CustomerRole = model.CustomerRole,
                    ButtonLink = model.ButtonLink,
                   
                };

                _DistributorCoursesService.UpdateDistributorCourses(rewards);

                if (continueEditing)
                    return RedirectToAction("editDistributorPortal", new { id = rewards.Id });

                return RedirectToAction("DistributorCoursesView");
            }
            return RedirectToAction("DistributorCoursesView");

        }
        public IActionResult Delete(int id)
        {


            var DistributorCourses = _DistributorCoursesService.GetDistributorCoursesById(id);

            if (DistributorCourses == null)
                //No promotion found with the specified id
                return RedirectToAction("DistributorCoursesView");

            _DistributorCoursesService.DeleteDistributorCourses(DistributorCourses);


            return RedirectToAction("DistributorCoursesView");
        }
        [ValidateAntiForgeryToken]
        public ActionResult SocialactiveInactive(bool priceStatus)
        {
            _settingService.SetSetting("SocialActive/Inactive", priceStatus);
            return Json(new { Status = priceStatus });
        }
        [ValidateAntiForgeryToken]
        public ActionResult TrainingactiveInactive(bool priceStatus)
        {
            _settingService.SetSetting("TrainingActive/Inactive", priceStatus);
            return Json(new { Status = priceStatus });
        } 
        [ValidateAntiForgeryToken]
        public ActionResult productsactiveinactive(bool priceStatus)
        {
            _settingService.SetSetting("productsactive/inactive", priceStatus);
            return Json(new { Status = priceStatus });
        } 
        [ValidateAntiForgeryToken]
        public ActionResult NonSterileactiveInactive(bool priceStatus)
        {
            _settingService.SetSetting("Non-SterileActive/Inactive", priceStatus);
            return Json(new { Status = priceStatus });
        }
    }
}
