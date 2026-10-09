using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Nop.Core;
using Nop.Core.Domain;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors; 
using Nop.Core.Http;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Html;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Themes;
using Nop.Web.Infrastructure;
using Nop.Web.Models.Common;
using Nop.Web.Models.Media;
using Nop.Web.Models.Sitemap;

namespace Nop.Web.Controllers
{
    [AutoValidateAntiforgeryToken]
    public partial class CommonController : BasePublicController
    {
        #region Fields

        private readonly CaptchaSettings _captchaSettings;
        private readonly CommonSettings _commonSettings;
        private readonly ICommonModelFactory _commonModelFactory;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHtmlFormatter _htmlFormatter;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ISitemapModelFactory _sitemapModelFactory;
        private readonly IStoreContext _storeContext;
        private readonly IThemeContext _themeContext;
        private readonly IVendorService _vendorService;
        private readonly IWorkContext _workContext;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly SitemapSettings _sitemapSettings;
        private readonly SitemapXmlSettings _sitemapXmlSettings;
        private readonly StoreInformationSettings _storeInformationSettings;
        private readonly VendorSettings _vendorSettings;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IUrlRecordService _urlRecordService;

        #endregion

        #region Ctor

        public CommonController(CaptchaSettings captchaSettings,
            CommonSettings commonSettings,
            ICommonModelFactory commonModelFactory,
            ICurrencyService currencyService,
            ICustomerActivityService customerActivityService,
            IGenericAttributeService genericAttributeService,
            IHtmlFormatter htmlFormatter,
            ILanguageService languageService,
            ILocalizationService localizationService,
            ISitemapModelFactory sitemapModelFactory,
            IStoreContext storeContext,
            IThemeContext themeContext,
            IVendorService vendorService,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            LocalizationSettings localizationSettings,
            SitemapSettings sitemapSettings,
            SitemapXmlSettings sitemapXmlSettings,
            StoreInformationSettings storeInformationSettings,
            ICustomerService customerService,
            IProductService productService,
            IProductModelFactory productModelFactory,
            IUrlRecordService urlRecordService,
            VendorSettings vendorSettings)
        {
            _captchaSettings = captchaSettings;
            _commonSettings = commonSettings;
            _commonModelFactory = commonModelFactory;
            _currencyService = currencyService;
            _customerActivityService = customerActivityService;
            _genericAttributeService = genericAttributeService;
            _htmlFormatter = htmlFormatter;
            _languageService = languageService;
            _localizationService = localizationService;
            _sitemapModelFactory = sitemapModelFactory;
            _storeContext = storeContext;
            _themeContext = themeContext;
            _vendorService = vendorService;
            _workContext = workContext;
            _workflowMessageService = workflowMessageService;
            _localizationSettings = localizationSettings;
            _sitemapSettings = sitemapSettings;
            _sitemapXmlSettings = sitemapXmlSettings;
            _storeInformationSettings = storeInformationSettings;
            _vendorSettings = vendorSettings;
            _customerService = customerService;
            _productService = productService;
            _productModelFactory = productModelFactory;
            _urlRecordService = urlRecordService;
        }

        #endregion

        #region Methods

        //page not found
        public virtual IActionResult PageNotFound()
        {
            Response.StatusCode = 404;
            Response.ContentType = "text/html";

            return View();
        }

        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> SetLanguage(int langid, string returnUrl = "")
        {
            var language = await _languageService.GetLanguageByIdAsync(langid);
            if (!language?.Published ?? false)
                language = await _workContext.GetWorkingLanguageAsync();

            //home page
            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("Homepage");

            //language part in URL
            if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                //remove current language code if it's already localized URL
                if ((await returnUrl.IsLocalizedUrlAsync(Request.PathBase, true)).IsLocalized)
                    returnUrl = returnUrl.RemoveLanguageSeoCodeFromUrl(Request.PathBase, true);

                //and add code of passed language
                returnUrl = returnUrl.AddLanguageSeoCodeToUrl(Request.PathBase, true, language);
            }

            await _workContext.SetWorkingLanguageAsync(language);

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("Homepage");

            return Redirect(returnUrl);
        }

        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> SetCurrency(int customerCurrency, string returnUrl = "")
        {
            var currency = await _currencyService.GetCurrencyByIdAsync(customerCurrency);
            if (currency != null)
                await _workContext.SetWorkingCurrencyAsync(currency);

            //home page
            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("Homepage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("Homepage");

            return Redirect(returnUrl);
        }

        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> SetTaxType(int customerTaxType, string returnUrl = "")
        {
            var taxDisplayType = (TaxDisplayType)Enum.ToObject(typeof(TaxDisplayType), customerTaxType);
            await _workContext.SetTaxDisplayTypeAsync(taxDisplayType);

            //home page
            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("Homepage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("Homepage");

            return Redirect(returnUrl);
        }

        //contact us page
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        public virtual async Task<IActionResult> ContactUs()
        {
            var model = new ContactUsModel();
            model = await _commonModelFactory.PrepareContactUsModelAsync(model, false);

            return View(model);
        }

        [HttpPost, ActionName("ContactUs")]
        [ValidateCaptcha]
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        public virtual async Task<IActionResult> ContactUsSend(ContactUsModel model, bool captchaValid)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
            }

            model = await _commonModelFactory.PrepareContactUsModelAsync(model, true);

            if (ModelState.IsValid)
            {
                var subject = _commonSettings.SubjectFieldOnContactUsForm ? model.Subject : null;
                var body = _htmlFormatter.FormatText(model.Enquiry, false, true, false, false, false, false);

                await _workflowMessageService.SendContactUsMessageAsync((await _workContext.GetWorkingLanguageAsync()).Id,
                    model.Email.Trim(), model.FullName, subject, body);

                model.SuccessfullySent = true;
                model.Result = await _localizationService.GetResourceAsync("ContactUs.YourEnquiryHasBeenSent");

                //activity log
                await _customerActivityService.InsertActivityAsync("PublicStore.ContactUs",
                    await _localizationService.GetResourceAsync("ActivityLog.PublicStore.ContactUs"));

                return View(model);
            }

            return View(model);
        }

        [HttpsRequirement]
        //available even when a store is closed
        [CheckAccessClosedStore(true)]
        public virtual IActionResult all_contact_us_pages()
        {
            var model = new ContactUsModel();
            model = _commonModelFactory.PrepareContactUsModelAsync(model, false).Result;
            TempData["fas"] = "1";
            return View(model);
        }
        [HttpPost]
        [ValidateCaptcha]
        //available even when a store is closed
        [CheckAccessClosedStore(true)]
        public virtual IActionResult all_contact_us_pages(ContactUsModel model, bool captchaValid)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", _localizationService.GetResourceAsync("Common.WrongCaptchaMessage").Result);
            }

            model = _commonModelFactory.PrepareContactUsModelAsync(model, true).Result;

            if (ModelState.IsValid)
            {
                var subject = _commonSettings.SubjectFieldOnContactUsForm ? model.Subject : null;
                //  var body = Core.Ht.Html.HtmlHelper.FormatText(model.Enquiry, false, true, false, false, false, false);

                //_workflowMessageService.SendContactUsMessage(_workContext.WorkingLanguage.Id,
                //  model.Email.Trim(), model.FullName, subject, body);

                model.SuccessfullySent = true;
                model.Result = _localizationService.GetResourceAsync("ContactUs.YourEnquiryHasBeenSent").Result;

                //activity log
                _customerActivityService.InsertActivityAsync("PublicStore.ContactUs",
                    _localizationService.GetResourceAsync("ActivityLog.PublicStore.ContactUs").Result);
                ViewBag.successmessage = "Success";
                return View(model);

            }
            return View(model);
        }
        [HttpsRequirement]
        //available even when a store is closed
        [CheckAccessClosedStore(true)]
        public virtual IActionResult newsLetterForm()
        {
            var model = new ContactUsModel();
            List<SelectListItem> ObjItem = new List<SelectListItem>()
            {
          new SelectListItem {Text=_localizationService.GetResourceAsync("Support.select.Hint").Result,Value=_localizationService.GetResourceAsync("Support.select.Hint").Result,Selected=true },
          new SelectListItem {Text="Dr.",Value="Dr"},
          new SelectListItem {Text="Dre",Value="Dre"},
          new SelectListItem {Text="Drs.",Value="Drs"},
          new SelectListItem {Text="Mr.",Value="Mr"},
          new SelectListItem {Text="Ms.",Value="Ms"},
          new SelectListItem {Text="M.",Value="M"},
          new SelectListItem {Text="Mme",Value="Mme"},
            };
            model.Salutationlist = ObjItem;
            List<String> list = new List<String>();

            list.Add(_localizationService.GetResourceAsync("Support.GeneralPractitioner.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.Oral&amp;MaxillofacialSurgery.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.Periodontist.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.Orthodontist.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.Endodontist.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.Prosthodontist.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.Hygienist.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.Dentalassistant.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.PediatricDentistry.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.OralMedicine&amp;Pathology.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.DentalAnaesthesia.Hint").Result);
            list.Add(_localizationService.GetResourceAsync("Support.Other.Hint").Result);
            model.Specialty = list;
            List<SelectListItem> Obj = new List<SelectListItem>()
            {
          new SelectListItem {Text=_localizationService.GetResourceAsync("Support.select.Hint").Result,Value=_localizationService.GetResourceAsync("Support.select.Hint").Result,Selected=true },
          new SelectListItem {Text="Asia/Pacific Region",Value="Asia/Pacific Region" },
          new SelectListItem {Text="Australia",Value="Australia"},
          new SelectListItem {Text="Canada",Value="Canada"},
          new SelectListItem {Text="Europe",Value="Europe" },
          new SelectListItem {Text="United State",Value="United State" },
          new SelectListItem {Text="Africa",Value="Africa" },
          new SelectListItem {Text="South America",Value="South America" },
            };
            model.countrylist = Obj;
            List<SelectListItem> items = new List<SelectListItem>()
            {
          new SelectListItem {Text=_localizationService.GetResourceAsync("Support.select.Hint").Result,Value=_localizationService.GetResourceAsync("Support.select.Hint").Result,Selected=true },
          new SelectListItem {Text=_localizationService.GetResourceAsync("Support.English.Hint").Result,Value=_localizationService.GetResourceAsync("Support.English.Hint").Result },
          new SelectListItem {Text=_localizationService.GetResourceAsync("Support.French.Hint").Result,Value=_localizationService.GetResourceAsync("Support.French.Hint").Result},
            };
            model.PrefferedLanguageList = items;
            model = _commonModelFactory.PrepareContactUsModelAsync(model, false).Result;
            TempData["fas"] = "1";
            return View(model);
        }
        [HttpPost, ActionName("newsLetterForm")]
        [ValidateCaptcha]
        //available even when a store is closed
        [CheckAccessClosedStore(true)]
        public virtual IActionResult newsLetterForm(ContactUsModel model, bool captchaValid)
        {
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", _localizationService.GetResourceAsync("Common.WrongCaptchaMessage").Result);
            }
            PriceChange priceChange = new PriceChange();
            var count = priceChange.Gettheemailcount("News Letter Form");
            if (ModelState.IsValid && count <= 3)
            {
                var field = "";
                for (var i = 0; i < model.Specialty.Count; i++)
                {
                    if (model.Specialty[i] != "")
                    {
                        field = field + model.Specialty[i] + ",";
                    }
                }
                var Subjectmodel = "News Letter Form ";
                var subject = _commonSettings.SubjectFieldOnContactUsForm ? Subjectmodel : null;
                // var body = Core.Html.HtmlHelper.FormatText(model.Enquiry, false, true, false, false, false, false);
                var body = "<br/><br/>Full Name :" + model.Salutation + "." + model.FirstName + "" + model.LastName + " " +
                        "<br/> Email :" + model.Email + "" +
                         "<br/> Field Specialty : " + field + "" +
                          "<br/> Country : " + model.country + "" +
                          "<br/> Postal / Zip Code : " + model.ZipCode + "" +
                          "<br/>Preferred Language : " + model.Preferredlanguage + "" +
                          "";

                _workflowMessageService.SendContactUsMessageAsync(_workContext.GetWorkingLanguageAsync().Result.Id,
                  model.Email, model.FirstName, Subjectmodel, body);

                model.SuccessfullySent = true;
                model.Result = _localizationService.GetResourceAsync("ContactUs.YourEnquiryHasBeenSent").Result;

                //activity log
                _customerActivityService.InsertActivityAsync("PublicStore.ContactUs",
                    _localizationService.GetResourceAsync("ActivityLog.PublicStore.ContactUs").Result);
                ViewBag.successmessage = "Success";
                return View(model);
            }

            return View(model);

        }
        [HttpsRequirement]
        [CheckAccessClosedStore(true)]
        public virtual async Task<IActionResult> Support()
        {
            var model = new ContactUsModel();
            model = await _commonModelFactory.PrepareContactUsModelAsync(model, false);

            return View(model);
        }
        [HttpPost, ActionName("Support")]
        [ValidateCaptcha]
        //available even when a store is closed
        [CheckAccessClosedStore(true)]
        public virtual async Task<IActionResult> Support(ContactUsModel model, bool captchaValid)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
            }

            model = await _commonModelFactory.PrepareContactUsModelAsync(model, true);

            if (ModelState.IsValid)
            {
                var subject = _commonSettings.SubjectFieldOnContactUsForm ? model.Subject : null;
                var body = _htmlFormatter.FormatText(model.Enquiry, false, true, false, false, false, false);

                await _workflowMessageService.SendContactUsMessageAsync((await _workContext.GetWorkingLanguageAsync()).Id,
                    model.Email.Trim(), model.FullName, subject, body);

                model.SuccessfullySent = true;
                model.Result = await _localizationService.GetResourceAsync("ContactUs.YourEnquiryHasBeenSent");

                //activity log
                await _customerActivityService.InsertActivityAsync("PublicStore.ContactUs",
                    await _localizationService.GetResourceAsync("ActivityLog.PublicStore.ContactUs"));

                return View(model);
            }

            return View(model);
        }
        [HttpsRequirement]
        [CheckAccessClosedStore(true)]
        public virtual IActionResult Catalogues()
        {
            return View();
        }
        [HttpPost, ActionName("Catalogues")]
        [ValidateCaptcha]
        [CheckAccessClosedStore(true)]
        public virtual IActionResult Catalogues(IFormCollection model, bool captchaValid)
        {
            var email = model["email"];
            var firstname = model["firstname"];
            var lastname = model["lastname"];
            var clinic_name = model["clinic_name"];
            var address = model["address"];
            var city = model["city"];
            var state = model["state"];
            var zip = model["zip"];
            var country = model["country"];
            var phone = model["phone"];
            var work_email = model["work_email"];
            var catalogue_request = model["catalogue_request"];
            var hs_language = model["hs_language"];
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", _localizationService.GetResourceAsync("Common.WrongCaptchaMessage").Result);
            }

            // model = _commonModelFactory.PrepareContactUsModel(model, true);
            PriceChange priceChange = new PriceChange();
            var count = priceChange.Gettheemailcount("catalogues");
            if (ModelState.IsValid && count <= 3)
            {
                var Subjectmodel = "catalogues";
                var subject = _commonSettings.SubjectFieldOnContactUsForm ? Subjectmodel : null;
                // var body = Core.Html.HtmlHelper.FormatText(model.Enquiry, false, true, false, false, false, false);
                var body = "<br/><br/>Full Name :" + firstname + "" + lastname + " " +
                         "<br/> Email :" + email + "" +
                    "<br/> Practice/Clinic name :" + clinic_name + "" +
                         "<br/> Street address : " + address + "" +
                          "<br/> City : " + city + "" +
                          "<br/> State/Region : " + state + "" +
                          "<br/> Postal code : " + zip + "" +
                          "<br/> Country/Region : " + country + "" +
                          "<br/> Practice/Clinic Phone number : " + phone + "" +
                          "<br/> Practice/Clinic main email : " + work_email + "" +
                          "<br/> Catalogue request : " + catalogue_request + "" +
                          "<br/> Preferred language: " + hs_language + "" + "";

                _workflowMessageService.SendContactUsMessageAsync(_workContext.GetWorkingLanguageAsync().Result.Id,
                   email, firstname, Subjectmodel, body);

                //model.SuccessfullySent = true;
                //model.Result = _localizationService.GetResource("ContactUs.YourEnquiryHasBeenSent");

                //activity log
                _customerActivityService.InsertActivityAsync("PublicStore.ContactUs",
                    _localizationService.GetResourceAsync("ActivityLog.PublicStore.ContactUs").Result);
                ViewBag.successmessage = "Success";
                return View(model);
            }

            return View(model);
        }
        [HttpsRequirement]
        [CheckAccessClosedStore(true)]
        public virtual IActionResult catalogues_usa()
        {
            return View();
        }
        [HttpPost, ActionName("catalogues-usa")]
        [ValidateCaptcha]
        [CheckAccessClosedStore(true)]
        public virtual IActionResult catalogues_usa(IFormCollection model, bool captchaValid)
        {
            var email = model["email"];
            var firstname = model["firstname"];
            var lastname = model["lastname"];
            var clinic_name = model["clinic_name"];
            var address = model["address"];
            var city = model["city"];
            var state = model["state"];
            var zip = model["zip"];
            var country = model["country"];
            var phone = model["phone"];
            var work_email = model["work_email"];
            var catalogue_request = model["catalogue_request"];
            var hs_language = model["hs_language"];
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", _localizationService.GetResourceAsync("Common.WrongCaptchaMessage").Result);
            }

            // model = _commonModelFactory.PrepareContactUsModel(model, true);
            PriceChange priceChange = new PriceChange();
            var count = priceChange.Gettheemailcount("catalogues_usa");
            if (ModelState.IsValid && count <= 3)
            {
                var Subjectmodel = "catalogues_usa";
                var subject = _commonSettings.SubjectFieldOnContactUsForm ? Subjectmodel : null;
                // var body = Core.Html.HtmlHelper.FormatText(model.Enquiry, false, true, false, false, false, false);
                var body = "<br/><br/>Full Name :" + firstname + "" + lastname + " " +
                         "<br/> Email :" + email + "" +
                    "<br/> Practice/Clinic name :" + clinic_name + "" +
                         "<br/> Street address : " + address + "" +
                          "<br/> City : " + city + "" +
                          "<br/> State/Region : " + state + "" +
                          "<br/> Postal code : " + zip + "" +
                          "<br/> Country/Region : " + country + "" +
                          "<br/> Practice/Clinic Phone number : " + phone + "" +
                          "<br/> Practice/Clinic main email : " + work_email + "" +
                          "<br/> Catalogue request : " + catalogue_request + "" +
                          "<br/> Preferred language: " + hs_language + "" + "";

                _workflowMessageService.SendContactUsMessageAsync(_workContext.GetWorkingLanguageAsync().Result.Id,
                   email, firstname, Subjectmodel, body);

                //model.SuccessfullySent = true;
                //model.Result = _localizationService.GetResource("ContactUs.YourEnquiryHasBeenSent");

                //activity log
                _customerActivityService.InsertActivityAsync("PublicStore.ContactUs",
                    _localizationService.GetResourceAsync("ActivityLog.PublicStore.ContactUs").Result);
                ViewBag.successmessage = "Success";
                return View(model);
            }

            return View(model);
        }

        [HttpsRequirement]
        [CheckAccessClosedStore(true)]
        public virtual IActionResult AboutHansamed()
        {
            return View();
        }

        [HttpPost, ActionName("AboutHansamed")]
        [ValidateCaptcha]
        [CheckAccessClosedStore(true)]
        public virtual IActionResult AboutHansamed(IFormCollection model, bool captchaValid)
        {
            var Email = model["email"];
            var Country = model["country"];
            var Salutation = model["salutation"];
            var FirstName = model["firstName"];
            var LastName = model["lastName"];
            var CompanyName = model["companyName"];
            var PhoneNumber = model["phone"];
            var Message = model["message"];
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", _localizationService.GetResourceAsync("Common.WrongCaptchaMessage").Result);
            }

            // model = _commonModelFactory.PrepareContactUsModel(model, true);

            PriceChange priceChange = new PriceChange();
            var count = priceChange.Gettheemailcount("About");
            if (ModelState.IsValid && count <= 3)
            {
                var Subjectmodel = "About";
                var subject = _commonSettings.SubjectFieldOnContactUsForm ? Subjectmodel : null;
                // var body = Core.Html.HtmlHelper.FormatText(model.Enquiry, false, true, false, false, false, false);
                var body = "<br/><br/>Email :" + Email + " " +
                         "<br/> Country :" + Country + "" +
                    "<br/> Salutation :" + Salutation + "" +
                   "< br />< br /> Full Name: " + FirstName + "" + LastName + " " +
                          "<br/> Company Name : " + CompanyName + "" +
                          "<br/> Phone Number : " + PhoneNumber + "" +
                          "<br/> Message : " + Message + "" + "" + "";

                _workflowMessageService.SendContactUsMessageAsync(_workContext.GetWorkingLanguageAsync().Result.Id,
                   Email, FirstName, Subjectmodel, body);

                //model.SuccessfullySent = true;
                //model.Result = _localizationService.GetResource("ContactUs.YourEnquiryHasBeenSent");

                //activity log
                _customerActivityService.InsertActivityAsync("PublicStore.ContactUs",
                    _localizationService.GetResourceAsync("ActivityLog.PublicStore.ContactUs").Result);
                ViewBag.successmessage = "Success";
                return View(model);
            }

            return View(model);
        }
        [HttpsRequirement]
        [CheckAccessClosedStore(true)]
        public virtual IActionResult newaccount()
        {
            return View();
        }


        [HttpPost, ActionName("newaccount")]
        [ValidateCaptcha]
        [CheckAccessClosedStore(true)]
        public virtual IActionResult newaccount(IFormCollection model, bool captchaValid)
        {
            var salutation = model["salutation"];
            var firstname = model["firstname"];
            var lastname = model["lastname"];
            var clinic_name = model["clinic_name"];
            var website = model["website"];
            var email = model["email"];
            var work_email = model["work_email"];
            var purchasing_contact = model["purchasing_contact"];
            var accounts_payable_contact = model["accounts_payable_contact"];
            var clinic_payables_email_accounting_ = model["clinic_payables_email_accounting_"];
            var licence_nb = model["licence_nb"];
            var phone = model["phone"];
            var fax = model["fax"];
            var address = model["address"];
            var city = model["city"];
            var zip = model["zip"];
            var state = model["state"];
            var country = model["country"];
            var field_of_study = model["field_of_study"];
            var mobilephone = model["mobilephone"];
            var account_type = model["account_type"];
            var ship_to_address = model["ship_to_address"];
            var preferred_payment_option = model["preferred_payment_option"];
            var hs_language = model["hs_language"];
            var internal_account_comments = model["internal_account_comments"];
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", _localizationService.GetResourceAsync("Common.WrongCaptchaMessage").Result);
            }

            // model = _commonModelFactory.PrepareContactUsModel(model, true);

            PriceChange priceChange = new PriceChange();
            var count = priceChange.Gettheemailcount("New-Account");
            if (ModelState.IsValid && count <= 3)
            {
                var Subjectmodel = "New-Account";
                var subject = _commonSettings.SubjectFieldOnContactUsForm ? Subjectmodel : null;
                // var body = Core.Html.HtmlHelper.FormatText(model.Enquiry, false, true, false, false, false, false);
                var body = "<br/><br/>Full Name :" + salutation + "" + firstname + " " + lastname + " " +
                          "<br/> Business name :" + clinic_name + "" +
                     "<br/> Website URL :" + website + "" +
                          "<br/> Email : " + email + "" +
                           "<br/> Main email : " + work_email + "" +
                           "<br/> Contact person | Orders : " + purchasing_contact + "" +
                           "<br/> Contact person | Accounts payable : " + accounts_payable_contact + "" +
                           "<br/> Clinic/Practice | Payables email (accounting) : " + clinic_payables_email_accounting_ + "" +
                           "<br/> Doctor's licence # : " + licence_nb + "" +
                           "<br/> Phone Number : " + phone + "" +
                           "<br/> Fax Number : " + fax + "" +
                           "<br/> Street address : " + address + "" +
                           "<br/> City : " + city + "" +
                           "<br/> Zip/Postal Code : " + zip + "" +
                           "<br/> State/Province : " + state + "" +
                           "<br/> Country : " + country + "" +
                           "<br/> Field Specialty : " + field_of_study + "" +
                           "<br/> Doctor's cell phone if available : " + mobilephone + "" +
                           "<br/> Account type : " + account_type + "" +
                           "<br/> Ship to address : " + ship_to_address + "" +
                           "<br/> Preferred payment option : " + preferred_payment_option + "" +
                           "<br/> Preferred language of communication : " + hs_language + "" +
                           "<br/> Comments & identification: " + internal_account_comments + "" + "";

                _workflowMessageService.SendContactUsMessageAsync(_workContext.GetWorkingLanguageAsync().Result.Id,
                   email, firstname, Subjectmodel, body);

                //model.SuccessfullySent = true;
                //model.Result = _localizationService.GetResource("ContactUs.YourEnquiryHasBeenSent");

                //activity log
                _customerActivityService.InsertActivityAsync("PublicStore.ContactUs",
                    _localizationService.GetResourceAsync("ActivityLog.PublicStore.ContactUs").Result);
                ViewBag.successmessage = "Success";
                return View(model);
            }

            return View(model);
        }

        //contact vendor page
        public virtual async Task<IActionResult> ContactVendor(int vendorId)
        {
            if (!_vendorSettings.AllowCustomersToContactVendors)
                return RedirectToRoute("Homepage");

            var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
            if (vendor == null || !vendor.Active || vendor.Deleted)
                return RedirectToRoute("Homepage");

            var model = new ContactVendorModel();
            model = await _commonModelFactory.PrepareContactVendorModelAsync(model, vendor, false);

            return View(model);
        }

        [HttpPost, ActionName("ContactVendor")]
        [ValidateCaptcha]
        public virtual async Task<IActionResult> ContactVendorSend(ContactVendorModel model, bool captchaValid)
        {
            if (!_vendorSettings.AllowCustomersToContactVendors)
                return RedirectToRoute("Homepage");

            var vendor = await _vendorService.GetVendorByIdAsync(model.VendorId);
            if (vendor == null || !vendor.Active || vendor.Deleted)
                return RedirectToRoute("Homepage");

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
            }

            model = await _commonModelFactory.PrepareContactVendorModelAsync(model, vendor, true);

            if (ModelState.IsValid)
            {
                var subject = _commonSettings.SubjectFieldOnContactUsForm ? model.Subject : null;
                var body = _htmlFormatter.FormatText(model.Enquiry, false, true, false, false, false, false);

                await _workflowMessageService.SendContactVendorMessageAsync(vendor, (await _workContext.GetWorkingLanguageAsync()).Id,
                    model.Email.Trim(), model.FullName, subject, body);

                model.SuccessfullySent = true;
                model.Result = await _localizationService.GetResourceAsync("ContactVendor.YourEnquiryHasBeenSent");

                return View(model);
            }

            return View(model);
        }

        //sitemap page
        public virtual async Task<IActionResult> Sitemap(SitemapPageModel pageModel)
        {
            if (!_sitemapSettings.SitemapEnabled)
                return RedirectToRoute("Homepage");

            var model = await _sitemapModelFactory.PrepareSitemapModelAsync(pageModel);

            return View(model);
        }

        //SEO sitemap page
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> SitemapXml(int? id)
        {
            if (!_sitemapXmlSettings.SitemapXmlEnabled)
                return StatusCode(StatusCodes.Status403Forbidden);

            try
            {
                var sitemapXmlModel = await _sitemapModelFactory.PrepareSitemapXmlModelAsync(id ?? 0);
                return PhysicalFile(sitemapXmlModel.SitemapXmlPath, MimeTypes.ApplicationXml);
            }
            catch
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        public virtual async Task<IActionResult> SetStoreTheme(string themeName, string returnUrl = "")
        {
            await _themeContext.SetWorkingThemeNameAsync(themeName);

            //home page
            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("Homepage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("Homepage");

            return Redirect(returnUrl);
        }

        [HttpPost]
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> EuCookieLawAccept()
        {
            if (!_storeInformationSettings.DisplayEuCookieLawWarning)
                //disabled
                return Json(new { stored = false });

            //save setting
            var store = await _storeContext.GetCurrentStoreAsync();
            await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.EuCookieLawAcceptedAttribute, true, store.Id);
            return Json(new { stored = true });
        }

        //robots.txt file
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> RobotsTextFile()
        {
            var robotsFileContent = await _commonModelFactory.PrepareRobotsTextFileAsync();

            return Content(robotsFileContent, MimeTypes.TextPlain);
        }

        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual IActionResult GenericUrl()
        {
            //seems that no entity was found
            return InvokeHttp404();
        }

        //store is closed
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        public virtual IActionResult StoreClosed()
        {
            return View();
        }

        //helper method to redirect users. Workaround for GenericPathRoute class where we're not allowed to do it
        public virtual IActionResult InternalRedirect(string url, bool permanentRedirect)
        {
            //ensure it's invoked from our GenericPathRoute class
            if (!HttpContext.Items.TryGetValue(NopHttpDefaults.GenericRouteInternalRedirect, out var value) || value is not bool redirect || !redirect)
            {
                url = Url.RouteUrl("Homepage");
                permanentRedirect = false;
            }

            //home page
            if (string.IsNullOrEmpty(url))
            {
                url = Url.RouteUrl("Homepage");
                permanentRedirect = false;
            }

            //prevent open redirection attack
            if (!Url.IsLocalUrl(url))
            {
                url = Url.RouteUrl("Homepage");
                permanentRedirect = false;
            }

            if (permanentRedirect)
                return RedirectPermanent(url);

            return Redirect(url);
        }

        public virtual IActionResult GetPreviousOrderProduct()
        {
            var connection = DataSettingsManager.LoadSettings().ConnectionString;
            var conn = new SqlConnection(connection);
            PriceChange priceChange = new PriceChange();
            var customer = _workContext.GetCurrentCustomerAsync().Result;
            var GetallRoles = _customerService.GetCustomerRolesAsync(customer).Result.Where(d => d.Name == "CAD B2C"
                                                    || d.Name == "USD B2C"
                                                    || d.Name == "USD B2B INTL"
                                                    || d.Name == "CAD B2C HS"
                                                    || d.Name == "USD B2B ST"
                                                    || d.Name == "USD B2B DS"
                                                    || d.Name == "CAD B2B ST"
                                                    || d.Name == "CAD B2B DS"
                                                    ).FirstOrDefault();
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();
            List<GetPreviousOrderModel> responseModels = new List<GetPreviousOrderModel>();
            try
            {

                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@Customerid", customer.Id);
                    var result = conn1.Query("getpreviousorder", param: ds, commandType: CommandType.StoredProcedure);
                    foreach (var i in result)
                    {

                        GetPreviousOrderModel getPreviousOrderModel = new GetPreviousOrderModel();
                        var ISvalid = ValidProduct(Convert.ToInt32(i.PictureProductId), GetallRoles.Id);
                        if (ISvalid == "Valid")
                        {
                            var price = priceChange.getthelowestprice(Convert.ToInt32(i.PictureProductId), 4058, GetallRoles.Id);
                            if (price == "$0.00")
                            {
                                getPreviousOrderModel.Price = Gettierprice(Convert.ToInt32(i.PictureProductId), GetallRoles.Id);
                            }
                            else
                            {
                                getPreviousOrderModel.Price = price + _localizationService.GetResourceAsync("ShoppingCart.Startingfrom").Result;

                            }
                            var productDetail=_productService.GetProductByIdAsync(i.PictureProductId).Result;
                            var ProductSeName = _urlRecordService.GetSeNameAsync(productDetail).Result;
                            var isAssociatedProduct = false;
                            var picturedetail= _productModelFactory.getpictures(productDetail, isAssociatedProduct).Result;
                            // getPreviousOrderModel.Price = Convert.ToString(i.PriceExclTax);
                            //getPreviousOrderModel.Image = Convert.ToString(i.PictureDetails);
                            getPreviousOrderModel.Image = picturedetail.PictureModels[0].ThumbImageUrl;
                            getPreviousOrderModel.ProductId = Convert.ToInt32(i.PictureProductId);
                            getPreviousOrderModel.ProductName = Convert.ToString(i.ProductName);
                            getPreviousOrderModel.UrlSlug = ProductSeName;
                            responseModels.Add(getPreviousOrderModel);

                        }
                     }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                conn.Close();
            }
            string partialView = "~/Views/Common/GetPreviousOrderProduct.cshtml";
            if (responseModels.Count() > 0)
            {
                return Json(new { result = 1, innerhtml = RenderPartialViewToStringAsync(partialView, responseModels).Result });
            }
            else
            {
                return Json(new { result = 0, innerhtml = RenderPartialViewToStringAsync(partialView, responseModels).Result });
            }
        }
        public string Gettierprice(int productid, int customerroleid)
        {

            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var conn = new SqlConnection(connstring);
            var dt = new DataTable();
            try
            {

                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();



                //var Customercmd = new SqlCommand("select count(*) from QueuedEmail where Subject ='" + Name + "' and CreatedOnUtc = GETDATE()", conn);
                var Customercmd = new SqlCommand("select Price from TierPrice where ProductId='" + productid + "' and CustomerRoleId ='" + customerroleid + "'", conn);
                Customercmd.CommandType = CommandType.Text;
                Customercmd.CommandTimeout = 0;


                da = new SqlDataAdapter(Customercmd);
                dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    var aa = dt.Rows[0].ItemArray[0];
                    return string.Format("{0:c}", aa);


                }
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
            finally
            {
                conn.Close();
            }
        }
        public string ValidProduct(int productid, int customerroleid)
        {

            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var conn = new SqlConnection(connstring);
            var dt = new DataTable();
            try
            {

                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();



                //var Customercmd = new SqlCommand("select count(*) from QueuedEmail where Subject ='" + Name + "' and CreatedOnUtc = GETDATE()", conn);
                var Customercmd = new SqlCommand("select * from [AclRecord] where EntityId='" + productid + "' and CustomerRoleId ='" + customerroleid + "'", conn);
                Customercmd.CommandType = CommandType.Text;
                Customercmd.CommandTimeout = 0;


                da = new SqlDataAdapter(Customercmd);
                dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {

                    return "Valid";


                }
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
            finally
            {
                conn.Close();
            }
        }
        #endregion
    }
}