using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.CustomPages;
using Nop.Data;
using Nop.Services.KnowledgeBaseArticle;
using Nop.Web.Models.KnowledgeBase;
using Nop.Web.Models.KnowledgeBaseArticle;
using Nop.Services.Customers;
using Nop.Services.Configuration;
using System.Text.RegularExpressions;
using Nop.Core.Domain.Customers;

namespace Nop.Web.Controllers
{
    public class KnowledgeBaseController : BasePublicController
    {
        #region Fields

        private readonly IRepository<TblCustomPages> _customPageRepository;
        private readonly IRepository<TblCustomPageContent> _customPageContentRepository;
        private readonly IRepository<TblSubTitle> _tblSubTitleRepository;
        private readonly IWorkContext _workContext;
        private readonly IKnowledgeBaseArticleService _KnowledgeBaseArticleService;
        private readonly ICustomerService _customerService;
        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public KnowledgeBaseController(IWorkContext workContext, IRepository<TblCustomPages> customPageRepository,
            IRepository<TblCustomPageContent> customPageContentRepository, ISettingService settingService, ICustomerService customerService, IRepository<TblSubTitle> tblSubTitleRepository, IKnowledgeBaseArticleService knowledgeBaseArticleService)
        {
            _workContext = workContext;
            _customPageRepository = customPageRepository;
            _tblSubTitleRepository = tblSubTitleRepository;
            _customPageContentRepository = customPageContentRepository;
            _KnowledgeBaseArticleService = knowledgeBaseArticleService;
            _customerService = customerService;
            _settingService = settingService;
        }

        #endregion

        #region Methods 

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> Index(string pageNameID, string pagesubcategory, string subtitleId, int qid)
        {
            try
            {

                var languageid = (await _workContext.GetWorkingLanguageAsync()).Id;
                var customerrole = _workContext.GetCurrentCustomerAsync().Result;
                var GetallRoles = _customerService.GetCustomerRolesAsync(customerrole).Result.Where(d => d.Name == "CAD B2C"
                   || d.Name == "USD B2C"
                   || d.Name == "USD B2B INTL"
                   || d.Name == "CAD B2C HS"
                   || d.Name == "USD B2B ST"
                   || d.Name == "USD B2B DS"
                   || d.Name == "CAD B2B ST"
                   || d.Name == "CAD B2B DS"
                   ).FirstOrDefault();


                var data = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().ToList();
                var alldata = new List<Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle>();
                foreach (var j in data)
                {
                    var splitedroles = j.CustomerRoles.Split(",");
                    foreach (var k in splitedroles)
                    {
                        if (k.Trim() == GetallRoles.Name)
                        {
                            alldata.Add(j);
                        }
                    }
                }
                var model = new KnowledgeBaseArticleModel();
                model.Categorylist = alldata.Select(x => x.Category).Distinct().ToList();
                model.subcategoryModelList = alldata
                                             .Where(x => model.Categorylist.Contains(x.Category))
                                             .Select(x => new subcategorylist
                                             {
                                                 id = x.Id,
                                                 Category = x.Category,
                                                 subcategory = x.SubCategory,
                                                 product = x.Product,
                                                 Question = x.ArticleTitle,
                                                 Answer = x.ArticleBody
                                             })
                                             .GroupBy(p => p.subcategory)
                                             .SelectMany(g => g) // Adjust if you need all items from the group or just the first one
                                             .ToList();

                if (pageNameID == null)
                {
                    var page = alldata.GroupBy(p => p.Category).Select(g => g.First()).FirstOrDefault();
                    model.Pagename = page.Category;
                    model.PagenameDescription = page.CategoryDescription;
                    model.SubCategoryPagename = page.SubCategory;
                    model.ProductPagename = page.Product;
                    //model.PagenameBanner = model.MainMenuList.FirstOrDefault().Image;
                    //model.Id = model.MainMenuList.FirstOrDefault().Id;
                }
                else
                {
                    var modelPageName = new Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle();
                    if (subtitleId != null && pagesubcategory != null)
                    {
                        var model1 = alldata
                            .Where(d => Regex.Replace(d.Category.Replace(" ", ""), "[^a-zA-Z0-9]", "") == pageNameID)
                            .ToList();

                        var model2 = model1
                            .Where(d => Regex.Replace(d.SubCategory.Replace(" ", ""), "[^a-zA-Z0-9]", "") == pagesubcategory)
                            .ToList();

                        modelPageName = model2
                            .Where(d => Regex.Replace(d.Product.Replace(" ", ""), "[^a-zA-Z0-9]", "") == subtitleId)
                            .FirstOrDefault();
                    }
                    else
                    {
                        modelPageName = alldata.Where(d => d.Category == pageNameID).FirstOrDefault();
                    }

                    if (modelPageName == null)
                    {
                        modelPageName = alldata.FirstOrDefault();
                        ViewBag.PageName = modelPageName.Category;
                    }
                    model.Pagename = modelPageName.Category;
                    model.PagenameDescription = modelPageName.CategoryDescription;
                    model.SubCategoryPagename = Regex.Replace(modelPageName.SubCategory.Replace(" ", ""), "[^a-zA-Z0-9]", "");
                    model.ProductPagename = Regex.Replace(modelPageName.Product.Replace(" ", ""), "[^a-zA-Z0-9]", "");
                    //model.PagenameBanner = modelPageName.Image;
                    //model.Id = modelPageName.Id;

                    if (pagesubcategory != null && subtitleId != null)
                    {
                        ViewBag.PageId = modelPageName.Id;
                        ViewBag.PageName = pageNameID;
                        ViewBag.Subtitle = subtitleId;
                        ViewBag.pagesubcategory = pagesubcategory;
                        ViewBag.Qid = qid;
                    }
                    else
                    {
                        ViewBag.Subtitle = model.ProductPagename;
                        ViewBag.pagesubcategory = model.SubCategoryPagename;
                    }
                }

                model.PagenameBanner = _settingService.GetSetting("BannerImagepath").Value;
                return View(model);
            }
            catch (Exception)
            {
                return RedirectToAction("KnowledgeBaseMainPage");
            }

        }
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> Partialquestionanswer(string pageNameID, string pagesubcategory, string subtitleId, int qid)
        {
            try
            {

                var languageid = (await _workContext.GetWorkingLanguageAsync()).Id;
                var customerrole = _workContext.GetCurrentCustomerAsync().Result;
                var GetallRoles = _customerService.GetCustomerRolesAsync(customerrole).Result.Where(d => d.Name == "CAD B2C"
                   || d.Name == "USD B2C"
                   || d.Name == "USD B2B INTL"
                   || d.Name == "CAD B2C HS"
                   || d.Name == "USD B2B ST"
                   || d.Name == "USD B2B DS"
                   || d.Name == "CAD B2B ST"
                   || d.Name == "CAD B2B DS"
                   ).FirstOrDefault();


                var data = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().ToList();
                var alldata = new List<Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle>();
                foreach (var j in data)
                {
                    var splitedroles = j.CustomerRoles.Split(",");
                    foreach (var k in splitedroles)
                    {
                        if (k.Trim() == GetallRoles.Name)
                        {
                            alldata.Add(j);
                        }
                    }
                }
                var model = new KnowledgeBaseArticleModel();
                model.Categorylist = alldata.Select(x => x.Category).Distinct().ToList();
                model.subcategoryModelList = alldata
                                             .Where(x => model.Categorylist.Contains(x.Category))
                                             .Select(x => new subcategorylist
                                             {
                                                 id = x.Id,
                                                 Category = x.Category,
                                                 subcategory = x.SubCategory,
                                                 product = x.Product,
                                                 Question = x.ArticleTitle,
                                                 Answer = x.ArticleBody
                                             })
                                             .GroupBy(p => p.subcategory)
                                             .SelectMany(g => g) // Adjust if you need all items from the group or just the first one
                                             .ToList();

                if (pageNameID == null)
                {
                    var page = alldata.GroupBy(p => p.Category).Select(g => g.First()).FirstOrDefault();
                    model.Pagename = page.Category;
                    model.PagenameDescription = page.CategoryDescription;
                    model.SubCategoryPagename = page.SubCategory;
                    model.ProductPagename = page.Product;
                    //model.PagenameBanner = model.MainMenuList.FirstOrDefault().Image;
                    //model.Id = model.MainMenuList.FirstOrDefault().Id;
                }
                else
                {
                    var modelPageName = new Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle();
                    if (subtitleId != null && pagesubcategory != null)
                    {
                        var model1 = alldata
                            .Where(d => Regex.Replace(d.Category.Replace(" ", ""), "[^a-zA-Z0-9]", "") == pageNameID)
                            .ToList();

                        var model2 = model1
                            .Where(d => Regex.Replace(d.SubCategory.Replace(" ", ""), "[^a-zA-Z0-9]", "") == pagesubcategory)
                            .ToList();

                        modelPageName = model2
                            .Where(d => Regex.Replace(d.Product.Replace(" ", ""), "[^a-zA-Z0-9]", "") == subtitleId)
                            .FirstOrDefault();
                    }
                    else
                    {
                        modelPageName = alldata.Where(d => d.Category == pageNameID).FirstOrDefault();
                    }

                    if (modelPageName == null)
                    {
                        modelPageName = alldata.FirstOrDefault();
                        ViewBag.PageName = modelPageName.Category;
                    }
                    model.Pagename = modelPageName.Category;
                    model.PagenameDescription = modelPageName.CategoryDescription;
                    model.SubCategoryPagename = Regex.Replace(modelPageName.SubCategory.Replace(" ", ""), "[^a-zA-Z0-9]", "");
                    model.ProductPagename = Regex.Replace(modelPageName.Product.Replace(" ", ""), "[^a-zA-Z0-9]", "");
                    //model.PagenameBanner = modelPageName.Image;
                    //model.Id = modelPageName.Id;

                    if (pagesubcategory != null && subtitleId != null)
                    {
                        ViewBag.PageId = modelPageName.Id;
                        ViewBag.PageName = pageNameID;
                        ViewBag.Subtitle = subtitleId;
                        ViewBag.pagesubcategory = pagesubcategory;
                        ViewBag.Qid = qid;
                    }
                    else
                    {
                        ViewBag.Subtitle = model.ProductPagename;
                        ViewBag.pagesubcategory = model.SubCategoryPagename;
                    }
                }

                model.PagenameBanner = _settingService.GetSetting("BannerImagepath").Value;
                return PartialView("_partialQ&A", model);
            }
            catch (Exception)
            {
                return RedirectToAction("KnowledgeBaseMainPage");
            }

        }
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> oldIndex(int pageNameID, int subtitleId, int qid)
        {
            try
            {

                var languageid = (await _workContext.GetWorkingLanguageAsync()).Id;
                var model = new KnowledgeBaseModel();
                model.MainMenuList = _customPageRepository.Table.Where(x => x.LanguageId == Convert.ToString(languageid)).ToList();   //Left Main Menu List 
                var subtitlelist = _tblSubTitleRepository.Table.Where(x => x.LanguageId == Convert.ToString(languageid)).ToList();
                TittleModelList obj = new TittleModelList();
                if (model.MainMenuList.Count > 0)
                {
                    if (pageNameID == 0)
                    {
                        model.Pagename = model.MainMenuList.FirstOrDefault().Tittle;
                        model.PagenameDescription = model.MainMenuList.FirstOrDefault().Description;
                        model.PagenameBanner = model.MainMenuList.FirstOrDefault().Image;
                        model.Id = model.MainMenuList.FirstOrDefault().Id;
                    }
                    else
                    {
                        var modelPageName = model.MainMenuList.Where(d => d.Id == pageNameID).FirstOrDefault();
                        if (modelPageName == null)
                        {
                            modelPageName = model.MainMenuList.FirstOrDefault();
                            ViewBag.PageName = modelPageName.Tittle;
                        }
                        model.Pagename = modelPageName.Tittle;
                        model.PagenameDescription = modelPageName.Description;
                        model.PagenameBanner = modelPageName.Image;
                        model.Id = modelPageName.Id;

                        if (pageNameID != 0 && subtitleId != 0)
                        {
                            ViewBag.PageId = modelPageName.Id;
                            ViewBag.PageName = pageNameID;
                            ViewBag.Subtitle = subtitleId;
                            ViewBag.Qid = qid;
                        }
                    }

                    var contentModelList = new List<TblCustomPageContent>();

                    model.SubMenuList = _customPageContentRepository.Table.OrderBy(d => d.Display).Where(x => x.LanguageId == Convert.ToString(languageid)).ToList();//Left Sub Menu List

                    foreach (var item in model.SubMenuList)
                    {
                        item.Tittle = subtitlelist.Where(x => x.Id == item.SubTitleID).FirstOrDefault().SubTittle;
                        if (model.Titlename.Count > 0)
                        {
                            if (!model.Titlename.Any(d => d.Titlename == item.Tittle && d.MainPageID == item.TblcustomPageId))
                            {
                                var li = new mainsubTitle
                                {
                                    Titlename = item.Tittle,
                                    MainPageID = item.TblcustomPageId,
                                    SubTitleID = item.SubTitleID
                                };
                                model.Titlename.Add(li);
                            }
                        }
                        else
                        {
                            mainsubTitle li = new mainsubTitle();
                            li.Titlename = item.Tittle;
                            li.MainPageID = item.TblcustomPageId;
                            li.SubTitleID = item.SubTitleID;
                            model.Titlename.Add(li);
                        }
                    }
                    model.ContentModelList = model.SubMenuList.Where(d => d.TblcustomPageId == model.Id).ToList();

                }
                return View(model);
            }
            catch (Exception)
            {
                return RedirectToAction("KnowledgeBaseMainPage");
            }

        }


        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> Autocomplete(string term)
        {
            //var languageid = (await _workContext.GetWorkingLanguageAsync()).Id;

            //var result4 = _customPageContentRepository.Table.Where(x => x.LanguageId == Convert.ToString(languageid)).ToList();//Where(s => s.Question.ToLower().Contains(term.ToLower())).ToList();
            //var result = new List<KeyValuePair<string, string>>();
            //if (result4.Count > 0)
            //{
            //    foreach (var item in result4)
            //    {
            //        result.Add(new KeyValuePair<string, string>(item.Id.ToString(), item.Question));
            //    }
            //    var result3 = result.Where(s => s.Value.ToLower().Contains(term.ToLower())).Select(w => w).ToList();
            //    return Json(result3);
            //}
            var languageid = (await _workContext.GetWorkingLanguageAsync()).Id;
            var customerrole = _workContext.GetCurrentCustomerAsync().Result;
            var GetallRoles = _customerService.GetCustomerRolesAsync(customerrole).Result.Where(d => d.Name == "CAD B2C"
               || d.Name == "USD B2C"
               || d.Name == "USD B2B INTL"
               || d.Name == "CAD B2C HS"
               || d.Name == "USD B2B ST"
               || d.Name == "USD B2B DS"
               || d.Name == "CAD B2B ST"
               || d.Name == "CAD B2B DS"
               ).FirstOrDefault();
            var data = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().ToList();
            var result4 = new List<Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle>();
            foreach (var j in data)
            {
                var splitedroles = j.CustomerRoles.Split(",");
                foreach (var k in splitedroles)
                {
                    if (k.Trim() == GetallRoles.Name)
                    {
                        result4.Add(j);
                    }
                }
            }
            //var result4 = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().Where(x => x.CustomerRoles == GetallRoles.Name).ToList();
            var result = new List<KeyValuePair<string, string>>();
            if (result4.Count > 0)
            {
                foreach (var item in result4)
                {
                    result.Add(new KeyValuePair<string, string>(item.Id.ToString(), item.ArticleTitle));
                }
                var result3 = result.Where(s => s.Value.ToLower().Contains(term.ToLower())).Select(w => w).ToList();
                return Json(result3);
            }
            return Json(0);
        }


        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> GetData(int id)
        {
            var languageid = (await _workContext.GetWorkingLanguageAsync()).Id;
            var getQuestionbyid = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().Where(d => d.Id == id).FirstOrDefault();

            //return Json(new { PageNameId = getQuestionbyid.Category, pagesubcategory=getQuestionbyid.SubCategory.Replace(" ",""), SubtitleId = getQuestionbyid.Product.Replace(" ",""), Qid = getQuestionbyid.Id });
            return Json(new { PageNameId = Regex.Replace(getQuestionbyid.Category.Replace(" ", ""), "[^a-zA-Z0-9]", ""), pagesubcategory = Regex.Replace(getQuestionbyid.SubCategory.Replace(" ", ""), "[^a-zA-Z0-9]", ""), SubtitleId = Regex.Replace(getQuestionbyid.Product.Replace(" ", ""), "[^a-zA-Z0-9]", ""), Qid = getQuestionbyid.Id });

        }

        public async Task<IActionResult> KnowledgeBaseMainPage()
        {
            //KnowledgeBaseModel model = new KnowledgeBaseModel();
            //var languageid = (await _workContext.GetWorkingLanguageAsync()).Id;
            //var item = await _customPageRepository.Table.Where(x => x.LanguageId == Convert.ToString(languageid)).ToListAsync();
            //if (item.Count > 0)
            //{
            //    foreach (var obj in item)
            //    {
            //        var model1 = new KnowledgeBaseModel();
            //        model1.Pagename = obj.Tittle;
            //        model1.Id = obj.Id;
            //        model1.PagenameDescription = obj.Description;
            //        model1.PagenameBanner = obj.Image;
            //        model.ModelList.Add(model1);
            //    }
            //}
            //return View(model);
            var languageid = (await _workContext.GetWorkingLanguageAsync()).Id;
            var customerrole = _workContext.GetCurrentCustomerAsync().Result;
            var GetallRoles = _customerService.GetCustomerRolesAsync(customerrole).Result.Where(d => d.Name == "CAD B2C"
               || d.Name == "USD B2C"
               || d.Name == "USD B2B INTL"
               || d.Name == "CAD B2C HS"
               || d.Name == "USD B2B ST"
               || d.Name == "USD B2B DS"
               || d.Name == "CAD B2B ST"
               || d.Name == "CAD B2B DS"
               ).FirstOrDefault();
            if (GetallRoles == null)
            {
                GetallRoles = new CustomerRole();
            }
            var data = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().ToList();
            var alldata = new List<Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle>();
            foreach (var j in data)
            {
                var splitedroles = j.CustomerRoles.Split(",");
                foreach (var k in splitedroles)
                {
                    if (k.Trim() == GetallRoles.Name)
                    {
                        alldata.Add(j);
                    }
                }
            }
            var model = new KnowledgeBaseArticleModel();
            model.Categorylist = alldata.Select(x => x.Category).Distinct().ToList();

            foreach (var categoryitem in model.Categorylist)
            {
                var obj = data.Where(d => d.Category == categoryitem).FirstOrDefault();


                var model1 = new KnowledgeBaseArticleModel();
                model1.Id = obj.Id;
                model1.Category = obj.Category;
                model1.CategoryDescription = obj.CategoryDescription;
                model1.CategoryIcon = obj.CategoryIcon;
                model1.ColorCode = obj.ColorCode;
                model.ModelList.Add(model1);


            }
            model.PagenameBanner = _settingService.GetSetting("BannerImagepath").Value;
            //var model = new KnowledgeBaseArticleModel();
            //model.ModelList = new List<KnowledgeBaseArticleModel>();
            //var item = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().GroupBy(p => p.Category).Select(g => g.First()).ToList();
            //if (item.Count > 0)
            //{

            //    foreach (var obj in item)
            //    {

            //        var splitedroles = obj.CustomerRoles.Split(",");
            //        foreach (var k in splitedroles)
            //        {
            //            if (k == GetallRoles.Name)
            //            {
            //                var model1 = new KnowledgeBaseArticleModel();
            //                model1.Id = obj.Id;
            //                model1.Category = obj.Category;
            //                model1.CategoryDescription = obj.CategoryDescription;
            //                model1.CategoryIcon = obj.CategoryIcon;
            //                model.ModelList.Add(model1);
            //            }
            //        }

            //    }
            //    model.PagenameBanner = _settingService.GetSetting("BannerImagepath").Value;
            //}
            return View(model);

        }


        #endregion

    }
}
