using Dapper;
using iTextSharp.text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Nop.Core;
using Nop.Core.Domain.CustomPages;
using Nop.Data;
using Nop.Web.Areas.Admin.Models.CustomPage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Web.Areas.Admin.Controllers
{
    public class CustomPageController : BaseAdminController
    {
        private readonly IRepository<TblCustomPages> _CustomPageRepository;
        private readonly IRepository<TblCustomPageContent> _CustomPageContentRepository;
        private readonly IRepository<TblSubTitle> _TblSubTitleRepository;
        private readonly IWorkContext _workContext;
        private readonly IHostingEnvironment _env;
        public CustomPageController(
            IRepository<TblCustomPages> customPageRepository,
            IRepository<TblCustomPageContent> CustomPageContentRepository,
            IRepository<TblSubTitle> TblSubTitleRepository
            , IHostingEnvironment env,
            IWorkContext workContext

            )
        {
            _CustomPageRepository = customPageRepository;
            _CustomPageContentRepository = CustomPageContentRepository;
            _TblSubTitleRepository = TblSubTitleRepository;
            _env = env;
            _workContext = workContext;

        }

        public IActionResult Index(int id)
        {
            CustomPageModel model = new CustomPageModel();
            model.LanguageId = Convert.ToString(_workContext.GetWorkingLanguageAsync().Result.Id);
            if (id != 0)
            {
                var x = _CustomPageRepository.GetById(id);
                model.ID = x.Id;
                model.Title = x.Tittle;
                model.Description = x.Description;
                model.Active = x.Active;
                model.Image = x.Image;
                model.LanguageId = x.LanguageId;
                return View(model);
            }
            else
            {
                return View(model);
            }

        }
        [HttpPost]
        public async Task<IActionResult> Index(CustomPageModel Model)
        {

            Nop.Core.Domain.CustomPages.TblCustomPages obj = new Core.Domain.CustomPages.TblCustomPages();
            Boolean hasImage = false;
            try
            {

                if (Model.BannerImage != null)
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
                    obj.Image = fileName;
                    hasImage = true;
                }
                if (!hasImage)
                {
                    obj.Image = _CustomPageRepository.GetById(Model.ID).Image;
                }
                // return "/Uploads/" + fileName;
                obj.Id = Model.ID;
                obj.Tittle = Model.Title;
                obj.Description = Model.Description;
                obj.Active = Model.Active;
                obj.LanguageId = Model.LanguageId;
                obj.LanguageId = Model.LanguageId;
                if (Model.ID != 0)
                {
                    _CustomPageRepository.Update(obj);
                }
                else
                {
                    _CustomPageRepository.Insert(obj);
                }


            }
            catch (Exception exception)
            {

            }
            return RedirectToAction("CustomListPage");

        }
        public IActionResult CustomListPage()
        {

            CustomPageModel model = new CustomPageModel();
            var list = _CustomPageRepository.Table.ToList();
            //if (id != 0)
            //{
            //    list = _CustomPageRepository.Table.ToList().Where(x => x.LanguageId == Convert.ToString(id)).ToList();
            //}            
            foreach (var x in list)
            {
                CustomPageModel model1 = new CustomPageModel();
                model1.ID = x.Id;
                model1.Title = x.Tittle;
                model1.Description = x.Description;
                model1.Active = x.Active;
                model1.Image = x.Image;
                model1.LanguageId = x.LanguageId;
                model.ModelList.Add(model1);
            }


            return View(model);
        }
        public IActionResult Delete(int id)
        {
            var connection = DataSettingsManager.LoadSettings().ConnectionString;
            using (IDbConnection conn1 = new SqlConnection(connection))
            {

                DynamicParameters ds = new DynamicParameters();

                ds.Add("@Page", "CustomPage");
                ds.Add("@id", id);
                var result = conn1.Query("SP_DeleteKnowledgebase", param: ds, commandType: CommandType.StoredProcedure);

            }


            return RedirectToAction("CustomListPage");
        }

        public IActionResult ContentPage(int id)
        {
            CustomPageContentModel obj = new CustomPageContentModel();

            var list = _CustomPageRepository.Table.ToList();
            var Subtitle = _TblSubTitleRepository.Table.ToList();
            obj.LanguageId = Convert.ToString(_workContext.GetWorkingLanguageAsync().Result.Id);
            obj.CustomPageList = list;
            obj.TitleList = Subtitle;
            if (id != 0)
            {
                var x = _CustomPageContentRepository.GetById(id);

                obj.ID = x.Id;
                obj.Title = x.Tittle;
                obj.Question = x.Question;
                obj.Answer = x.Answer;
                obj.Active = x.Active;
                obj.TblcustomPageId = x.TblcustomPageId;
                obj.SubTitleID = x.SubTitleID;
                obj.Display = x.Display;
                obj.LanguageId = x.LanguageId;

            }
            return View(obj);
        }
        [HttpPost]
        public IActionResult ContentPage(CustomPageContentModel model)
        {
            TblCustomPageContent getbyid = new TblCustomPageContent();
            getbyid.Id = model.ID;
            getbyid.TblcustomPageId = model.TblcustomPageId;
            getbyid.SubTitleID = model.SubTitleID;
            getbyid.Tittle = model.Title;
            getbyid.Question = model.Question;
            getbyid.Answer = model.Answer;
            getbyid.Active = model.Active;
            getbyid.Display = model.Display;
            getbyid.LanguageId = model.LanguageId;
            getbyid.LanguageId = model.LanguageId;
            if (model.ID == 0)
            {
                _CustomPageContentRepository.Insert(getbyid);
            }
            else
            {
                _CustomPageContentRepository.Update(getbyid);
            }
            return RedirectToAction("ContentPageList");
        }
        public IActionResult ContentPageList()
        {
            CustomPageContentModel model = new CustomPageContentModel();
            var list = _CustomPageContentRepository.Table.ToList();

            foreach (var x in list)
            {
                CustomPageContentModel model1 = new CustomPageContentModel();
                model1.ID = x.Id;
                model1.Title = x.Tittle;
                model1.Question = x.Question;
                model1.Answer = x.Answer;
                model1.Active = x.Active;
                model1.TblcustomPageId = x.TblcustomPageId;
                model1.CustomPage = _CustomPageRepository.Table.ToList().Where(y => y.Id == x.TblcustomPageId).First().Tittle;
                model1.SubTitle = _TblSubTitleRepository.Table.ToList().Where(y => y.Id == x.SubTitleID).First().SubTittle;
                model1.Display = x.Display;
                model1.LanguageId = x.LanguageId;
                //model1.Image = x.Image;
                model.ModelList.Add(model1);
            }
            return View(model);
        }
        [HttpPost]
        public IActionResult DeleteContent(int id)
        {

            _CustomPageContentRepository.Delete(_CustomPageContentRepository.GetById(id));
            return RedirectToAction("ContentPageList");
        }
        public IActionResult SubtitleList()
        {
            SubTitleModel subTitleModel = new SubTitleModel();
            var list = _TblSubTitleRepository.Table.ToList();
            foreach (var i in list)
            {
                SubTitleModel subTitleModel1 = new SubTitleModel();
                subTitleModel1.ID = i.Id;
                subTitleModel1.SubTittle = i.SubTittle;
                subTitleModel1.LanguageId = i.LanguageId;
                subTitleModel.ModelList.Add(subTitleModel1);
            }
            return View(subTitleModel);
        }
        public IActionResult createSubtitle(int id)
        {
            SubTitleModel subTitleModel = new SubTitleModel();
            subTitleModel.LanguageId = Convert.ToString(_workContext.GetWorkingLanguageAsync().Result.Id);
            if (id == 0)
            {
                return View(subTitleModel);
            }
            else
            {
                var getbyid = _TblSubTitleRepository.GetById(id);
                subTitleModel.ID = getbyid.Id;
                subTitleModel.SubTittle = getbyid.SubTittle;
                subTitleModel.LanguageId = getbyid.LanguageId;
                subTitleModel.LanguageId = getbyid.LanguageId;
                return View(subTitleModel);

            }
        }
        public IActionResult SaveSubtitle(SubTitleModel model)
        {
            TblSubTitle tblSubTitle = new TblSubTitle();
            tblSubTitle.Id = model.ID;
            tblSubTitle.SubTittle = model.SubTittle;
            tblSubTitle.LanguageId = model.LanguageId;
            if (model.ID == 0)
            {
                _TblSubTitleRepository.Insert(tblSubTitle);
            }
            else
            {
                _TblSubTitleRepository.Update(tblSubTitle);
            }
            return RedirectToAction("SubtitleList");
        }
        public IActionResult DeleteSubtitle(int ID)
        {
            var connection = DataSettingsManager.LoadSettings().ConnectionString;
            using (IDbConnection conn1 = new SqlConnection(connection))
            {

                DynamicParameters ds = new DynamicParameters();

                ds.Add("@Page", "SubtitlePage");
                ds.Add("@id", ID);
                var result = conn1.Query("SP_DeleteKnowledgebase", param: ds, commandType: CommandType.StoredProcedure);

            }
            // _TblSubTitleRepository.Delete(_TblSubTitleRepository.GetById(ID));
            return RedirectToAction("SubtitleList");
        }
    }
}
