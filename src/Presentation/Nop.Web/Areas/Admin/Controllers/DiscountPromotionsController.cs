using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.DiscountPromotions;
using Nop.Services.Configuration;
using Nop.Services.DiscountPromotion;
using Nop.Services.Discounts;
using Nop.Services.Messages;
using Nop.Web.Areas.Admin.Models.DiscountPromotions;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class DiscountPromotionsController : BaseAdminController
    {
        private readonly IDiscountPromotionService _DiscountPromotionsRepository;
        private readonly IDiscountService _discountService;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly ISettingService _settingService;
        public DiscountPromotionsController(
            IDiscountPromotionService DiscountPromotionsRepository,
            IMessageTemplateService messageTemplateService,
             ISettingService settingService,
            IDiscountService discountService
            )
        {
            _DiscountPromotionsRepository = DiscountPromotionsRepository;
            _discountService = discountService;
            _settingService = settingService; 
            _messageTemplateService = messageTemplateService;

        }
        public virtual IActionResult DiscountPromotionList()
        {
            DiscountPromotionModel  model1 = new DiscountPromotionModel();
            var obj = _DiscountPromotionsRepository.GetAllDiscountPromotions();
            foreach(var i in obj)
            {
                DiscountPromotionModel model = new DiscountPromotionModel();
                model.Id = i.Id;
                model.DiscountCode = i.DiscountCode;
                model.DiscountPromotionType = i.DiscountPromotionType;
                model.OrderCount = i.OrderCount;
                model.AddDays = i.AddDays;
                model.Active = i.Active;
                model.MessageTemplatetId = i.MessageTemplatetId;
                model.ReminderDays = i.ReminderDays;
                model.ReminderMessageTemplatetId = i.ReminderMessageTemplatetId;
                model.CreatedDate = i.CreatedDate;
                model1.ModelList.Add(model);
            }
            model1.DaysforNewPurchasers = _settingService.GetSetting("DaysforNewPurchasers").Value;
            model1.DaysforLostCustomers = _settingService.GetSetting("DaysforLostCustomers").Value;
            return View(model1);
        }
        public virtual IActionResult addoreditDiscountPromotion(int id)
        {
            DiscountPromotionModel model = new DiscountPromotionModel();
            if (id != 0)
            {
                

                var obj = _DiscountPromotionsRepository.GetDiscountPromotionsById(id);
                model.Id = obj.Id;
                    model.DiscountCode = obj.DiscountCode;
                    model.OrderCount = obj.OrderCount;
                    model.AddDays = obj.AddDays;
                    model.Active = obj.Active;
                model.ReminderDays = obj.ReminderDays;
                model.ReminderMessageTemplatetId = obj.ReminderMessageTemplatetId;
                model.CreatedDate = obj.CreatedDate;
                model.MessageTemplatetId = obj.MessageTemplatetId;
                model.DiscountPromotionType = obj.DiscountPromotionType;
                model.ModelList.Add(model);
                

            }
            model.AvailableMessageTemplates = new List<SelectListItem>
{
    new SelectListItem { Value = "0", Text = "Select Message Template" },
    new SelectListItem { Value = "1", Text = "New Purchaser" },
    new SelectListItem { Value = "2", Text = "Lost Customer" }
};
            model.MessageTemplateNames.Add(new SelectListItem { Text = "Select Message Template", Value = "0" });
            foreach (var m in _messageTemplateService.GetAllMessageTemplatesAsync(1).Result)
                model.MessageTemplateNames.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
            
            model.ReminderMessageTemplateNames.Add(new SelectListItem { Text = "Select Message Template", Value = "0" });
            foreach (var m in _messageTemplateService.GetAllMessageTemplatesAsync(1).Result)
                model.ReminderMessageTemplateNames.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });

            return View(model);
        }
        [HttpPost]
        public virtual IActionResult DiscountPromotion(DiscountPromotionModel model)
        {
            if (model.Id == 0)
            {
                DiscountPromotions discountPromotions = new DiscountPromotions();
                discountPromotions.DiscountCode = model.DiscountCode;
                discountPromotions.OrderCount = model.OrderCount;
                discountPromotions.AddDays = model.AddDays;
                discountPromotions.MessageTemplatetId = model.MessageTemplatetId;
                discountPromotions.DiscountPromotionType = model.DiscountPromotionType;
                discountPromotions.ReminderDays = model.ReminderDays;
                discountPromotions.ReminderMessageTemplatetId = model.ReminderMessageTemplatetId;
                discountPromotions.Active = model.Active;
                discountPromotions.CreatedDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd"));
                _DiscountPromotionsRepository.InsertDiscountPromotions(discountPromotions);
            }
            else
            {
                DiscountPromotions discountPromotions = new DiscountPromotions();
                discountPromotions.Id = model.Id;
                discountPromotions.DiscountCode = model.DiscountCode;
                discountPromotions.OrderCount = model.OrderCount;
                discountPromotions.MessageTemplatetId = model.MessageTemplatetId;
                discountPromotions.DiscountPromotionType = model.DiscountPromotionType;
                discountPromotions.ReminderDays = model.ReminderDays;
                discountPromotions.ReminderMessageTemplatetId = model.ReminderMessageTemplatetId;
                discountPromotions.AddDays = model.AddDays;
                discountPromotions.Active = model.Active;
                discountPromotions.CreatedDate = _DiscountPromotionsRepository.GetDiscountPromotionsById(model.Id).CreatedDate;
                _DiscountPromotionsRepository.UpdateDiscountPromotions(discountPromotions);
            }

            return RedirectToAction("DiscountPromotionList");
        }
        public IActionResult Delete(int id)
        {
            if (id > 0)
            {
                var disc = _DiscountPromotionsRepository.GetDiscountPromotionsById(id);
                _DiscountPromotionsRepository.DeleteDiscountPromotions(disc);
            }

            return RedirectToAction("DiscountPromotionList");
        }
        public IActionResult CheckValidcode(string couponcode)
        {
            var data = "";
            var disccode = _discountService.GetAllDiscountsAsync(null,couponcode).Result;
            var disccode1 = _DiscountPromotionsRepository.GetAllDiscountPromotions().Where(x => x.DiscountCode == couponcode).ToList();
            
            if (disccode.Count > 0 )
            {
                if (disccode1.Count > 0)
                {
                    data = "2";
                }
                else
                {
                    data = "0";
                }

            }
            else
            {
                data = "1";
            }

            return Json(new { Result = data });
        }
        [ValidateAntiForgeryToken]
        public ActionResult DaysforNewPurchasers(string DaysforNewPurchasers,string DaysforLostCustomers)
        {
            _settingService.SetSetting("DaysforNewPurchasers", DaysforNewPurchasers);
            _settingService.SetSetting("DaysforLostCustomers", DaysforLostCustomers);
            return Json(new { Status = 1 });
        }
    }
}
