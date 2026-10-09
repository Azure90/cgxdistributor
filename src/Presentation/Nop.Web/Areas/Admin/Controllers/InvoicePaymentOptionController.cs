using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Nop.Services.Configuration;
using Nop.Services.InvoicePaymentOptions;
using Nop.Services.Localization;
using Nop.Web.Models.DistributorPortal;
using Nop.Web.Areas.Admin.Models.InvoicePayment;
using Nop.Web.Framework.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Services.Customers;
using System.Linq;
using System;

namespace Nop.Web.Areas.Admin.Controllers
{
    public class InvoicePaymentOptionController : BaseAdminController
    {
        private readonly ISettingService _settingService;
        private readonly IInvoicePaymentOption _InvoicePaymentOptionService;
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;
        public InvoicePaymentOptionController(
             ISettingService settingService,
              ILocalizationService localizationService,
              ICustomerService customerService,
             IInvoicePaymentOption InvoicePaymentOptionService
          
            )
        {

            _InvoicePaymentOptionService = InvoicePaymentOptionService;
            _settingService = settingService;
            _localizationService = localizationService;
            _customerService = customerService;
        }

        public IActionResult InvoicePaymentOptionView()
        {

            var model = new InvoicePaymentModel();
            model.ModelList = new List<InvoicePaymentModel>(); // Initialize the list

            var obj = _InvoicePaymentOptionService.GetAllInvoicePaymentOption();
            foreach (var i in obj)
            {
                var articleModel = new InvoicePaymentModel
                {

                    Id = i.Id,
                    //CustomerRole=Convert.ToInt32(i.CustomerRole),
                    CustomerRoleName = string.Join(", ",    _customerService.GetAllCustomerRolesAsync().Result.Where(r => i.CustomerRole.Split(',').Select(id => id.Trim()).Contains(r.Id.ToString())).Select(r => r.Name)),
                    PaymentTittle =i.PaymentTittle,
                    paymentOption=i.paymentOption,
                    PaynowButton=i.PaynowButton,
                    Paynowtoption=i.Paynowtoption,
                    Active=i.Active
                };



                model.ModelList.Add(articleModel);
            }
            //model.PagenameBanner = _settingService.GetSetting("BannerImagepath").Value;
            //ViewBag.KnowledgeBasePagenameBanner = _settingService.GetSetting("KnowledgeBaseBannerImagepath").Value;
            //model.KnowledgeBaseLabel = _settingService.GetSetting("KnowledgeBaseLabel").Value;
            //ViewBag.KnowledgePointActive = Convert.ToBoolean(_settingService.GetSetting("KnowledgeBAseActive/Inactive").Value);
           // ViewBag.PayNowButton = Convert.ToBoolean(_settingService.GetSetting("PayNowButton").Value);
            //ViewBag.PayNowOption = Convert.ToBoolean(_settingService.GetSetting("PayNowOption").Value);

            return View(model);
        }
        public IActionResult editInvoicePaymentOption(int id)
        {
            var model = new InvoicePaymentModel();
            if (id != 0)
            {
                var obj = _InvoicePaymentOptionService.GetInvoicePaymentOptionById(id);                

                model.Id = obj.Id;
               
                var aa = obj.CustomerRole.Split(',');
                foreach (var i in aa)
                {
                    if (i != "")
                        model.CustomerRole.Add(Convert.ToInt32(i));
                }
                model.paymentOption = obj.paymentOption;
                model.PaymentTittle = obj.PaymentTittle;
                model.PaynowButton = obj.PaynowButton;
                model.Paynowtoption = obj.Paynowtoption;
                model.Active = obj.Active;
            }
            var availableCustomerRoles = _customerService.GetAllCustomerRolesAsync().Result.Where(role => role.Name.Contains("usd", StringComparison.OrdinalIgnoreCase) || role.Name.Contains("cad", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var customerRole in availableCustomerRoles)
            {
                model.AvailableCustomerRoles.Add(new SelectListItem { Value = customerRole.Id.ToString(), Text = customerRole.Name });
            }
            return View(model);
        }
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public IActionResult editInvoicePaymentOption(InvoicePaymentModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                if (model.Id == 0)
                {
                    var rewards = new Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption
                    {
                       
                        CustomerRole = string.Join(", ", model.CustomerRole.Select(x => x)),
                    paymentOption = model.paymentOption,
                        PaymentTittle = model.PaymentTittle,
                        PaynowButton = model.PaynowButton,
                        Paynowtoption = model.Paynowtoption,
                        Active = model.Active


                    };

                    _InvoicePaymentOptionService.InsertInvoicePaymentOption(rewards);
                }
                else
                {
                    var rewards = new Nop.Core.Domain.InvoicePaymentOptions.InvoicePaymentOption
                    {
                        Id = model.Id,
                        CustomerRole = string.Join(", ", model.CustomerRole.Select(x => x)),
                paymentOption = model.paymentOption,
                        PaymentTittle = model.PaymentTittle,
                        PaynowButton = model.PaynowButton,
                        Paynowtoption = model.Paynowtoption,
                        Active = model.Active


                    };

                    _InvoicePaymentOptionService.UpdateInvoicePaymentOption(rewards);
                }

                if (continueEditing)
                    return RedirectToAction("InvoicePaymentOptionView", new { id = model.Id });

                return RedirectToAction("InvoicePaymentOptionView");
            }
            return RedirectToAction("InvoicePaymentOptionView");

        }
        public IActionResult Delete(int id)
        {


            var DistributorCourses = _InvoicePaymentOptionService.GetInvoicePaymentOptionById(id);

            if (DistributorCourses == null)
                //No promotion found with the specified id
                return RedirectToAction("InvoicePaymentOptionView");

            _InvoicePaymentOptionService.DeleteInvoicePaymentOption(DistributorCourses);


            return RedirectToAction("InvoicePaymentOptionView");
        }
        [ValidateAntiForgeryToken]
        public ActionResult SetActiveInactivePayNowOption(bool priceStatus)
        {
            _settingService.SetSetting("PayNowOption", priceStatus);
            return Json(new { Status = priceStatus });
        }
        [ValidateAntiForgeryToken]
        public ActionResult SetActiveInactivePayNowButton(bool priceStatus)
        {
            _settingService.SetSetting("PayNowButton", priceStatus);
            return Json(new { Status = priceStatus });
        }
    }
}
