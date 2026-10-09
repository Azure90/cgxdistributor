using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Gdpr;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Tax;
using Nop.Core.Events;
using Nop.Core.Http;
using Nop.Core.Http.Extensions;
using Nop.Core.Infrastructure;
using Nop.Services.Authentication;
using Nop.Services.Authentication.External;
using Nop.Services.Authentication.MultiFactor;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.ExportImport;
using Nop.Services.Gdpr;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Tax;
using Nop.Web.Extensions;
using Nop.Web.Factories;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Validators;
using Nop.Web.Models.Customer;
using Nop.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Invoices;
using Nop.Web.Models.OfflineTransaction;
using System.IO;
using Nop.Web.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.Net;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Model;
using Nop.Services.Configuration;
using Dapper;
using Nop.Web.Models.AssociatedCustomer;
using Nop.Core.Caching;
using static Nop.Web.Areas.Admin.Controllers.CommonController;
using System.Globalization;
using Nop.Services.Seo;
using Nop.Web.Areas.Admin.Models.Reports;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Services.KnowledgeBaseArticle;
using Nop.Web.Models.KnowledgeBaseArticle;
using Nop.Web.Models.Media;
using System.Text.RegularExpressions;
using static Nop.Web.Models.Order.CustomerOrderListModel;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Order.PreOrders;
using Task = System.Threading.Tasks.Task;

namespace Nop.Web.Controllers
{
    [AutoValidateAntiforgeryToken]
    public partial class CustomerController : BasePublicController
    {
        #region Fields

        private readonly AddressSettings _addressSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly DateTimeSettings _dateTimeSettings;
        private readonly ForumSettings _forumSettings;
        private readonly GdprSettings _gdprSettings;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IAddressModelFactory _addressModelFactory;
        private readonly IAddressService _addressService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerAttributeParser _customerAttributeParser;
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly ICustomerModelFactory _customerModelFactory;
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly ICustomerService _customerService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IDownloadService _downloadService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IExportManager _exportManager;
        private readonly IExternalAuthenticationService _externalAuthenticationService;
        private readonly IGdprService _gdprService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IGiftCardService _giftCardService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IMultiFactorAuthenticationPluginManager _multiFactorAuthenticationPluginManager;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly INotificationService _notificationService;
        private readonly IOrderService _orderService;
        private readonly IPermissionService _permissionService;
        private readonly IPictureService _pictureService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductService _productService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly MultiFactorAuthenticationSettings _multiFactorAuthenticationSettings;
        private readonly StoreInformationSettings _storeInformationSettings;
        private readonly TaxSettings _taxSettings;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IPdfService _pdfService;
        private readonly OrderSettings _orderSettings;
        private readonly IConfiguration _configuration;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly ISettingService _settingService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IOrderModelFactory _orderModelFactory;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly ICustomDocumentService _customDocumentService;
        private readonly IPreOrderService _preOrderService;
        private readonly IOrderProcessingService _orderProcessingService;

        #endregion

        #region Ctor

        public CustomerController(AddressSettings addressSettings,
            CaptchaSettings captchaSettings,
            CustomerSettings customerSettings,
            DateTimeSettings dateTimeSettings,
            ForumSettings forumSettings,
            GdprSettings gdprSettings,
            HtmlEncoder htmlEncoder,
            IAddressAttributeParser addressAttributeParser,
            IAddressModelFactory addressModelFactory,
            IAddressService addressService,
            IAuthenticationService authenticationService,
            ICountryService countryService,
            ICurrencyService currencyService,
            IDateTimeHelper dateTimeHelper,
            ICustomerActivityService customerActivityService,
            ICustomerAttributeParser customerAttributeParser,
            ICustomerAttributeService customerAttributeService,
            ICustomerModelFactory customerModelFactory,
            ICustomerRegistrationService customerRegistrationService,
            ICustomerService customerService,
            IDownloadService downloadService,
            IEventPublisher eventPublisher,
            IExportManager exportManager,
            IExternalAuthenticationService externalAuthenticationService,
            IGdprService gdprService,
            IGenericAttributeService genericAttributeService,
            IGiftCardService giftCardService,
            ILocalizationService localizationService,
            ILogger logger,
            IMultiFactorAuthenticationPluginManager multiFactorAuthenticationPluginManager,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            INotificationService notificationService,
            IOrderService orderService,
            IPermissionService permissionService,
            IPictureService pictureService,
            IPriceFormatter priceFormatter,
            IProductService productService,
            IStateProvinceService stateProvinceService,
            IStoreContext storeContext,
            ITaxService taxService,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            LocalizationSettings localizationSettings,
            MediaSettings mediaSettings,
            MultiFactorAuthenticationSettings multiFactorAuthenticationSettings,
            StoreInformationSettings storeInformationSettings,
            TaxSettings taxSettings,
            IPdfService pdfService,
            OrderSettings orderSettings,
            IEmailAccountService emailAccountService,
           IConfiguration configuration,
           ISettingService settingService,
           EmailAccountSettings emailAccountSettings,
           IQueuedEmailService queuedEmailService,

            IUrlRecordService urlRecordService,
           IOrderModelFactory orderModelFactory,
           IStaticCacheManager staticCacheManager,
           ICustomDocumentService customDocumentService,
           IPreOrderService preOrderService,
           IOrderProcessingService orderProcessingService)
        {
            _addressSettings = addressSettings;
            _captchaSettings = captchaSettings;
            _customerSettings = customerSettings;
            _dateTimeSettings = dateTimeSettings;
            _forumSettings = forumSettings;
            _gdprSettings = gdprSettings;
            _htmlEncoder = htmlEncoder;
            _addressAttributeParser = addressAttributeParser;
            _addressModelFactory = addressModelFactory;
            _addressService = addressService;
            _authenticationService = authenticationService;
            _countryService = countryService;
            _currencyService = currencyService;
            _customerActivityService = customerActivityService;
            _customerAttributeParser = customerAttributeParser;
            _customerAttributeService = customerAttributeService;
            _customerModelFactory = customerModelFactory;
            _customerRegistrationService = customerRegistrationService;
            _customerService = customerService;
            _downloadService = downloadService;
            _eventPublisher = eventPublisher;
            _exportManager = exportManager;
            _externalAuthenticationService = externalAuthenticationService;
            _gdprService = gdprService;
            _genericAttributeService = genericAttributeService;
            _giftCardService = giftCardService;
            _localizationService = localizationService;
            _logger = logger;
            _multiFactorAuthenticationPluginManager = multiFactorAuthenticationPluginManager;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _notificationService = notificationService;
            _orderService = orderService;
            _permissionService = permissionService;
            _pictureService = pictureService;
            _priceFormatter = priceFormatter;
            _productService = productService;
            _stateProvinceService = stateProvinceService;
            _storeContext = storeContext;
            _taxService = taxService;
            _workContext = workContext;
            _workflowMessageService = workflowMessageService;
            _localizationSettings = localizationSettings;
            _mediaSettings = mediaSettings;
            _multiFactorAuthenticationSettings = multiFactorAuthenticationSettings;
            _storeInformationSettings = storeInformationSettings;
            _taxSettings = taxSettings;
            _pdfService = pdfService;
            _orderSettings = orderSettings;
            _emailAccountService = emailAccountService;
            _configuration = configuration;
            _emailAccountSettings = emailAccountSettings;
            _settingService = settingService;
            _staticCacheManager = staticCacheManager;
            _queuedEmailService = queuedEmailService;
            _urlRecordService = urlRecordService;
            _dateTimeHelper = dateTimeHelper;
            _orderModelFactory = orderModelFactory;
            _customDocumentService = customDocumentService;
            _preOrderService = preOrderService;
            _orderProcessingService = orderProcessingService;
        }

        #endregion

        #region Utilities

        protected virtual void ValidateRequiredConsents(List<GdprConsent> consents, IFormCollection form)
        {
            foreach (var consent in consents)
            {
                var controlId = $"consent{consent.Id}";
                var cbConsent = form[controlId];
                if (StringValues.IsNullOrEmpty(cbConsent) || !cbConsent.ToString().Equals("on"))
                {
                    ModelState.AddModelError("", consent.RequiredMessage);
                }
            }
        }

        protected virtual async Task<string> ParseSelectedProviderAsync(IFormCollection form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var store = await _storeContext.GetCurrentStoreAsync();

            var multiFactorAuthenticationProviders = await _multiFactorAuthenticationPluginManager.LoadActivePluginsAsync(await _workContext.GetCurrentCustomerAsync(), store.Id);
            foreach (var provider in multiFactorAuthenticationProviders)
            {
                var controlId = $"provider_{provider.PluginDescriptor.SystemName}";

                var curProvider = form[controlId];
                if (!StringValues.IsNullOrEmpty(curProvider))
                {
                    var selectedProvider = curProvider.ToString();
                    if (!string.IsNullOrEmpty(selectedProvider))
                    {
                        return selectedProvider;
                    }
                }
            }
            return string.Empty;
        }

        protected virtual async Task<string> ParseCustomCustomerAttributesAsync(IFormCollection form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var attributesXml = "";
            var attributes = await _customerAttributeService.GetAllCustomerAttributesAsync();
            foreach (var attribute in attributes)
            {
                var controlId = $"{NopCustomerServicesDefaults.CustomerAttributePrefix}{attribute.Id}";
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                            {
                                var selectedAttributeId = int.Parse(ctrlAttributes);
                                if (selectedAttributeId > 0)
                                    attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                        attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            var cblAttributes = form[controlId];
                            if (!StringValues.IsNullOrEmpty(cblAttributes))
                            {
                                foreach (var item in cblAttributes.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    var selectedAttributeId = int.Parse(item);
                                    if (selectedAttributeId > 0)
                                        attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                            attribute, selectedAttributeId.ToString());
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = await _customerAttributeService.GetCustomerAttributeValuesAsync(attribute.Id);
                            foreach (var selectedAttributeId in attributeValues
                                .Where(v => v.IsPreSelected)
                                .Select(v => v.Id)
                                .ToList())
                            {
                                attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                    attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                            {
                                var enteredText = ctrlAttributes.ToString().Trim();
                                attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                    attribute, enteredText);
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                    case AttributeControlType.FileUpload:
                    //not supported customer attributes
                    default:
                        break;
                }
            }

            return attributesXml;
        }

        protected virtual async System.Threading.Tasks.Task LogGdprAsync(Customer customer, CustomerInfoModel oldCustomerInfoModel,
            CustomerInfoModel newCustomerInfoModel, IFormCollection form)
        {
            try
            {
                //consents
                var consents = (await _gdprService.GetAllConsentsAsync()).Where(consent => consent.DisplayOnCustomerInfoPage).ToList();
                foreach (var consent in consents)
                {
                    var previousConsentValue = await _gdprService.IsConsentAcceptedAsync(consent.Id, customer.Id);
                    var controlId = $"consent{consent.Id}";
                    var cbConsent = form[controlId];
                    if (!StringValues.IsNullOrEmpty(cbConsent) && cbConsent.ToString().Equals("on"))
                    {
                        //agree
                        if (!previousConsentValue.HasValue || !previousConsentValue.Value)
                        {
                            await _gdprService.InsertLogAsync(customer, consent.Id, GdprRequestType.ConsentAgree, consent.Message);
                        }
                    }
                    else
                    {
                        //disagree
                        if (!previousConsentValue.HasValue || previousConsentValue.Value)
                        {
                            await _gdprService.InsertLogAsync(customer, consent.Id, GdprRequestType.ConsentDisagree, consent.Message);
                        }
                    }
                }

                //newsletter subscriptions
                if (_gdprSettings.LogNewsletterConsent)
                {
                    if (oldCustomerInfoModel.Newsletter && !newCustomerInfoModel.Newsletter)
                        await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ConsentDisagree, await _localizationService.GetResourceAsync("Gdpr.Consent.Newsletter"));
                    if (!oldCustomerInfoModel.Newsletter && newCustomerInfoModel.Newsletter)
                        await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ConsentAgree, await _localizationService.GetResourceAsync("Gdpr.Consent.Newsletter"));
                }

                //user profile changes
                if (!_gdprSettings.LogUserProfileChanges)
                    return;

                if (oldCustomerInfoModel.Gender != newCustomerInfoModel.Gender)
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.Gender")} = {newCustomerInfoModel.Gender}");

                if (oldCustomerInfoModel.FirstName != newCustomerInfoModel.FirstName)
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.FirstName")} = {newCustomerInfoModel.FirstName}");

                if (oldCustomerInfoModel.LastName != newCustomerInfoModel.LastName)
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.LastName")} = {newCustomerInfoModel.LastName}");

                if (oldCustomerInfoModel.ParseDateOfBirth() != newCustomerInfoModel.ParseDateOfBirth())
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.DateOfBirth")} = {newCustomerInfoModel.ParseDateOfBirth()}");

                if (oldCustomerInfoModel.Email != newCustomerInfoModel.Email)
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.Email")} = {newCustomerInfoModel.Email}");

                if (oldCustomerInfoModel.Company != newCustomerInfoModel.Company)
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.Company")} = {newCustomerInfoModel.Company}");

                if (oldCustomerInfoModel.StreetAddress != newCustomerInfoModel.StreetAddress)
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.StreetAddress")} = {newCustomerInfoModel.StreetAddress}");

                if (oldCustomerInfoModel.StreetAddress2 != newCustomerInfoModel.StreetAddress2)
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.StreetAddress2")} = {newCustomerInfoModel.StreetAddress2}");

                if (oldCustomerInfoModel.ZipPostalCode != newCustomerInfoModel.ZipPostalCode)
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.ZipPostalCode")} = {newCustomerInfoModel.ZipPostalCode}");

                if (oldCustomerInfoModel.City != newCustomerInfoModel.City)
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.City")} = {newCustomerInfoModel.City}");

                if (oldCustomerInfoModel.County != newCustomerInfoModel.County)
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.County")} = {newCustomerInfoModel.County}");

                if (oldCustomerInfoModel.CountryId != newCustomerInfoModel.CountryId)
                {
                    var countryName = (await _countryService.GetCountryByIdAsync(newCustomerInfoModel.CountryId))?.Name;
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.Country")} = {countryName}");
                }

                if (oldCustomerInfoModel.StateProvinceId != newCustomerInfoModel.StateProvinceId)
                {
                    var stateProvinceName = (await _stateProvinceService.GetStateProvinceByIdAsync(newCustomerInfoModel.StateProvinceId))?.Name;
                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ProfileChanged, $"{await _localizationService.GetResourceAsync("Account.Fields.StateProvince")} = {stateProvinceName}");
                }
            }
            catch (Exception exception)
            {
                await _logger.ErrorAsync(exception.Message, exception, customer);
            }
        }

        #endregion

        #region Methods

        #region Login / logout

        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> Login(bool? checkoutAsGuest)
        {
            var model = await _customerModelFactory.PrepareLoginModelAsync(checkoutAsGuest);
            var customer = await _workContext.GetCurrentCustomerAsync();

            if (await _customerService.IsRegisteredAsync(customer))
            {
                var fullName = await _customerService.GetCustomerFullNameAsync(customer);
                var message = await _localizationService.GetResourceAsync("Account.Login.AlreadyLogin");
                _notificationService.SuccessNotification(string.Format(message, _htmlEncoder.Encode(fullName)));

            }
            ViewBag.Isauthenticated = _settingService.GetSetting("Authentication.Isactive").Value.ToLower();
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
            return View(model);
        }

        [HttpPost]
        [ValidateCaptcha]
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> Login(LoginModel model, string returnUrl, bool captchaValid)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage && !captchaValid)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
            }

            if (ModelState.IsValid)
            {
                SetSeesionBanner();
                var customerUserName = model.Username?.Trim();
                var customerEmail = model.Email?.Trim();
                var userNameOrEmail = _customerSettings.UsernamesEnabled ? customerUserName : customerEmail;

                var loginResult = await _customerRegistrationService.ValidateCustomerAsync(userNameOrEmail, model.Password);
                switch (loginResult)
                {
                    case CustomerLoginResults.Successful:
                        {
                            var customer = _customerSettings.UsernamesEnabled
                                ? await _customerService.GetCustomerByUsernameAsync(customerUserName)
                                : await _customerService.GetCustomerByEmailAsync(customerEmail);

                            return await _customerRegistrationService.SignInCustomerAsync(customer, returnUrl, model.RememberMe);
                        }
                    case CustomerLoginResults.MultiFactorAuthenticationRequired:
                        {
                            var customerMultiFactorAuthenticationInfo = new CustomerMultiFactorAuthenticationInfo
                            {
                                UserName = userNameOrEmail,
                                RememberMe = model.RememberMe,
                                ReturnUrl = returnUrl
                            };
                            HttpContext.Session.Set(NopCustomerDefaults.CustomerMultiFactorAuthenticationInfo, customerMultiFactorAuthenticationInfo);
                            return RedirectToRoute("MultiFactorVerification");
                        }
                    case CustomerLoginResults.CustomerNotExist:
                        ModelState.AddModelError("", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.CustomerNotExist"));
                        break;
                    case CustomerLoginResults.Deleted:
                        ModelState.AddModelError("", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.Deleted"));
                        break;
                    case CustomerLoginResults.NotActive:
                        ModelState.AddModelError("", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotActive"));
                        break;
                    case CustomerLoginResults.NotRegistered:
                        ModelState.AddModelError("", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotRegistered"));
                        break;
                    case CustomerLoginResults.LockedOut:
                        ModelState.AddModelError("", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.LockedOut"));
                        break;
                    case CustomerLoginResults.WrongPassword:
                    default:
                        ModelState.AddModelError("", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials"));
                        break;
                }
            }

            //If we got this far, something failed, redisplay form
            model = await _customerModelFactory.PrepareLoginModelAsync(model.CheckoutAsGuest);
            return View(model);
        }


        public virtual async Task<IActionResult> Loginwithcode(LoginModel model, string returnUrl, bool captchaValid, string visit, string uniquecode, string ToEMail, string Pasword)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage && !captchaValid)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
            }

            var customerlastlogin = _customerService.GetCustomerByEmailAsync(ToEMail).Result?.LastLoginDateUtc;
            bool isSuccess = false;

            var loginResult = await _customerRegistrationService.ValidateAuthenticationCustomerAsync(ToEMail, Pasword);
            switch (loginResult)
            {
                case CustomerLoginResults.Successful:
                    {
                        isSuccess = true;
                        break;
                    }

            }

            if (isSuccess)
            {
                var maxTries = 3;
                //var getqueued = _queuedEmailService.SearchEmailsAsync(null, ToEMail, null, null, null, false, false, 1, false, 0, 500);
                var queuedEmails = await _queuedEmailService.SearchEmailsAsync(null, ToEMail, null, null, null,
                false, false, maxTries, false, 0, 500);
                var filtemails = queuedEmails.Where(x => x.Subject.Contains("Multifactor Authentication")).ToList();
                DateTime currentTimeMinus10 = DateTime.UtcNow.AddMinutes(-10);
                var checkemail = filtemails.Where(y => y.SentOnUtc > currentTimeMinus10).ToList();
                DateTime currentUtcDateTime = DateTime.UtcNow;
                var authenticationtime = _settingService.GetSetting("Authentication.Time").Value.ToLower();
                var asduthenticationtime = TimeSpan.FromMinutes(Convert.ToDouble(authenticationtime));
                var validtime = currentUtcDateTime - customerlastlogin;
                var Isauthenticated = _settingService.GetSetting("Authentication.Isactive").Value.ToLower();
                if (validtime <= asduthenticationtime && Isauthenticated == "true" && uniquecode == null)
                {
                    return Json(new { result = 3 });
                }
                else
                {
                    if (uniquecode == null)
                    {
                        if (checkemail.Count() == 0)
                        {
                            try
                            {
                                var emailAccount = _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId).Result;
                                var ApiKey = "";
                                var DisplayName = "";
                                var emailAccountEmail = "";
                                string subject = "Citagenix Multifactor Authentication - Identity verification security token";
                                var GUID = Guid.NewGuid().ToString().Substring(0, 8);
                                var Password = "";
                                ApiKey = emailAccount.ApiKey;
                                DisplayName = emailAccount.DisplayName;
                                emailAccountEmail = emailAccount.Email;
                                Password = emailAccount.Password;




                                if (!sib_api_v3_sdk.Client.Configuration.Default.ApiKey.ContainsKey("api-key"))
                                    sib_api_v3_sdk.Client.Configuration.Default.ApiKey.Add("api-key", ApiKey);
                                {
                                    var apiInstance = new TransactionalEmailsApi();
                                    List<SendSmtpEmailAttachment> Attachment = new List<SendSmtpEmailAttachment>();
                                    string SenderName = string.IsNullOrEmpty(DisplayName) ? null : DisplayName;
                                    string SenderEmail = emailAccountEmail;
                                    SendSmtpEmailSender Email = new SendSmtpEmailSender(SenderName, SenderEmail);
                                    List<SendSmtpEmailTo> To = new List<SendSmtpEmailTo>();
                                    List<SendSmtpEmailBcc> BccTO = new List<SendSmtpEmailBcc>();
                                    string ToEmail = ToEMail;
                                    string ToName = string.IsNullOrEmpty(ToEMail) ? null : ToEMail;
                                    var BccMail = _localizationService.GetResourceAsync("Customer.AunthenticationBcc").Result;


                                    if (ToEmail.Contains(";"))
                                    {
                                        var listemail = ToEmail.Split(";");
                                        foreach (var i in listemail)
                                        {
                                            string tobcc = i;
                                            SendSmtpEmailTo smtpto = new SendSmtpEmailTo(tobcc, ToName);
                                            To.Add(smtpto);
                                        }

                                    }
                                    else
                                    {
                                        SendSmtpEmailTo smtpEmailTo = new SendSmtpEmailTo(ToEmail, ToName);

                                        To.Add(smtpEmailTo);
                                    }
                                    if (BccMail != "No")
                                    {
                                        if (BccMail.Contains(";"))
                                        {
                                            var listemail = BccMail.Split(";");

                                            foreach (var i in listemail)
                                            {
                                                string tobcc = i;
                                                SendSmtpEmailBcc smtpbcc = new SendSmtpEmailBcc(tobcc, null);
                                                BccTO.Add(smtpbcc);
                                            }
                                        }
                                        else
                                        {
                                            SendSmtpEmailBcc smtpbcc = new SendSmtpEmailBcc(BccMail, null);

                                            BccTO.Add(smtpbcc);
                                        }
                                    }

                                    var NAme = _customerService.GetCustomerByEmailAsync(ToEMail).Result.FirstName + " " + _customerService.GetCustomerByEmailAsync(ToEMail).Result.LastName;

                                    //var body = "Please use the following Email Verification security token:" + GUID;
                                    var body = $" Hello {NAme}</br></br>\" " +
                                        $" Please use the following <strong>Email Verification security token: {GUID}</strong></br>" +
                                        $"***This token will be valid for the next <strong>10 minutes.</strong>***</br></br>" +
                                        $"To avoid errors, you can copy and paste this token directly into the <strong>“Verification Code“</strong> field.</br>" +
                                        $"After providing the security token, you can start using the site immediately.</br></br>" +
                                        $"***This is an auto-generated email. Please do not reply to this sender.</br></br>" +
                                        $"Thanks";
                                    var Subject = subject;
                                    Newtonsoft.Json.Linq.JObject Headers = new Newtonsoft.Json.Linq.JObject();
                                    // Headers.Add(emailAccountEmail, Password);
                                    Headers.Add("content-type", "application/json");
                                    Headers.Add("api-key", ApiKey);
                                    ServicePointManager.Expect100Continue = true;
                                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                                    // var sendSmtpEmail = new SendSmtpEmail(Email, To, null, null, body, null, Subject, null, null, Headers, null, null, null, null);
                                    var sendSmtpEmail = new SendSmtpEmail(Email, To, BccMail == "No" ? null : BccTO, null, body, null, Subject, null, null, Headers, null, null, null, null);
                                    //  CreateSmtpEmail result = apiInstance.SendTransacEmail(sendSmtpEmail);
                                    if (ToEMail != null)
                                    {
                                        var email = new QueuedEmail
                                        {
                                            PriorityId = 5,
                                            EmailAccountId = emailAccount.Id,
                                            FromName = emailAccount.DisplayName,
                                            From = emailAccount.Email,
                                            ToName = ToName,
                                            To = ToEmail,
                                            Subject = subject,
                                            Body = body,
                                            Bcc = BccMail,
                                            CreatedOnUtc = DateTime.UtcNow,
                                            DontSendBeforeDateUtc = null,
                                            SentTries = 1,
                                            SentOnUtc = DateTime.UtcNow,
                                            // MessageCode = result.MessageId

                                        };
                                        //  await _queuedEmailService.InsertQueuedEmailAsync(email);
                                        //var currcustomer = _customerService.GetCustomerByEmailAsync(ToEMail).Result;
                                        // currcustomer.AuthenticationCode = GUID;
                                        //  currcustomer.LastLoginDateUtc = customerlastlogin;
                                        //  await _customerService.UpdateCustomerAsync(currcustomer);

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to send email: {ex.Message}");
                            }
                            return Json(new { result = 0 });

                        }
                        else
                        {
                            return Json(new { result = 0 });
                        }
                    }
                    else
                    {
                        var currcustomer = _customerService.GetCustomerByEmailAsync(ToEMail).Result;
                        if (currcustomer.AuthenticationCode == uniquecode)
                        {
                            return Json(new { result = 1 });
                        }
                    }
                }
            }
            else
            {
                return Json(new { result = 4 });
            }
            //If we got this far, something failed, redisplay form
            //model = await _customerModelFactory.PrepareLoginModelAsync(model.CheckoutAsGuest);
            //return View(model);
            return Json(new { result = 2 });
        }
        /// <summary>
        /// The entry point for injecting a plugin component of type "MultiFactorAuth"
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the user verification page for Multi-factor authentication. Served by an authentication provider.
        /// </returns>
        public virtual async Task<IActionResult> MultiFactorVerification()
        {
            if (!await _multiFactorAuthenticationPluginManager.HasActivePluginsAsync())
                return RedirectToRoute("Login");

            var customerMultiFactorAuthenticationInfo = HttpContext.Session.Get<CustomerMultiFactorAuthenticationInfo>(NopCustomerDefaults.CustomerMultiFactorAuthenticationInfo);
            var userName = customerMultiFactorAuthenticationInfo?.UserName;
            if (string.IsNullOrEmpty(userName))
                return RedirectToRoute("Homepage");

            var customer = _customerSettings.UsernamesEnabled ? await _customerService.GetCustomerByUsernameAsync(userName) : await _customerService.GetCustomerByEmailAsync(userName);
            if (customer == null)
                return RedirectToRoute("Homepage");

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableMultiFactorAuthentication, customer))
                return RedirectToRoute("Homepage");

            var selectedProvider = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.SelectedMultiFactorAuthenticationProviderAttribute);
            if (string.IsNullOrEmpty(selectedProvider))
                return RedirectToRoute("Homepage");

            var model = new MultiFactorAuthenticationProviderModel();
            model = await _customerModelFactory.PrepareMultiFactorAuthenticationProviderModelAsync(model, selectedProvider, true);

            return View(model);
        }

        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> Logout()
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (_workContext.OriginalCustomerIfImpersonated != null)
            {
                //activity log
                await _customerActivityService.InsertActivityAsync(_workContext.OriginalCustomerIfImpersonated, "Impersonation.Finished",
                    string.Format(await _localizationService.GetResourceAsync("ActivityLog.Impersonation.Finished.StoreOwner"),
                        customer.Email, customer.Id),
                    customer);

                await _customerActivityService.InsertActivityAsync("Impersonation.Finished",
                    string.Format(await _localizationService.GetResourceAsync("ActivityLog.Impersonation.Finished.Customer"),
                        _workContext.OriginalCustomerIfImpersonated.Email, _workContext.OriginalCustomerIfImpersonated.Id),
                    _workContext.OriginalCustomerIfImpersonated);

                //logout impersonated customer
                await _genericAttributeService
                    .SaveAttributeAsync<int?>(_workContext.OriginalCustomerIfImpersonated, NopCustomerDefaults.ImpersonatedCustomerIdAttribute, null);

                //redirect back to customer details page (admin area)

                //var myValue = Session["ImpersonateCustomer"].ToString();
                HttpContext.Request.Cookies.TryGetValue("ImpersonateCustomer", out var cookieLanguageCode);
                HttpContext.Request.Cookies.TryGetValue("ImpersonateCustomerid", out var LanguageCode);
                if (cookieLanguageCode != "Impersonate")
                {

                    return RedirectToAction("Edit", "Customer", new { id = customer.Id, area = AreaNames.Admin });
                }
                else
                {
                    var ImpersonateCustomer = EngineContext.Current.Resolve<IHttpContextAccessor>().HttpContext.Response.Cookies;
                    ImpersonateCustomer.Delete("ImpersonateCustomer");
                    ImpersonateCustomer.Delete("ImpersonateCustomerid");
                    return RedirectToAction("Index", "Home", new { area = string.Empty });
                }

            }

            //code by Tp
            var session = EngineContext.Current.Resolve<IHttpContextAccessor>().HttpContext.Response.Cookies;
            session.Delete("Pollpopup");

            //activity log
            await _customerActivityService.InsertActivityAsync(customer, "PublicStore.Logout",
                await _localizationService.GetResourceAsync("ActivityLog.PublicStore.Logout"), customer);

            //standard logout 
            await _authenticationService.SignOutAsync();

            //raise logged out event       
            await _eventPublisher.PublishAsync(new CustomerLoggedOutEvent(customer));

            //EU Cookie
            if (_storeInformationSettings.DisplayEuCookieLawWarning)
            {
                //the cookie law message should not pop up immediately after logout.
                //otherwise, the user will have to click it again...
                //and thus next visitor will not click it... so violation for that cookie law..
                //the only good solution in this case is to store a temporary variable
                //indicating that the EU cookie popup window should not be displayed on the next page open (after logout redirection to homepage)
                //but it'll be displayed for further page loads
                TempData[$"{NopCookieDefaults.Prefix}{NopCookieDefaults.IgnoreEuCookieLawWarning}"] = true;
            }

            return RedirectToRoute("Homepage");
        }


        #region Custom Code by Tp

        [IgnoreAntiforgeryToken]
        public IActionResult SetSeesionBanner()
        {
            var cookies = EngineContext.Current.Resolve<IHttpContextAccessor>().HttpContext.Response.Cookies;
            cookies.Append("Pollpopup", "Pollpopup");

            return Json(1);
        }

        #endregion

        #endregion

        #region Password recovery

        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        public virtual async Task<IActionResult> PasswordRecovery()
        {
            var model = new PasswordRecoveryModel();
            model = await _customerModelFactory.PreparePasswordRecoveryModelAsync(model);
            return View(model);
        }

        [ValidateCaptcha]
        [HttpPost, ActionName("PasswordRecovery")]
        [FormValueRequired("send-email")]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        public virtual async Task<IActionResult> PasswordRecoverySend(PasswordRecoveryModel model, bool captchaValid)
        {
            // validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnForgotPasswordPage && !captchaValid)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
            }

            if (ModelState.IsValid)
            {
                var customer = await _customerService.GetCustomerByEmailAsync(model.Email.Trim());
                if (customer != null && customer.Active && !customer.Deleted)
                {
                    //save token and current date
                    var passwordRecoveryToken = Guid.NewGuid();
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PasswordRecoveryTokenAttribute,
                        passwordRecoveryToken.ToString());
                    DateTime? generatedDateTime = DateTime.UtcNow;
                    await _genericAttributeService.SaveAttributeAsync(customer,
                        NopCustomerDefaults.PasswordRecoveryTokenDateGeneratedAttribute, generatedDateTime);

                    //send email
                    await _workflowMessageService.SendCustomerPasswordRecoveryMessageAsync(customer,
                        (await _workContext.GetWorkingLanguageAsync()).Id);

                    _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Account.PasswordRecovery.EmailHasBeenSent"));
                }
                else
                {
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Account.PasswordRecovery.EmailNotFound"));
                }
            }

            model = await _customerModelFactory.PreparePasswordRecoveryModelAsync(model);

            return View(model);
        }

        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        public virtual async Task<IActionResult> PasswordRecoveryConfirm(string token, string email, Guid guid)
        {
            //For backward compatibility with previous versions where email was used as a parameter in the URL
            var customer = await _customerService.GetCustomerByEmailAsync(email)
                ?? await _customerService.GetCustomerByGuidAsync(guid);

            if (customer == null)
                return RedirectToRoute("Homepage");

            var model = new PasswordRecoveryConfirmModel { ReturnUrl = Url.RouteUrl("Homepage") };
            if (string.IsNullOrEmpty(await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.PasswordRecoveryTokenAttribute)))
            {
                model.DisablePasswordChanging = true;
                model.Result = await _localizationService.GetResourceAsync("Account.PasswordRecovery.PasswordAlreadyHasBeenChanged");
                return View(model);
            }

            //validate token
            if (!await _customerService.IsPasswordRecoveryTokenValidAsync(customer, token))
            {
                model.DisablePasswordChanging = true;
                model.Result = await _localizationService.GetResourceAsync("Account.PasswordRecovery.WrongToken");
                return View(model);
            }

            //validate token expiration date
            if (await _customerService.IsPasswordRecoveryLinkExpiredAsync(customer))
            {
                model.DisablePasswordChanging = true;
                model.Result = await _localizationService.GetResourceAsync("Account.PasswordRecovery.LinkExpired");
                return View(model);
            }

            return View(model);
        }

        [HttpPost, ActionName("PasswordRecoveryConfirm")]
        [FormValueRequired("set-password")]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        public virtual async Task<IActionResult> PasswordRecoveryConfirmPOST(string token, string email, Guid guid, PasswordRecoveryConfirmModel model)
        {
            //For backward compatibility with previous versions where email was used as a parameter in the URL
            var customer = await _customerService.GetCustomerByEmailAsync(email)
                ?? await _customerService.GetCustomerByGuidAsync(guid);

            if (customer == null)
                return RedirectToRoute("Homepage");

            model.ReturnUrl = Url.RouteUrl("Homepage");

            //validate token
            if (!await _customerService.IsPasswordRecoveryTokenValidAsync(customer, token))
            {
                model.DisablePasswordChanging = true;
                model.Result = await _localizationService.GetResourceAsync("Account.PasswordRecovery.WrongToken");
                return View(model);
            }

            //validate token expiration date
            if (await _customerService.IsPasswordRecoveryLinkExpiredAsync(customer))
            {
                model.DisablePasswordChanging = true;
                model.Result = await _localizationService.GetResourceAsync("Account.PasswordRecovery.LinkExpired");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            var response = await _customerRegistrationService
                .ChangePasswordAsync(new ChangePasswordRequest(customer.Email, false, _customerSettings.DefaultPasswordFormat, model.NewPassword));
            if (!response.Success)
            {
                model.Result = string.Join(';', response.Errors);
                return View(model);
            }

            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PasswordRecoveryTokenAttribute, "");

            //authenticate customer after changing password
            await _customerRegistrationService.SignInCustomerAsync(customer, null, true);

            model.DisablePasswordChanging = true;
            model.Result = await _localizationService.GetResourceAsync("Account.PasswordRecovery.PasswordHasBeenChanged");
            return View(model);
        }

        #endregion     

        #region Register

        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> Register(string returnUrl)
        {
            //check whether registration is allowed
            if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled, returnUrl });

            var model = new RegisterModel();
            model = _customerModelFactory.PrepareRegisterModelAsync(model, false, setDefaultValues: true).Result;

            if (string.IsNullOrEmpty(model.Country))
            {
                var CustomerDetails = _workContext.GetCurrentCustomerAsync().Result;

                var rolename = _customerService.GetCustomerRolesAsync(CustomerDetails).Result;//.Where(d => d.Id == 41 || d.Id == 42 || d.Id == 43);
                if (rolename.Any(d => d.Id == 4))
                {
                    if (rolename.Any(d => d.Name.ToLower().Contains("usd") && d.Id != 43))
                    {
                        model.Country = "USA";
                    }
                    if (rolename.Any(d => d.Name.ToLower().Contains("cad")))
                    {
                        model.Country = "CANADA";
                    }
                    if (rolename.Any(d => d.Name.ToLower().Contains("intl")))
                    {
                        model.Country = "INTERNATIONAL";
                    }
                }
            }

            //List<SelectListItem> Modellist = new List<SelectListItem>();
            //Modellist.Add(new SelectListItem { Text = "USA", Value = "USA", Selected = model.Country == "USA" ? true : false });
            ////Modellist.Add(new SelectListItem { Text = "CANADA", Value = "CANADA", Selected = model.Country == "CANADA" ? true : false });
            //Modellist.Add(new SelectListItem { Text = "INTERNATIONAL", Value = "INTERNATIONAL", Selected = model.Country == "INTERNATIONAL" ? true : false });
            //ViewBag.CountryList = Modellist;// new SelectList(Modellist);

            List<SelectListItem> Modellist1 = new List<SelectListItem>();
            Modellist1.Add(new SelectListItem { Text = "Student", Value = "Coperate" });
            Modellist1.Add(new SelectListItem { Text = "Government", Value = "Government" });
            Modellist1.Add(new SelectListItem { Text = "Coperate", Value = "Coperate" });
            model.SubjectList = Modellist1;// new SelectList(Modellist);


            return View(model);
        }

        [HttpPost]
        [ValidateCaptcha]
        [ValidateHoneypot]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> Register(RegisterModel model, string returnUrl, bool captchaValid, IFormCollection form)
        {
            //check whether registration is allowed
            if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled, returnUrl });

            var customer = await _workContext.GetCurrentCustomerAsync();
            if (await _customerService.IsRegisteredAsync(customer))
            {
                //Already registered customer. 
                await _authenticationService.SignOutAsync();

                //raise logged out event       
                await _eventPublisher.PublishAsync(new CustomerLoggedOutEvent(customer));

                //Save a new record
                await _workContext.SetCurrentCustomerAsync(await _customerService.InsertGuestCustomerAsync());
            }

            var store = await _storeContext.GetCurrentStoreAsync();
            customer.RegisteredInStoreId = store.Id;

            //custom customer attributes
            var customerAttributesXml = await ParseCustomCustomerAttributesAsync(form);
            var customerAttributeWarnings = await _customerAttributeParser.GetAttributeWarningsAsync(customerAttributesXml);
            foreach (var error in customerAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnRegistrationPage && !captchaValid)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
            }

            //GDPR
            if (_gdprSettings.GdprEnabled)
            {
                var consents = (await _gdprService
                    .GetAllConsentsAsync()).Where(consent => consent.DisplayDuringRegistration && consent.IsRequired).ToList();

                ValidateRequiredConsents(consents, form);
            }

            if (ModelState.IsValid)
            {
                //code by tp
                customer.Country = form["country"];
                //var roles = _customerService.GetCustomerRolesAsync(customer).Result;
                //foreach (var role in roles)
                //{
                //    if (role.Id != 4)
                //    {
                //        _customerService.RemoveCustomerRoleMappingAsync(customer, role);
                //    }
                //}
                //CustomerCustomerRoleMapping obj = new CustomerCustomerRoleMapping();
                //if (form["country"] == "USA")
                //{
                //    obj.CustomerId = customer.Id;
                //    obj.CustomerRoleId = 42;
                //    _customerService.AddCustomerRoleMappingAsync(obj);
                //}
                //if (form["country"] == "CANADA")
                //{
                //    obj.CustomerId = customer.Id;
                //    obj.CustomerRoleId = 41;
                //    _customerService.AddCustomerRoleMappingAsync(obj);
                //}
                //if (form["country"] == "INTERNATIONAL")
                //{
                //    obj.CustomerId = customer.Id;
                //    obj.CustomerRoleId = 43;
                //    _customerService.AddCustomerRoleMappingAsync(obj);
                //}



                var customerUserName = model.Username?.Trim();
                var customerEmail = model.Email?.Trim();
                customer.IsSynched = true;
                var isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;
                var registrationRequest = new CustomerRegistrationRequest(customer,
                    customerEmail,
                    _customerSettings.UsernamesEnabled ? customerUserName : customerEmail,
                    model.Password,
                    _customerSettings.DefaultPasswordFormat,
                    store.Id,
                    isApproved);
                var registrationResult = await _customerRegistrationService.RegisterCustomerAsync(registrationRequest);
                if (registrationResult.Success)
                {


                    //await _workflowMessageService.DistributorNewCustomer(customer, 1, model.FirstName+" "+model.LastName, model.Email, model.Subject, model.Country, model.Enquiry );
                    await _workflowMessageService.CustomerResponse(customer, 1, model.FirstName + " " + model.LastName);

                    //properties
                    if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                        customer.TimeZoneId = model.TimeZoneId;

                    //VAT number
                    if (_taxSettings.EuVatEnabled)
                    {
                        customer.VatNumber = model.VatNumber;

                        var (vatNumberStatus, _, vatAddress) = await _taxService.GetVatNumberStatusAsync(model.VatNumber);
                        customer.VatNumberStatusId = (int)vatNumberStatus;
                        //send VAT number admin notification
                        // if (!string.IsNullOrEmpty(model.VatNumber) && _taxSettings.EuVatEmailAdminWhenNewVatSubmitted)
                        // await _workflowMessageService.SendNewVatSubmittedStoreOwnerNotificationAsync(customer, model.VatNumber, vatAddress, _localizationSettings.DefaultAdminLanguageId);
                    }

                    //form fields
                    if (_customerSettings.GenderEnabled)
                        customer.Gender = model.Gender;
                    if (_customerSettings.FirstNameEnabled)
                        customer.FirstName = model.FirstName;
                    if (_customerSettings.LastNameEnabled)
                        customer.LastName = model.LastName;
                    if (_customerSettings.DateOfBirthEnabled)
                        customer.DateOfBirth = model.ParseDateOfBirth();
                    if (_customerSettings.CompanyEnabled)
                        customer.Company = model.Company;
                    if (_customerSettings.StreetAddressEnabled)
                        customer.StreetAddress = model.StreetAddress;
                    if (_customerSettings.StreetAddress2Enabled)
                        customer.StreetAddress2 = model.StreetAddress2;
                    if (_customerSettings.ZipPostalCodeEnabled)
                        customer.ZipPostalCode = model.ZipPostalCode;
                    if (_customerSettings.CityEnabled)
                        customer.City = model.City;
                    if (_customerSettings.CountyEnabled)
                        customer.County = model.County;
                    if (_customerSettings.CountryEnabled)
                        customer.CountryId = model.CountryId;
                    if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                        customer.StateProvinceId = model.StateProvinceId;
                    if (_customerSettings.PhoneEnabled)
                        customer.Phone = model.Phone;
                    if (_customerSettings.FaxEnabled)
                        customer.Fax = model.Fax;

                    //save customer attributes
                    customer.CustomCustomerAttributesXML = customerAttributesXml;
                    await _customerService.UpdateCustomerAsync(customer);

                    //newsletter
                    if (_customerSettings.NewsletterEnabled)
                    {
                        var isNewsletterActive = _customerSettings.UserRegistrationType != UserRegistrationType.EmailValidation;

                        //save newsletter value
                        var newsletter = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreIdAsync(customerEmail, store.Id);
                        if (newsletter != null)
                        {
                            if (model.Newsletter)
                            {
                                newsletter.Active = isNewsletterActive;
                                await _newsLetterSubscriptionService.UpdateNewsLetterSubscriptionAsync(newsletter);

                                //GDPR
                                if (_gdprSettings.GdprEnabled && _gdprSettings.LogNewsletterConsent)
                                {
                                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ConsentAgree, await _localizationService.GetResourceAsync("Gdpr.Consent.Newsletter"));
                                }
                            }
                            //else
                            //{
                            //When registering, not checking the newsletter check box should not take an existing email address off of the subscription list.
                            //_newsLetterSubscriptionService.DeleteNewsLetterSubscription(newsletter);
                            //}
                        }
                        else
                        {
                            if (model.Newsletter)
                            {
                                await _newsLetterSubscriptionService.InsertNewsLetterSubscriptionAsync(new NewsLetterSubscription
                                {
                                    NewsLetterSubscriptionGuid = Guid.NewGuid(),
                                    Email = customerEmail,
                                    Active = isNewsletterActive,
                                    StoreId = store.Id,
                                    CreatedOnUtc = DateTime.UtcNow
                                });

                                //GDPR
                                if (_gdprSettings.GdprEnabled && _gdprSettings.LogNewsletterConsent)
                                {
                                    await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ConsentAgree, await _localizationService.GetResourceAsync("Gdpr.Consent.Newsletter"));
                                }
                            }
                        }
                    }

                    if (_customerSettings.AcceptPrivacyPolicyEnabled)
                    {
                        //privacy policy is required
                        //GDPR
                        if (_gdprSettings.GdprEnabled && _gdprSettings.LogPrivacyPolicyConsent)
                        {
                            await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ConsentAgree, await _localizationService.GetResourceAsync("Gdpr.Consent.PrivacyPolicy"));
                        }
                    }

                    //GDPR
                    if (_gdprSettings.GdprEnabled)
                    {
                        var consents = (await _gdprService.GetAllConsentsAsync()).Where(consent => consent.DisplayDuringRegistration).ToList();
                        foreach (var consent in consents)
                        {
                            var controlId = $"consent{consent.Id}";
                            var cbConsent = form[controlId];
                            if (!StringValues.IsNullOrEmpty(cbConsent) && cbConsent.ToString().Equals("on"))
                            {
                                //agree
                                await _gdprService.InsertLogAsync(customer, consent.Id, GdprRequestType.ConsentAgree, consent.Message);
                            }
                            else
                            {
                                //disagree
                                await _gdprService.InsertLogAsync(customer, consent.Id, GdprRequestType.ConsentDisagree, consent.Message);
                            }
                        }
                    }

                    //insert default address (if possible)
                    var defaultAddress = new Address
                    {
                        FirstName = customer.FirstName,
                        LastName = customer.LastName,
                        Email = customer.Email,
                        Company = customer.Company,
                        CountryId = customer.CountryId > 0
                            ? (int?)customer.CountryId
                            : null,
                        StateProvinceId = customer.StateProvinceId > 0
                            ? (int?)customer.StateProvinceId
                            : null,
                        County = customer.County,
                        City = customer.City,
                        Address1 = customer.StreetAddress,
                        Address2 = customer.StreetAddress2,
                        ZipPostalCode = customer.ZipPostalCode,
                        PhoneNumber = customer.Phone,
                        FaxNumber = customer.Fax,
                        CreatedOnUtc = customer.CreatedOnUtc
                    };
                    if (await _addressService.IsAddressValidAsync(defaultAddress))
                    {
                        //some validation
                        if (defaultAddress.CountryId == 0)
                            defaultAddress.CountryId = null;
                        if (defaultAddress.StateProvinceId == 0)
                            defaultAddress.StateProvinceId = null;
                        //set default address
                        //customer.Addresses.Add(defaultAddress);

                        await _addressService.InsertAddressAsync(defaultAddress);

                        await _customerService.InsertCustomerAddressAsync(customer, defaultAddress);

                        customer.BillingAddressId = defaultAddress.Id;
                        customer.ShippingAddressId = defaultAddress.Id;

                        await _customerService.UpdateCustomerAsync(customer);
                    }

                    //notifications
                    if (_customerSettings.NotifyNewCustomerRegistration)
                        await _workflowMessageService.SendCustomerRegisteredStoreOwnerNotificationMessageAsync(customer,
                            _localizationSettings.DefaultAdminLanguageId);

                    //raise event       
                    await _eventPublisher.PublishAsync(new CustomerRegisteredEvent(customer));
                    var currentLanguage = await _workContext.GetWorkingLanguageAsync();

                    switch (_customerSettings.UserRegistrationType)
                    {
                        case UserRegistrationType.EmailValidation:
                            //email validation message
                            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AccountActivationTokenAttribute, Guid.NewGuid().ToString());
                            // await _workflowMessageService.SendCustomerEmailValidationMessageAsync(customer, currentLanguage.Id);

                            //result
                            return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.EmailValidation, returnUrl });

                        case UserRegistrationType.AdminApproval:
                            return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval, returnUrl });

                        case UserRegistrationType.Standard:
                            //send customer welcome message
                            // await _workflowMessageService.SendCustomerWelcomeMessageAsync(customer, currentLanguage.Id);

                            //raise event       
                            await _eventPublisher.PublishAsync(new CustomerActivatedEvent(customer));

                            returnUrl = Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.Standard, returnUrl });
                            return await _customerRegistrationService.SignInCustomerAsync(customer, returnUrl, true);

                        default:
                            return RedirectToRoute("Homepage");
                    }
                }

                //errors
                foreach (var error in registrationResult.Errors)
                    ModelState.AddModelError("", error);
            }

            //If we got this far, something failed, redisplay form
            model = await _customerModelFactory.PrepareRegisterModelAsync(model, true, customerAttributesXml);

            return View(model);
        }

        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> RegisterResult(int resultId, string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("Homepage");

            var model = await _customerModelFactory.PrepareRegisterResultModelAsync(resultId, returnUrl);
            return View(model);
        }

        [HttpPost]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> CheckUsernameAvailability(string username)
        {
            var usernameAvailable = false;
            var statusText = await _localizationService.GetResourceAsync("Account.CheckUsernameAvailability.NotAvailable");

            if (!UsernamePropertyValidator<string, string>.IsValid(username, _customerSettings))
            {
                statusText = await _localizationService.GetResourceAsync("Account.Fields.Username.NotValid");
            }
            else if (_customerSettings.UsernamesEnabled && !string.IsNullOrWhiteSpace(username))
            {
                var currentCustomer = await _workContext.GetCurrentCustomerAsync();
                if (currentCustomer != null &&
                    currentCustomer.Username != null &&
                    currentCustomer.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase))
                {
                    statusText = await _localizationService.GetResourceAsync("Account.CheckUsernameAvailability.CurrentUsername");
                }
                else
                {
                    var customer = await _customerService.GetCustomerByUsernameAsync(username);
                    if (customer == null)
                    {
                        statusText = await _localizationService.GetResourceAsync("Account.CheckUsernameAvailability.Available");
                        usernameAvailable = true;
                    }
                }
            }

            return Json(new { Available = usernameAvailable, Text = statusText });
        }

        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> AccountActivation(string token, string email, Guid guid)
        {
            //For backward compatibility with previous versions where email was used as a parameter in the URL
            var customer = await _customerService.GetCustomerByEmailAsync(email)
                ?? await _customerService.GetCustomerByGuidAsync(guid);

            if (customer == null)
                return RedirectToRoute("Homepage");

            var model = new AccountActivationModel { ReturnUrl = Url.RouteUrl("Homepage") };
            var cToken = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.AccountActivationTokenAttribute);
            if (string.IsNullOrEmpty(cToken))
            {
                model.Result = await _localizationService.GetResourceAsync("Account.AccountActivation.AlreadyActivated");
                return View(model);
            }

            if (!cToken.Equals(token, StringComparison.InvariantCultureIgnoreCase))
                return RedirectToRoute("Homepage");

            //activate user account
            customer.Active = true;
            await _customerService.UpdateCustomerAsync(customer);
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AccountActivationTokenAttribute, "");

            //send welcome message
            await _workflowMessageService.SendCustomerWelcomeMessageAsync(customer, (await _workContext.GetWorkingLanguageAsync()).Id);

            //raise event       
            await _eventPublisher.PublishAsync(new CustomerActivatedEvent(customer));

            //authenticate customer after activation
            await _customerRegistrationService.SignInCustomerAsync(customer, null, true);

            //activating newsletter if need
            var store = await _storeContext.GetCurrentStoreAsync();
            var newsletter = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreIdAsync(customer.Email, store.Id);
            if (newsletter != null && !newsletter.Active)
            {
                newsletter.Active = true;
                await _newsLetterSubscriptionService.UpdateNewsLetterSubscriptionAsync(newsletter);
            }

            model.Result = await _localizationService.GetResourceAsync("Account.AccountActivation.Activated");
            return View(model);
        }

        #endregion

        #region My account / Info

        public virtual async Task<IActionResult> Info()
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            var model = new CustomerInfoModel();
            model = _customerModelFactory.PrepareCustomerInfoModelAsync(model, _workContext.GetCurrentCustomerAsync().Result, false).Result;
            //code by tp
            model.Country = _workContext.GetCurrentCustomerAsync().Result.Country;
            //   if (string.IsNullOrEmpty(model.Country))
            {
                var CustomerDetails = _workContext.GetCurrentCustomerAsync().Result;

                var rolename = _customerService.GetCustomerRolesAsync(CustomerDetails).Result;//.Where(d => d.Id == 41 || d.Id == 42 || d.Id == 43);
                //if (rolename.Any(d => d.Id == 4))
                {
                    if (rolename.Any(d => d.Name.ToLower().Contains("usd") && d.Id != 43))
                    {
                        model.Country = "USA";
                    }
                    if (rolename.Any(d => d.Name.ToLower().Contains("cad")))
                    {
                        model.Country = "CANADA";
                    }
                    if (rolename.Any(d => d.Name.ToLower().Contains("intl")))
                    {
                        model.Country = "INTERNATIONAL";
                    }
                }
            }

            List<SelectListItem> Modellist = new List<SelectListItem>();
            Modellist.Add(new SelectListItem { Text = "USA", Value = "USA", Selected = model.Country == "USA" ? true : false });
            //Modellist.Add(new SelectListItem { Text = "CANADA", Value = "CANADA", Selected = model.Country == "CANADA" ? true : false });
            Modellist.Add(new SelectListItem { Text = "INTERNATIONAL", Value = "INTERNATIONAL", Selected = model.Country == "INTERNATIONAL" ? true : false });
            ViewBag.CountryList = Modellist;// new SelectList(Modellist);
            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Info(CustomerInfoModel model, IFormCollection form)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            var oldCustomerModel = new CustomerInfoModel();

            //get customer info model before changes for gdpr log
            if (_gdprSettings.GdprEnabled & _gdprSettings.LogUserProfileChanges)
                oldCustomerModel = await _customerModelFactory.PrepareCustomerInfoModelAsync(oldCustomerModel, customer, false);

            //custom customer attributes
            var customerAttributesXml = await ParseCustomCustomerAttributesAsync(form);
            var customerAttributeWarnings = await _customerAttributeParser.GetAttributeWarningsAsync(customerAttributesXml);
            foreach (var error in customerAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            //GDPR
            if (_gdprSettings.GdprEnabled)
            {
                var consents = (await _gdprService
                    .GetAllConsentsAsync()).Where(consent => consent.DisplayOnCustomerInfoPage && consent.IsRequired).ToList();

                ValidateRequiredConsents(consents, form);
            }

            try
            {
                if (ModelState.IsValid)
                {
                    customer.Country = form["country"];
                    _customerService.UpdateCustomerAsync(customer);
                    var roles = _customerService.GetCustomerRolesAsync(customer).Result;
                    var order = _orderService.SearchOrdersAsync(0, 0, customer.Id).Result;
                    if (order.Count == 0)
                    {
                        foreach (var role in roles)
                        {

                            if (role.Id == 41 || role.Id == 42 || role.Id == 43)
                            {
                                _customerService.RemoveCustomerRoleMappingAsync(customer, role);
                            }
                        }
                        CustomerCustomerRoleMapping obj = new CustomerCustomerRoleMapping();
                        if (form["country"] == "USA")
                        {
                            obj.CustomerId = customer.Id;
                            obj.CustomerRoleId = 42;
                            _customerService.AddCustomerRoleMappingAsync(obj);
                        }
                        if (form["country"] == "CANADA")
                        {
                            obj.CustomerId = customer.Id;
                            obj.CustomerRoleId = 41;
                            _customerService.AddCustomerRoleMappingAsync(obj);
                        }
                        if (form["country"] == "INTERNATIONAL")
                        {
                            obj.CustomerId = customer.Id;
                            obj.CustomerRoleId = 43;
                            _customerService.AddCustomerRoleMappingAsync(obj);
                        }

                    }
                    //username 
                    if (_customerSettings.UsernamesEnabled && _customerSettings.AllowUsersToChangeUsernames)
                    {
                        var userName = model.Username.Trim();
                        if (!customer.Username.Equals(userName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            //change username
                            await _customerRegistrationService.SetUsernameAsync(customer, userName);

                            //re-authenticate
                            //do not authenticate users in impersonation mode
                            if (_workContext.OriginalCustomerIfImpersonated == null)
                                await _authenticationService.SignInAsync(customer, true);
                        }
                    }
                    //email
                    var email = model.Email.Trim();
                    if (!customer.Email.Equals(email, StringComparison.InvariantCultureIgnoreCase))
                    {
                        //change email
                        var requireValidation = _customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation;
                        await _customerRegistrationService.SetEmailAsync(customer, email, requireValidation);

                        //do not authenticate users in impersonation mode
                        if (_workContext.OriginalCustomerIfImpersonated == null)
                        {
                            //re-authenticate (if usernames are disabled)
                            if (!_customerSettings.UsernamesEnabled && !requireValidation)
                                await _authenticationService.SignInAsync(customer, true);
                        }
                    }

                    //properties
                    if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                        customer.TimeZoneId = model.TimeZoneId;
                    //VAT number
                    if (_taxSettings.EuVatEnabled)
                    {
                        var prevVatNumber = customer.VatNumber;
                        customer.VatNumber = model.VatNumber;

                        if (prevVatNumber != model.VatNumber)
                        {
                            var (vatNumberStatus, _, vatAddress) = await _taxService.GetVatNumberStatusAsync(model.VatNumber);
                            customer.VatNumberStatusId = (int)vatNumberStatus;

                            //send VAT number admin notification
                            if (!string.IsNullOrEmpty(model.VatNumber) && _taxSettings.EuVatEmailAdminWhenNewVatSubmitted)
                                await _workflowMessageService.SendNewVatSubmittedStoreOwnerNotificationAsync(customer,
                                    model.VatNumber, vatAddress, _localizationSettings.DefaultAdminLanguageId);
                        }
                    }

                    //form fields
                    if (_customerSettings.GenderEnabled)
                        customer.Gender = model.Gender;
                    if (_customerSettings.FirstNameEnabled)
                        customer.FirstName = model.FirstName;
                    if (_customerSettings.LastNameEnabled)
                        customer.LastName = model.LastName;
                    if (_customerSettings.DateOfBirthEnabled)
                        customer.DateOfBirth = model.ParseDateOfBirth();
                    if (_customerSettings.CompanyEnabled)
                        customer.Company = model.Company;
                    if (_customerSettings.StreetAddressEnabled)
                        customer.StreetAddress = model.StreetAddress;
                    if (_customerSettings.StreetAddress2Enabled)
                        customer.StreetAddress2 = model.StreetAddress2;
                    if (_customerSettings.ZipPostalCodeEnabled)
                        customer.ZipPostalCode = model.ZipPostalCode;
                    if (_customerSettings.CityEnabled)
                        customer.City = model.City;
                    if (_customerSettings.CountyEnabled)
                        customer.County = model.County;
                    if (_customerSettings.CountryEnabled)
                        customer.CountryId = model.CountryId;
                    if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                        customer.StateProvinceId = model.StateProvinceId;
                    if (_customerSettings.PhoneEnabled)
                        customer.Phone = model.Phone;
                    if (_customerSettings.FaxEnabled)
                        customer.Fax = model.Fax;

                    customer.CustomCustomerAttributesXML = customerAttributesXml;
                    await _customerService.UpdateCustomerAsync(customer);

                    //newsletter
                    if (_customerSettings.NewsletterEnabled)
                    {
                        //save newsletter value
                        var store = await _storeContext.GetCurrentStoreAsync();
                        var newsletter = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreIdAsync(customer.Email, store.Id);
                        if (newsletter != null)
                        {
                            if (model.Newsletter)
                            {
                                newsletter.Active = true;
                                await _newsLetterSubscriptionService.UpdateNewsLetterSubscriptionAsync(newsletter);
                            }
                            else
                            {
                                await _newsLetterSubscriptionService.DeleteNewsLetterSubscriptionAsync(newsletter);
                            }
                        }
                        else
                        {
                            if (model.Newsletter)
                            {
                                await _newsLetterSubscriptionService.InsertNewsLetterSubscriptionAsync(new NewsLetterSubscription
                                {
                                    NewsLetterSubscriptionGuid = Guid.NewGuid(),
                                    Email = customer.Email,
                                    Active = true,
                                    StoreId = store.Id,
                                    CreatedOnUtc = DateTime.UtcNow
                                });
                            }
                        }
                    }

                    if (_forumSettings.ForumsEnabled && _forumSettings.SignaturesEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SignatureAttribute, model.Signature);

                    //GDPR
                    if (_gdprSettings.GdprEnabled)
                        await LogGdprAsync(customer, oldCustomerModel, model, form);

                    _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Account.CustomerInfo.Updated"));

                    return RedirectToRoute("CustomerInfo");
                }
            }
            catch (Exception exc)
            {
                ModelState.AddModelError("", exc.Message);
            }

            //If we got this far, something failed, redisplay form
            model = await _customerModelFactory.PrepareCustomerInfoModelAsync(model, customer, true, customerAttributesXml);

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> RemoveExternalAssociation(int id)
        {
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            //ensure it's our record
            var ear = await _externalAuthenticationService.GetExternalAuthenticationRecordByIdAsync(id);

            if (ear == null)
            {
                return Json(new
                {
                    redirect = Url.Action("Info"),
                });
            }

            await _externalAuthenticationService.DeleteExternalAuthenticationRecordAsync(ear);

            return Json(new
            {
                redirect = Url.Action("Info"),
            });
        }

        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> EmailRevalidation(string token, string email, Guid guid)
        {
            //For backward compatibility with previous versions where email was used as a parameter in the URL
            var customer = await _customerService.GetCustomerByEmailAsync(email)
                ?? await _customerService.GetCustomerByGuidAsync(guid);

            if (customer == null)
                return RedirectToRoute("Homepage");

            var model = new EmailRevalidationModel { ReturnUrl = Url.RouteUrl("Homepage") };
            var cToken = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.EmailRevalidationTokenAttribute);
            if (string.IsNullOrEmpty(cToken))
            {
                model.Result = await _localizationService.GetResourceAsync("Account.EmailRevalidation.AlreadyChanged");
                return View(model);
            }

            if (!cToken.Equals(token, StringComparison.InvariantCultureIgnoreCase))
                return RedirectToRoute("Homepage");

            if (string.IsNullOrEmpty(customer.EmailToRevalidate))
                return RedirectToRoute("Homepage");

            if (_customerSettings.UserRegistrationType != UserRegistrationType.EmailValidation)
                return RedirectToRoute("Homepage");

            //change email
            try
            {
                await _customerRegistrationService.SetEmailAsync(customer, customer.EmailToRevalidate, false);
            }
            catch (Exception exc)
            {
                model.Result = await _localizationService.GetResourceAsync(exc.Message);
                return View(model);
            }

            customer.EmailToRevalidate = null;
            await _customerService.UpdateCustomerAsync(customer);
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.EmailRevalidationTokenAttribute, "");

            //authenticate customer after changing email
            await _customerRegistrationService.SignInCustomerAsync(customer, null, true);

            model.Result = await _localizationService.GetResourceAsync("Account.EmailRevalidation.Changed");
            return View(model);
        }

        #endregion

        #region My account / Addresses

        public virtual async Task<IActionResult> Addresses()
        {
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            var model = await _customerModelFactory.PrepareCustomerAddressListModelAsync();

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddressDelete(int addressId)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            //find address (ensure that it belongs to the current customer)
            var address = await _customerService.GetCustomerAddressAsync(customer.Id, addressId);
            if (address != null)
            {
                await _customerService.RemoveCustomerAddressAsync(customer, address);
                await _customerService.UpdateCustomerAsync(customer);
                //now delete the address record
                await _addressService.DeleteAddressAsync(address);
            }

            //redirect to the address list page
            return Json(new
            {
                redirect = Url.RouteUrl("CustomerAddresses"),
            });
        }

        public virtual async Task<IActionResult> AddressAdd()
        {
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            var model = new CustomerAddressEditModel();
            await _addressModelFactory.PrepareAddressModelAsync(model.Address,
                address: null,
                excludeProperties: false,
                addressSettings: _addressSettings,
                loadCountries: async () => await _countryService.GetAllCountriesAsync((await _workContext.GetWorkingLanguageAsync()).Id));

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddressAdd(CustomerAddressEditModel model, IFormCollection form)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            //custom address attributes
            var customAttributes = await _addressAttributeParser.ParseCustomAddressAttributesAsync(form);
            var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarningsAsync(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            if (ModelState.IsValid)
            {
                var address = model.Address.ToEntity();
                address.CustomAttributes = customAttributes;
                address.CreatedOnUtc = DateTime.UtcNow;
                //some validation
                if (address.CountryId == 0)
                    address.CountryId = null;
                if (address.StateProvinceId == 0)
                    address.StateProvinceId = null;


                await _addressService.InsertAddressAsync(address);

                await _customerService.InsertCustomerAddressAsync(customer, address);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Account.CustomerAddresses.Added"));

                return RedirectToRoute("CustomerAddresses");
            }

            //If we got this far, something failed, redisplay form
            await _addressModelFactory.PrepareAddressModelAsync(model.Address,
                address: null,
                excludeProperties: true,
                addressSettings: _addressSettings,
                loadCountries: async () => await _countryService.GetAllCountriesAsync((await _workContext.GetWorkingLanguageAsync()).Id),
                overrideAttributesXml: customAttributes);

            return View(model);
        }

        public virtual async Task<IActionResult> AddressEdit(int addressId)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            //find address (ensure that it belongs to the current customer)
            var address = await _customerService.GetCustomerAddressAsync(customer.Id, addressId);
            if (address == null)
                //address is not found
                return RedirectToRoute("CustomerAddresses");

            var model = new CustomerAddressEditModel();
            await _addressModelFactory.PrepareAddressModelAsync(model.Address,
                address: address,
                excludeProperties: false,
                addressSettings: _addressSettings,
                loadCountries: async () => await _countryService.GetAllCountriesAsync((await _workContext.GetWorkingLanguageAsync()).Id));

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddressEdit(CustomerAddressEditModel model, IFormCollection form)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            //find address (ensure that it belongs to the current customer)
            var address = await _customerService.GetCustomerAddressAsync(customer.Id, model.Address.Id);
            if (address == null)
                //address is not found
                return RedirectToRoute("CustomerAddresses");

            //custom address attributes
            var customAttributes = await _addressAttributeParser.ParseCustomAddressAttributesAsync(form);
            var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarningsAsync(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            if (ModelState.IsValid)
            {
                address = model.Address.ToEntity(address);
                address.CustomAttributes = customAttributes;
                await _addressService.UpdateAddressAsync(address);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Account.CustomerAddresses.Updated"));

                return RedirectToRoute("CustomerAddresses");
            }

            //If we got this far, something failed, redisplay form
            await _addressModelFactory.PrepareAddressModelAsync(model.Address,
                address: address,
                excludeProperties: true,
                addressSettings: _addressSettings,
                loadCountries: async () => await _countryService.GetAllCountriesAsync((await _workContext.GetWorkingLanguageAsync()).Id),
                overrideAttributesXml: customAttributes);

            return View(model);
        }

        #endregion

        #region My account / Downloadable products

        public virtual async Task<IActionResult> DownloadableProducts()
        {
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            if (_customerSettings.HideDownloadableProductsTab)
                return RedirectToRoute("CustomerInfo");

            var model = await _customerModelFactory.PrepareCustomerDownloadableProductsModelAsync();

            return View(model);
        }

        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> UserAgreement(Guid orderItemId)
        {
            var orderItem = await _orderService.GetOrderItemByGuidAsync(orderItemId);
            if (orderItem == null)
                return RedirectToRoute("Homepage");

            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

            if (product == null || !product.HasUserAgreement)
                return RedirectToRoute("Homepage");

            var model = await _customerModelFactory.PrepareUserAgreementModelAsync(orderItem, product);

            return View(model);
        }

        #endregion

        #region My account / Change password

        public virtual async Task<IActionResult> ChangePassword()
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            var model = await _customerModelFactory.PrepareChangePasswordModelAsync();

            //display the cause of the change password 
            if (await _customerService.IsPasswordExpiredAsync(customer))
                ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Account.ChangePassword.PasswordIsExpired"));

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> ChangePassword(ChangePasswordModel model, string returnUrl)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            if (ModelState.IsValid)
            {
                var changePasswordRequest = new ChangePasswordRequest(customer.Email,
                    true, _customerSettings.DefaultPasswordFormat, model.NewPassword, model.OldPassword);
                var changePasswordResult = await _customerRegistrationService.ChangePasswordAsync(changePasswordRequest);
                if (changePasswordResult.Success)
                {
                    _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Account.ChangePassword.Success"));

                    //authenticate customer after changing password
                    await _customerRegistrationService.SignInCustomerAsync(customer, null, true);

                    if (string.IsNullOrEmpty(returnUrl))
                        return View(model);

                    //prevent open redirection attack
                    if (!Url.IsLocalUrl(returnUrl))
                        returnUrl = Url.RouteUrl("Homepage");

                    return new RedirectResult(returnUrl);
                }

                //errors
                foreach (var error in changePasswordResult.Errors)
                    ModelState.AddModelError("", error);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        #endregion

        #region My account / Avatar

        public virtual async Task<IActionResult> Avatar()
        {
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            if (!_customerSettings.AllowCustomersToUploadAvatars)
                return RedirectToRoute("CustomerInfo");

            var model = new CustomerAvatarModel();
            model = await _customerModelFactory.PrepareCustomerAvatarModelAsync(model);

            return View(model);
        }

        [HttpPost, ActionName("Avatar")]
        [FormValueRequired("upload-avatar")]
        public virtual async Task<IActionResult> UploadAvatar(CustomerAvatarModel model, IFormFile uploadedFile)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            if (!_customerSettings.AllowCustomersToUploadAvatars)
                return RedirectToRoute("CustomerInfo");

            var contentType = uploadedFile?.ContentType.ToLowerInvariant();

            if (contentType != null && !contentType.Equals("image/jpeg") && !contentType.Equals("image/gif"))
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Account.Avatar.UploadRules"));

            if (ModelState.IsValid)
            {
                try
                {
                    var customerAvatar = await _pictureService.GetPictureByIdAsync(await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.AvatarPictureIdAttribute));
                    if (uploadedFile != null && !string.IsNullOrEmpty(uploadedFile.FileName))
                    {
                        var avatarMaxSize = _customerSettings.AvatarMaximumSizeBytes;
                        if (uploadedFile.Length > avatarMaxSize)
                            throw new NopException(string.Format(await _localizationService.GetResourceAsync("Account.Avatar.MaximumUploadedFileSize"), avatarMaxSize));

                        var customerPictureBinary = await _downloadService.GetDownloadBitsAsync(uploadedFile);
                        if (customerAvatar != null)
                            customerAvatar = await _pictureService.UpdatePictureAsync(customerAvatar.Id, customerPictureBinary, contentType, null);
                        else
                            customerAvatar = await _pictureService.InsertPictureAsync(customerPictureBinary, contentType, null);
                    }

                    var customerAvatarId = 0;
                    if (customerAvatar != null)
                        customerAvatarId = customerAvatar.Id;

                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AvatarPictureIdAttribute, customerAvatarId);

                    model.AvatarUrl = await _pictureService.GetPictureUrlAsync(
                        await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.AvatarPictureIdAttribute),
                        _mediaSettings.AvatarPictureSize,
                        false);

                    return View(model);
                }
                catch (Exception exc)
                {
                    ModelState.AddModelError("", exc.Message);
                }
            }

            //If we got this far, something failed, redisplay form
            model = await _customerModelFactory.PrepareCustomerAvatarModelAsync(model);
            return View(model);
        }

        [HttpPost, ActionName("Avatar")]
        [FormValueRequired("remove-avatar")]
        public virtual async Task<IActionResult> RemoveAvatar(CustomerAvatarModel model)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            if (!_customerSettings.AllowCustomersToUploadAvatars)
                return RedirectToRoute("CustomerInfo");

            var customerAvatar = await _pictureService.GetPictureByIdAsync(await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.AvatarPictureIdAttribute));
            if (customerAvatar != null)
                await _pictureService.DeletePictureAsync(customerAvatar);
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AvatarPictureIdAttribute, 0);

            return RedirectToRoute("CustomerAvatar");
        }

        #endregion

        #region GDPR tools

        public virtual async Task<IActionResult> GdprTools()
        {
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            if (!_gdprSettings.GdprEnabled)
                return RedirectToRoute("CustomerInfo");

            var model = await _customerModelFactory.PrepareGdprToolsModelAsync();

            return View(model);
        }

        [HttpPost, ActionName("GdprTools")]
        [FormValueRequired("export-data")]
        public virtual async Task<IActionResult> GdprToolsExport()
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            if (!_gdprSettings.GdprEnabled)
                return RedirectToRoute("CustomerInfo");

            //log
            await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ExportData, await _localizationService.GetResourceAsync("Gdpr.Exported"));

            var store = await _storeContext.GetCurrentStoreAsync();

            //export
            var bytes = await _exportManager.ExportCustomerGdprInfoToXlsxAsync(customer, store.Id);

            return File(bytes, MimeTypes.TextXlsx, "customerdata.xlsx");
        }

        [HttpPost, ActionName("GdprTools")]
        [FormValueRequired("delete-account")]
        public virtual async Task<IActionResult> GdprToolsDelete()
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            if (!_gdprSettings.GdprEnabled)
                return RedirectToRoute("CustomerInfo");

            //log
            await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.DeleteCustomer, await _localizationService.GetResourceAsync("Gdpr.DeleteRequested"));

            var model = await _customerModelFactory.PrepareGdprToolsModelAsync();
            model.Result = await _localizationService.GetResourceAsync("Gdpr.DeleteRequested.Success");

            return View(model);
        }

        #endregion

        #region Check gift card balance

        //check gift card balance page
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        public virtual async Task<IActionResult> CheckGiftCardBalance()
        {
            if (!(_captchaSettings.Enabled && _customerSettings.AllowCustomersToCheckGiftCardBalance))
            {
                return RedirectToRoute("CustomerInfo");
            }

            var model = await _customerModelFactory.PrepareCheckGiftCardBalanceModelAsync();

            return View(model);
        }

        [HttpPost, ActionName("CheckGiftCardBalance")]
        [FormValueRequired("checkbalancegiftcard")]
        [ValidateCaptcha]
        public virtual async Task<IActionResult> CheckBalance(CheckGiftCardBalanceModel model, bool captchaValid)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && !captchaValid)
            {
                ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
            }

            if (ModelState.IsValid)
            {
                var giftCard = (await _giftCardService.GetAllGiftCardsAsync(giftCardCouponCode: model.GiftCardCode)).FirstOrDefault();
                if (giftCard != null && await _giftCardService.IsGiftCardValidAsync(giftCard))
                {
                    var remainingAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(await _giftCardService.GetGiftCardRemainingAmountAsync(giftCard), await _workContext.GetWorkingCurrencyAsync());
                    model.Result = await _priceFormatter.FormatPriceAsync(remainingAmount, true, false);
                }
                else
                {
                    model.Message = await _localizationService.GetResourceAsync("CheckGiftCardBalance.GiftCardCouponCode.Invalid");
                }
            }

            return View(model);
        }

        #endregion

        #region Multi-factor Authentication

        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        public virtual async Task<IActionResult> MultiFactorAuthentication()
        {
            if (!await _multiFactorAuthenticationPluginManager.HasActivePluginsAsync())
            {
                return RedirectToRoute("CustomerInfo");
            }

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableMultiFactorAuthentication))
                return RedirectToRoute("CustomerInfo");

            var model = new MultiFactorAuthenticationModel();
            model = await _customerModelFactory.PrepareMultiFactorAuthenticationModelAsync(model);
            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> MultiFactorAuthentication(MultiFactorAuthenticationModel model, IFormCollection form)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableMultiFactorAuthentication))
                return RedirectToRoute("CustomerInfo");

            try
            {
                if (ModelState.IsValid)
                {
                    //save MultiFactorIsEnabledAttribute
                    if (!model.IsEnabled)
                    {
                        if (!_multiFactorAuthenticationSettings.ForceMultifactorAuthentication)
                        {
                            await _genericAttributeService
                                .SaveAttributeAsync(customer, NopCustomerDefaults.SelectedMultiFactorAuthenticationProviderAttribute, string.Empty);

                            //raise change multi-factor authentication provider event       
                            await _eventPublisher.PublishAsync(new CustomerChangeMultiFactorAuthenticationProviderEvent(customer));
                        }
                        else
                        {
                            model = await _customerModelFactory.PrepareMultiFactorAuthenticationModelAsync(model);
                            model.Message = await _localizationService.GetResourceAsync("Account.MultiFactorAuthentication.Warning.ForceActivation");
                            return View(model);
                        }
                    }
                    else
                    {
                        //save selected multi-factor authentication provider
                        var selectedProvider = await ParseSelectedProviderAsync(form);
                        var lastSavedProvider = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.SelectedMultiFactorAuthenticationProviderAttribute);
                        if (string.IsNullOrEmpty(selectedProvider) && !string.IsNullOrEmpty(lastSavedProvider))
                        {
                            selectedProvider = lastSavedProvider;
                        }

                        if (selectedProvider != lastSavedProvider)
                        {
                            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedMultiFactorAuthenticationProviderAttribute, selectedProvider);

                            //raise change multi-factor authentication provider event       
                            await _eventPublisher.PublishAsync(new CustomerChangeMultiFactorAuthenticationProviderEvent(customer));
                        }
                    }

                    return RedirectToRoute("MultiFactorAuthenticationSettings");
                }
            }
            catch (Exception exc)
            {
                ModelState.AddModelError("", exc.Message);
            }

            //If we got this far, something failed, redisplay form
            model = await _customerModelFactory.PrepareMultiFactorAuthenticationModelAsync(model);
            return View(model);
        }

        public virtual async Task<IActionResult> ConfigureMultiFactorAuthenticationProvider(string providerSysName)
        {
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableMultiFactorAuthentication))
                return RedirectToRoute("CustomerInfo");

            var model = new MultiFactorAuthenticationProviderModel();
            model = await _customerModelFactory.PrepareMultiFactorAuthenticationProviderModelAsync(model, providerSysName);

            return View(model);
        }

        #endregion
        // UI/UX by Riz
        #region Customer Dashboard
        [HttpsRequirement]
        public virtual IActionResult Dashboard()
        {
            if (!_customerService.IsRegisteredAsync(_workContext.GetCurrentCustomerAsync().Result).Result)
                return Challenge();

            var model = _customerModelFactory.PrepareCustomerNavigationModelAsync(0).Result;
            model.Username = _workContext.GetCurrentCustomerAsync().Result.Username;
            model.SelectedTab = 0;
            return View(model);


        }
        #endregion
        [IgnoreAntiforgeryToken]
        public string customerrole()
        {
            var data = "";
            var currcust = _workContext.GetCurrentCustomerAsync().Result;
            var roleassign = _customerService.GetCustomerRolesAsync(currcust).Result;

            //var aa =roleassign.Where(x => x.Name != "Guests").Select(x => x.Name);
            //data = aa.FirstOrDefault();

            foreach (var i in roleassign)
            {
                if (i.Name != "Guests")
                {
                    data = i.Name;
                }
            }

            return data;
        }
        //public virtual IActionResult CustomerInvoice()
        //{
        //    var datasettings = DataSettingsManager.LoadSettings();
        //    var connstring = datasettings.ConnectionString;
        //    var dt = new DataTable();
        //    var conn = new SqlConnection(connstring);
        //    SqlDataAdapter da = new SqlDataAdapter();
        //    conn.Open();

        //    var currcust = _workContext.GetCurrentCustomerAsync().Result.BPCode;

        //    var Customercmd = new SqlCommand("select sii.Description, si.DocumentNumber,si.DocStatus,si.CustomerNo,si.CompanyName,si.Currency,si.Paid,si.Balance from SapInvoices si join PaymentInvoice as pa on pa.CustomerNumber = si.CustomerNo join PaymentInvoiceDetail pd on pd.PaymentInvoiceId = pa.id join SapInvoicesItems sii on sii.DocEntry=si.DocEntry  where si.CustomerNo = '" + currcust + "'group by sii.Description,si.CreateDate,si.DocumentNumber,si.DocStatus,si.CustomerNo,si.CompanyName,si.Currency,si.Paid,si.Balance", conn);
        //    Customercmd.CommandType = CommandType.Text;
        //    Customercmd.CommandTimeout = 0;


        //    da = new SqlDataAdapter(Customercmd);
        //    dt = new DataTable();
        //    da.Fill(dt);
        //    var model = dt.Rows;
        //    return View(model);
        //}


        //[HttpsRequirement]
        public virtual IActionResult OfflineTransactions()
        {
            if (!_customerService.IsRegisteredAsync(_workContext.GetCurrentCustomerAsync().Result).Result)
                return Challenge();

            OfflineTransactionModel model = new OfflineTransactionModel();
            IList<InvoiceModel> invList = model.Invoices;
            IList<InvoiceModel> invList1 = model.Invoices;
            var Bpcode = "0";
            var customer = _workContext.GetCurrentCustomerAsync().Result;
            ViewBag.customerBpcode = _workContext.GetCurrentCustomerAsync().Result.BPCode;
            if (customer.BPCode != null)
            {
                Bpcode = customer.BPCode;
            }
            try
            {
                var dt = GetAllTransaction(customer.Email, Bpcode);
                if (dt.Rows.Count > 0)
                {
                    DataRow dr = dt.Rows[0];
                    model.TotalOutstandingPayment = Convert.ToDouble(dr["TotalOutstandingPayment"]);
                    model.OutstandingInvoices = Convert.ToDouble(dr["OutstandingInvoices"]);
                    model.AccountCredits = Convert.ToDouble(dr["AccountCredits"]);
                }
                else
                {
                    model.TotalOutstandingPayment = 0;
                    model.OutstandingInvoices = 0;
                    model.AccountCredits = 0;
                }
                //var currentYear = DateTime.UtcNow.Year.ToString(); // Added by shubham 22-12-2023
                var dtInvoices = GetAllTransactionInvoices(Bpcode, customer.Email, "All", "All");
                var dtInvoice1 = GetAllTransactioncreditmemo(Bpcode, customer.Email, "All", "All");

                if (dtInvoices.Rows.Count > 0)
                {
                    foreach (DataRow r in dtInvoices.Rows)
                    {
                        //invList.Add(new InvoiceModel
                        //{
                        //    Invoice = Convert.ToString(r["Invoice"]),
                        //    InvoiceStatus = Convert.ToString(r["InvoiceStatus"]),
                        //    InvoiceDate = Convert.ToDateTime(r["InvoiceDate"]).ToString("dd/MM/yyyy"),
                        //    InvoiceTotal = Convert.ToDouble(r["InvoiceTotal"]).ToString("C"),
                        //    InvoiceType = Convert.ToString(r["InvoiceType"]),
                        //    Balance = Convert.ToDouble(r["Balance"]).ToString("C"),
                        //    ButtonText = Convert.ToString(r["ButtonText"]).ToString(),
                        //    EmailAddress = r["APEmail"].ToString(),
                        //    OrderId = r["OrderId"].ToString(),
                        //    CustomerName= _customerService.GetCustomerByIdAsync(_orderService.GetOrderByIdAsync(75045).Result..Id).Result.FirstName
                        //});
                        InvoiceModel invoice = new InvoiceModel();
                        invoice.Invoice = Convert.ToString(r["Invoice"]);
                        invoice.InvoiceStatus = Convert.ToString(r["InvoiceStatus"]);
                        invoice.InvoiceDate = Convert.ToDateTime(r["InvoiceDate"]).ToString("dd/MM/yyyy");
                        invoice.InvoiceTotal = Convert.ToDouble(r["InvoiceTotal"]).ToString("C");
                        invoice.InvoiceType = Convert.ToString(r["InvoiceType"]);
                        invoice.Balance = Convert.ToDouble(r["Balance"]).ToString("C");
                        invoice.ButtonText = Convert.ToString(r["ButtonText"]).ToString();
                        invoice.EmailAddress = r["APEmail"].ToString();
                        invoice.OrderId = r["OrderId"].ToString();

                        // Comented by Tpss sh for CM Exception
                        //var transactionorderid = r["OrderId"].ToString().Replace("CM", "");

                        var transactionorderid = r["OrderId"].ToString().Replace("CM", "").Replace("CG", "").Replace("cg", "");
                        var bb = transactionorderid == "" ? null : _orderService.GetOrderByIdAsync(Convert.ToInt32(transactionorderid));
                        if (bb != null)
                        {
                            if (bb.Result != null)
                            {
                                var aa = bb.Result.ParentCustomerId == 0 ? bb.Result.CustomerId : bb.Result.ParentCustomerId;

                                invoice.CustomerName = _customerService.GetCustomerByIdAsync(aa).Result.FirstName;
                            }
                            else
                            {
                                invoice.CustomerName = "";
                            }
                        }
                        invList.Add(invoice);
                    }
                    model.Invoices = invList;
                }
                if (dtInvoice1.Rows.Count > 0)
                {
                    foreach (DataRow r in dtInvoice1.Rows)
                    {
                        //invList1.Add(new InvoiceModel
                        //{
                        //    Invoice = Convert.ToString(r["Invoice"]),
                        //    InvoiceStatus = Convert.ToString(r["InvoiceStatus"]),
                        //    InvoiceDate = Convert.ToDateTime(r["InvoiceDate"]).ToString("dd/MM/yyyy"),
                        //    InvoiceTotal = Convert.ToDouble(r["InvoiceTotal"]).ToString("C"),
                        //    InvoiceType = Convert.ToString(r["InvoiceType"]),
                        //    Balance = Convert.ToDouble(r["Balance"]).ToString("C"),
                        //    ButtonText = Convert.ToString(r["ButtonText"]).ToString(),
                        //    EmailAddress = r["APEmail"].ToString(),
                        //    OrderId = r["OrderId"].ToString(),
                        //    CustomerName = _customerService.GetCustomerByIdAsync(_orderService.GetOrderByIdAsync(Convert.ToInt32(r["OrderId"])).Result.ParentCustomerId == 0 ? _orderService.GetOrderByIdAsync(Convert.ToInt32(r["OrderId"])).Result.CustomerId : _orderService.GetOrderByIdAsync(Convert.ToInt32(r["OrderId"])).Result.ParentCustomerId).Result.FirstName
                        //});
                        InvoiceModel invoice = new InvoiceModel();
                        invoice.Invoice = Convert.ToString(r["Invoice"]);
                        invoice.InvoiceStatus = Convert.ToString(r["InvoiceStatus"]);
                        invoice.InvoiceDate = Convert.ToDateTime(r["InvoiceDate"]).ToString("dd/MM/yyyy");
                        invoice.InvoiceTotal = Convert.ToDouble(r["InvoiceTotal"]).ToString("C");
                        invoice.InvoiceType = Convert.ToString(r["InvoiceType"]);
                        invoice.Balance = Convert.ToDouble(r["Balance"]).ToString("C");
                        invoice.ButtonText = Convert.ToString(r["ButtonText"]).ToString();
                        invoice.EmailAddress = r["APEmail"].ToString();
                        invoice.OrderId = r["OrderId"].ToString();

                        // Comented by Tpss sh for CM Exception
                        //var transactionorderid = r["OrderId"].ToString().Replace("CM", "");

                        var transactionorderid = r["OrderId"].ToString().Replace("CM", "").Replace("CG", "").Replace("cg", "");
                        var bb = transactionorderid == "" ? null : _orderService.GetOrderByIdAsync(Convert.ToInt32(transactionorderid));
                        if (bb != null)
                        {
                            if (bb.Result != null)
                            {
                                var aa = bb.Result.ParentCustomerId == 0 ? bb.Result.CustomerId : bb.Result.ParentCustomerId;

                                invoice.CustomerName = _customerService.GetCustomerByIdAsync(aa).Result.FirstName;
                            }
                            else
                            {
                                invoice.CustomerName = "";
                            }
                        }
                        invList1.Add(invoice);
                    }
                    model.Invoices = invList1;
                }

            }

            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message.ToString();
            }

            return View(model);
        }
        private DataTable GetAllTransaction(string email, string Bpcode)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            //var connstring = datasettings.ConnectionString;
            var connstring = _configuration["Nop:HansamedConnectionistring"];
            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();

            var cmd = new SqlCommand("dbo.GetOfflineTransaction", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 0;
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Bpcode", Bpcode);
            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (Exception)
            {
                return new DataTable();
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }

        public ActionResult GetAllTransactionInvoicesYear(string Bpcode, string email, string Selectedyear, string type)
        {
            string partialView = "~/Views/Customer/Partial_offlineTransaction.cshtml";
            OfflineTransactionModel model = new OfflineTransactionModel();
            IList<InvoiceModel> invList = model.Invoices;
            IList<InvoiceModel> invList1 = model.Invoices;
            var dtInvoices = GetAllTransactionInvoices(Bpcode, email, Selectedyear, type);
            var dtInvoice1 = GetAllTransactioncreditmemo(Bpcode, email, Selectedyear, type);
            if (type == "Credit Memo")
            {
                if (dtInvoice1.Rows.Count > 0)
                {
                    foreach (DataRow r in dtInvoice1.Rows)
                    {
                        invList1.Add(new InvoiceModel
                        {
                            Invoice = Convert.ToString(r["Invoice"]),
                            InvoiceStatus = Convert.ToString(r["InvoiceStatus"]),
                            InvoiceDate = Convert.ToDateTime(r["InvoiceDate"]).ToString("dd/MM/yyyy"),
                            InvoiceTotal = Convert.ToDouble(r["InvoiceTotal"]).ToString("C"),
                            InvoiceType = Convert.ToString(r["InvoiceType"]),
                            Balance = Convert.ToDouble(r["Balance"]).ToString("C"),
                            ButtonText = Convert.ToString(r["ButtonText"]).ToString(),
                            EmailAddress = r["APEmail"].ToString(),
                            OrderId = r["OrderId"].ToString()
                        });
                    }
                    model.Invoices = invList1;
                }
            }
            else
            {
                if (dtInvoices.Rows.Count > 0)
                {
                    foreach (DataRow r in dtInvoices.Rows)
                    {
                        invList.Add(new InvoiceModel
                        {
                            Invoice = Convert.ToString(r["Invoice"]),
                            InvoiceStatus = Convert.ToString(r["InvoiceStatus"]),
                            InvoiceDate = Convert.ToDateTime(r["InvoiceDate"]).ToString("dd/MM/yyyy"),
                            InvoiceTotal = Convert.ToDouble(r["InvoiceTotal"]).ToString("C"),
                            InvoiceType = Convert.ToString(r["InvoiceType"]),
                            Balance = Convert.ToDouble(r["Balance"]).ToString("C"),
                            ButtonText = Convert.ToString(r["ButtonText"]).ToString(),
                            EmailAddress = r["APEmail"].ToString(),
                            OrderId = r["OrderId"].ToString()
                        });
                    }
                    model.Invoices = invList;
                }
            }
            return Json(new { result = 1, innerhtml = RenderPartialViewToStringAsync(partialView, model).Result });

        }
        private DataTable GetAllTransactionInvoices(string Bpcode, string email, string Selectedyear, string type)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            //var connstring = datasettings.ConnectionString;
            var connstring = _configuration["Nop:HansamedConnectionistring"];
            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();

            var cmd = new SqlCommand("dbo.GetOfflineTransaction_InvoiceNumber", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 0;
            cmd.Parameters.AddWithValue("@Bpcode", Bpcode);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Selectedyear", Selectedyear); //shubham
            cmd.Parameters.AddWithValue("@InvoiceNumber", type);
            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (Exception)
            {
                return new DataTable();
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }
        private DataTable GetAllTransactioncreditmemo(string Bpcode, string email, string Selectedyear, string type)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            // var connstring = datasettings.ConnectionString;
            var connstring = _configuration["Nop:HansamedConnectionistring"];
            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();

            var cmd = new SqlCommand("dbo.GetOfflineTransaction_creditmemo", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 0;
            cmd.Parameters.AddWithValue("@Bpcode", Bpcode);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Selectedyear", Selectedyear); //shubham
            cmd.Parameters.AddWithValue("@InvoiceNumber", type);
            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (Exception)
            {
                return new DataTable();
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }
        [HttpsRequirement]
        public virtual IActionResult PdfInvoice(string invoiceid, string invType)
        {
            //invoiceid = "4779175"; // for Testing

            if (!_customerService.IsRegisteredAsync(_workContext.GetCurrentCustomerAsync().Result).Result)
                return Challenge();

            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                if (invType == "Invoice Number")  // Invoice
                {
                    Core.Domain.Invoices.Invoice ObjInv = GetPdfInvoice(invoiceid, invType);
                    List<InvoiceDetails> ObjInvDetails = GetPdfInvoiceDetails(Convert.ToInt32(ObjInv.DocEntry), invType);
                    List<TaxCodeDetails> ObjTaxCodeDetails = GetPdfInvoiceTaxCodeDetails(Convert.ToInt32(ObjInv.DocEntry), Convert.ToDecimal(ObjInv.NetFreight), invType);
                    List<DownPayments> ObjDownPayments = GetPdfInvoiceDownPayments(Convert.ToInt32(invoiceid), invType);
                    ObjInv.StoreId = _storeContext.GetCurrentStore().Id;

                    _pdfService.PrintInvoiceToPdf(stream, ObjInv, ObjInvDetails, ObjTaxCodeDetails, ObjDownPayments, _orderSettings.GeneratePdfInvoiceInCustomerLanguage ? 0 : _workContext.GetWorkingLanguageAsync().Result.Id);
                }
                else // Credit Memo
                {
                    Core.Domain.Invoices.Invoice ObjInv = GetPdfInvoice(invoiceid, invType);
                    List<InvoiceDetails> ObjInvDetails = GetPdfInvoiceDetails(Convert.ToInt32(ObjInv.DocEntry), invType);
                    List<TaxCodeDetails> ObjTaxCodeDetails = GetPdfInvoiceTaxCodeDetails(Convert.ToInt32(ObjInv.DocEntry), Convert.ToDecimal(ObjInv.NetFreight), invType);
                    List<DownPayments> ObjDownPayments = GetPdfInvoiceDownPayments(Convert.ToInt32(invoiceid), invType);
                    ObjInv.StoreId = _storeContext.GetCurrentStore().Id;

                    _pdfService.PrintCreditMemoToPdf(stream, ObjInv, ObjInvDetails, ObjTaxCodeDetails, ObjDownPayments, _orderSettings.GeneratePdfInvoiceInCustomerLanguage ? 0 : _workContext.GetWorkingLanguageAsync().Result.Id);
                }

                bytes = stream.ToArray();
            }

            if (invType == "Invoice Number")
            {
                return File(bytes, MimeTypes.ApplicationPdf, $"invoice_{invoiceid}.pdf");
            }
            else
            {
                return File(bytes, MimeTypes.ApplicationPdf, $"creditMemo_{invoiceid}.pdf");
            }


        }
        [HttpsRequirement]
        public virtual IActionResult PayNow(string emailAddress)
        {
            if (!_customerService.IsRegisteredAsync(_workContext.GetCurrentCustomerAsync().Result).Result)
                return Challenge();

            // link to jump  paymentinvoice#opc-custnumber
            return RedirectToAction("Index", "PaymentInvoice", new { email = emailAddress });
        }
        private Core.Domain.Invoices.Invoice GetPdfInvoice(string invoiceid, string invType)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();
            SqlCommand cmd;
            if (invType == "Invoice Number")
            {
                cmd = new SqlCommand("select * from ViewOfflineTransactionGetInvoice where DocumentNumber ='" + invoiceid + "'", conn);
            }
            else
            {
                cmd = new SqlCommand("select * from ViewOfflineTransactionGetCreditMemo where DocumentNumber ='" + invoiceid + "'", conn);
            }

            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;

            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                DataRow dr = dt.Rows[0];

                Core.Domain.Invoices.Invoice Obj = new Core.Domain.Invoices.Invoice();

                Obj.DocEntry = dr["DocEntry"].ToString();
                Obj.DocumentNumber = dr["DocumentNumber"].ToString();
                Obj.DocumentDate = dr["DocumentDate"].ToString();
                Obj.DueDate = dr["DueDate"].ToString();
                Obj.CustomerNo = dr["CustomerNo"].ToString();
                Obj.ReferencePoNo = dr["ReferencePoNo"].ToString();
                Obj.AccountManager = dr["AccountManager"].ToString();
                Obj.ServiceContractNo = dr["ServiceContractNo"].ToString();
                Obj.BillToName = dr["BillToName"].ToString();
                Obj.BillToAddress = dr["BillToAddress"].ToString();
                Obj.ShipToName = dr["ShipToName"].ToString();
                Obj.ShipToAddress = dr["ShipToAddress"].ToString();
                Obj.ShippingType = dr["ShippingType"].ToString();
                Obj.FreightCharge = Convert.ToDouble(dr["FreightCharge"]).ToString("0.##");
                Obj.TotalTaxAmount = Convert.ToDouble(dr["TotalTaxAmount"]).ToString("0.##");
                Obj.TotalAfterDownPayment = Convert.ToDouble(dr["TotalAfterDownPayment"]).ToString("0.##");
                Obj.Downpayment = Convert.ToDouble(dr["Downpayment"]).ToString("0.##");
                Obj.Currency = dr["Currency"].ToString();
                Obj.APEmail = dr["APEmail"].ToString();
                Obj.DescriptionFreight = dr["DescriptionFreight"].ToString();
                Obj.NetFreight = Convert.ToDouble(dr["NetFreight"]).ToString("0.##");
                Obj.TaxCodeFreight = dr["TaxCodeFreight"].ToString();
                Obj.TaxPrcFreight = Convert.ToDouble(dr["TaxPrcFreight"]).ToString("0.##");
                Obj.TaxFreight = Convert.ToDouble(dr["TaxFreight"]).ToString("0.##");
                Obj.GrossFreight = Convert.ToDouble(dr["GrossFreight"]).ToString("0.##");
                Obj.Paid = dr["Paid"].ToString();
                Obj.Balance = Convert.ToDouble(dr["Balance"]).ToString("0.##C");

                Obj.OrderId = dr["OrderId"].ToString();
                Obj.DocStatus = dr["DocStatus"].ToString();
                Obj.CreateDate = dr["CreateDate"].ToString();
                Obj.UpdateDate = dr["UpdateDate"].ToString();

                Obj.DownpaymentNum = dr["DownpaymentNum"].ToString();
                Obj.PaymentTerms = dr["PaymentTerms"].ToString();
                Obj.Remarks = dr["Remarks"].ToString();
                Obj.InvoiceType = dr["InvoiceType"].ToString();

                return Obj;
            }
            catch (Exception)
            {
                return new Core.Domain.Invoices.Invoice();
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }
        private List<InvoiceDetails> GetPdfInvoiceDetails(int DocEntry, string invType)
        {
            List<InvoiceDetails> ObjInvDetails = new List<InvoiceDetails>();

            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();

            SqlCommand cmd;
            if (invType == "Invoice Number")
            {
                cmd = new SqlCommand("select * from ViewOfflineTransactionGetInvoiceDetails where DocEntry =" + DocEntry + " Order by Description", conn);
            }
            else
            {
                cmd = new SqlCommand("select * from ViewOfflineTransactionGetCreditMemoDetails where DocEntry =" + DocEntry + " Order by Description", conn);
            }

            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;

            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                ObjInvDetails = (from DataRow row in dt.Rows
                                 select new InvoiceDetails
                                 {
                                     DocEntry = row["DocEntry"].ToString(),
                                     ItemCode = row["ItemCode"].ToString(),
                                     Description = row["Description"].ToString(),
                                     QtyOrd = row["QtyOrd"].ToString(),
                                     QtyShip = row["QtyShip"].ToString(),
                                     QtyBackOrd = row["QtyBackOrd"].ToString(),
                                     UnitPrice = row["UnitPrice"].ToString(),
                                     DiscountPrc = row["DiscountPrc"].ToString(),
                                     Tax = row["Tax"].ToString(),
                                     NetPrice = row["NetPrice"].ToString(),
                                     Total = row["Total"].ToString(),
                                 }).ToList();

                return ObjInvDetails;
            }
            catch (Exception)
            {
                return new List<InvoiceDetails>();
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }
        private List<TaxCodeDetails> GetPdfInvoiceTaxCodeDetails(int DocEntry, decimal FreightCharges, string invType)
        {
            List<TaxCodeDetails> ObjTaxCodeDetails = new List<TaxCodeDetails>();

            var datasettings = DataSettingsManager.LoadSettings();
            //var connstring = datasettings.ConnectionString;
            var connstring = _configuration["Nop:HansamedConnectionistring"];
            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();

            SqlCommand cmd;
            if (invType == "Invoice Number")
            {
                cmd = new SqlCommand("select Max(TaxCode) as TaxCode , Max(TaxPrc) as TaxPrc,Sum(NetTax) as NetTax,Sum(TotalTax) as TotalTax from SapInvoicesItems where DocEntry = " + DocEntry + " Group by TaxCode", conn);
            }
            else
            {
                cmd = new SqlCommand("select Max(TaxCode) as TaxCode , Max(TaxPrc) as TaxPrc,Sum(NetTax) as NetTax,Sum(TotalTax) as TotalTax from SapCreditMemosItems where DocEntry = " + DocEntry + " Group by TaxCode", conn);
            }

            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;
            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                ObjTaxCodeDetails = (from DataRow row in dt.Rows
                                     select new TaxCodeDetails
                                     {
                                         TaxCode = row["TaxCode"].ToString(),
                                         TaxPrc = row["TaxPrc"].ToString(),
                                         NetTax = (Convert.ToDecimal(row["NetTax"]) + FreightCharges).ToString(), // add Freight Charge in NetTax
                                         TotalTax = ((Convert.ToDecimal(row["TaxPrc"]) * ((Convert.ToDecimal(row["NetTax"]) + FreightCharges)) / 100).ToString()),  // Calculating Percentage

                                     }).ToList();

                return ObjTaxCodeDetails;
            }
            catch (Exception)
            {
                return new List<TaxCodeDetails>();
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }
        private List<DownPayments> GetPdfInvoiceDownPayments(int DocEntry, string invType)
        {
            List<DownPayments> Obj = new List<DownPayments>();

            var datasettings = DataSettingsManager.LoadSettings();
            //var connstring = datasettings.ConnectionString;
            var connstring = _configuration["Nop:HansamedConnectionistring"];
            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();

            SqlCommand cmd;
            if (invType == "Invoice Number")
            {
                cmd = new SqlCommand("select ISNULL(DownpaymentNum,'') as DocNo,DocumentDate as DocDate, Downpayment as Gross,  Downpayment as Payment from SapInvoices where DocumentNumber =" + DocEntry + "", conn);
            }
            else
            {
                cmd = new SqlCommand("select ISNULL(DownpaymentNum,'') as DocNo,DocumentDate as DocDate, Downpayment as Gross,  Downpayment as Payment from SapCreditMemos where DocumentNumber =" + DocEntry + "", conn);
            }
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;

            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                Obj = (from DataRow row in dt.Rows
                       select new DownPayments
                       {
                           DocNo = row["DocNo"].ToString(),
                           DocDate = row["DocDate"].ToString(),
                           Gross = row["Gross"].ToString(),
                           Payment = row["Payment"].ToString(),

                       }).ToList();

                return Obj;
            }
            catch (Exception)
            {
                return new List<DownPayments>();
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }
        public virtual IActionResult CheckImprotedCustomer()
        {
            PriceChange priceChange = new PriceChange();
            ViewBag.ISlogin = false;
            var data = priceChange.Getimportedcustomer(_workContext.GetCurrentCustomerAsync().Result.Id);
            if (_workContext.GetCurrentCustomerAsync().Result.Email != null)
            {
                ViewBag.ISlogin = true;
            }
            if (data > 0)
            {
                return PartialView("~/Views/Customer/ImportedCustomer.cshtml");
            }
            return Json(0);


        }
        public class CustomerInfo
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string Active { get; set; }
        }
        public List<CustomerInfo> Getthesubcustomerlist()
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var currcustomer = _workContext.GetCurrentCustomerAsync().Result.BPCode;
            if (_workContext.OriginalCustomerIfImpersonated != null)
            {
                currcustomer = _workContext.OriginalCustomerIfImpersonated.BPCode;
            }
            //var currcustomer = "C80040";
            using (var conn = new SqlConnection(connstring))
            {

                {
                    conn.Open();
                    var query = "SELECT   cast(id as varchar(max)) as Id,  CONCAT(FirstName, ' (', BPCode, ')') as DisplayName ,iif(Active=1,'True','false') as Active FROM Customer where BPCode in (SELECT value FROM AssociatedCustomer_mapping  CROSS APPLY STRING_SPLIT(ChildBPCode, ',') where  AdminBpCode= '" + currcustomer + "' )";
                    var result = conn.Query<CustomerInfo>(query);

                    return result.ToList();
                }
            }
        }
        public virtual async Task<IActionResult> AssociatedCustomerList()
        {
            var currcustomer = _workContext.GetCurrentCustomerAsync().Result.BPCode;
            var associatedList = new List<AssociatedCustomerModel>();

            int productId = 0;
            var lastname = "";
            PriceChange priceChange = new PriceChange();
            var store = _storeContext.GetCurrentStoreAsync();
            try
            {
                var connection = DataSettingsManager.LoadSettings().ConnectionString;

                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@AdminBpCode", currcustomer);

                    var result = conn1.Query("AssociatedCustomerList", param: ds, commandType: CommandType.StoredProcedure);
                    foreach (var i in result)
                    {
                        var customer = await _customerService.GetCustomerByIdAsync(i.CustomerId);
                        var allRoles = await _customerService.GetCustomerRolesAsync(customer);
                        var assignedrole = new CustomerRole();
                        foreach (var obj in allRoles)
                        {
                            if (obj.Name == "CAD B2C" || obj.Name == "USD B2C" || obj.Name == "USD B2B INTL" || obj.Name == "CAD B2C HS" || obj.Name == "USD B2B ST" || obj.Name == "USD B2B DS" || obj.Name == "CAD B2B ST" || obj.Name == "CAD B2B DS")
                            {
                                assignedrole = obj;
                            }

                        }

                        var associated = new AssociatedCustomerModel();
                        var asso = new associateorder();
                        //lastname = i.FirstName;
                        //if (lastname != i.FirstName)
                        {
                            associated.FirstName = i.FirstName;

                            // foreach (var asso in subassociate)
                            {
                                asso.OrderId = i.orderId == null ? 0 : i.orderId;
                                asso.BPCode = i.BPCode;
                                var price = priceChange.getthelowestprice(Convert.ToInt32(i.Productid), i.CustomerId, assignedrole.Id);
                                if (price == "$0.00")
                                {
                                    asso.Price = priceChange.Gettierprice(Convert.ToInt32(i.Productid), assignedrole.Id);
                                }
                                associated.associateorders.Add(asso);
                            }
                        }
                        associatedList.Add(associated);


                    }
                    associatedList = associatedList
                      .GroupBy(e => e.FirstName)
                      .Select(g => new AssociatedCustomerModel
                      {
                          FirstName = g.Key,
                          associateorders = g.SelectMany(e => e.associateorders).ToList()
                      })
                      .ToList();

                    return View(associatedList);


                }
            }
            catch (Exception ex)
            {
                return View();
            }


        }

        public virtual IActionResult Impersonate(int id)
        {
            var customer = _customerService.GetCustomerByIdAsync(id).Result;
            if (customer == null)
                return RedirectToAction("List");

            if (!customer.Active)
            {
                _notificationService.WarningNotification(
                     _localizationService.GetResourceAsync("Admin.Customers.Customers.Impersonate.Inactive").Result);
                return RedirectToAction("Edit", customer.Id);
            }

            //ensure that a non-admin user cannot impersonate as an administrator
            //otherwise, that user can simply impersonate as an administrator and gain additional administrative privileges
            var currentCustomer = _workContext.GetCurrentCustomerAsync().Result;
            if (!_customerService.IsAdminAsync(currentCustomer).Result && _customerService.IsAdminAsync(customer).Result)
            {
                _notificationService.ErrorNotification(_localizationService.GetResourceAsync("Admin.Customers.Customers.NonAdminNotImpersonateAsAdminError").Result);
                return RedirectToAction("Edit", customer.Id);
            }

            //activity log
            _customerActivityService.InsertActivityAsync("Impersonation.Started",
               string.Format(_localizationService.GetResourceAsync("ActivityLog.Impersonation.Started.StoreOwner").Result, customer.Email, customer.Id), customer);
            _customerActivityService.InsertActivityAsync(customer, "Impersonation.Started",
               string.Format(_localizationService.GetResourceAsync("ActivityLog.Impersonation.Started.Customer").Result, currentCustomer.Email, currentCustomer.Id), currentCustomer);

            //ensure login is not required
            customer.RequireReLogin = false;
            _customerService.UpdateCustomerAsync(customer);
            _genericAttributeService.SaveAttributeAsync<int?>(currentCustomer, NopCustomerDefaults.ImpersonatedCustomerIdAttribute, customer.Id);
            _staticCacheManager.ClearAsync();
            return RedirectToAction("Index", "Home", new { area = string.Empty });


        }
        //available even when a store is closed
        [CheckAccessClosedStore(ignore: true)]
        //available even when navigation is not allowed
        [CheckAccessPublicStore(ignore: true)]
        public virtual async Task<IActionResult> finishImpersonate()
        {
            var customer = _workContext.GetCurrentCustomerAsync().Result;
            if (_workContext.OriginalCustomerIfImpersonated != null)
            {
                //activity log
                await _customerActivityService.InsertActivityAsync(_workContext.OriginalCustomerIfImpersonated, "Impersonation.Finished",
                    string.Format(await _localizationService.GetResourceAsync("ActivityLog.Impersonation.Finished.StoreOwner"),
                        customer.Email, customer.Id),
                    customer);

                await _customerActivityService.InsertActivityAsync("Impersonation.Finished",
                    string.Format(await _localizationService.GetResourceAsync("ActivityLog.Impersonation.Finished.Customer"),
                        _workContext.OriginalCustomerIfImpersonated.Email, _workContext.OriginalCustomerIfImpersonated.Id),
                    _workContext.OriginalCustomerIfImpersonated);

                //logout impersonated customer
                await _genericAttributeService
                    .SaveAttributeAsync<int?>(_workContext.OriginalCustomerIfImpersonated, NopCustomerDefaults.ImpersonatedCustomerIdAttribute, null);

                //redirect back to customer details page (admin area)

                //var myValue = Session["ImpersonateCustomer"].ToString();
                HttpContext.Request.Cookies.TryGetValue("ImpersonateCustomer", out var cookieLanguageCode);
                HttpContext.Request.Cookies.TryGetValue("ImpersonateCustomerid", out var LanguageCode);
                if (cookieLanguageCode != "Impersonate")
                {

                    return RedirectToAction("Edit", "Customer", new { id = customer.Id, area = AreaNames.Admin });
                }
                else
                {
                    return RedirectToAction("Index", "Home", new { area = string.Empty });
                }

            }
            return Json(new { });
        }

        public virtual async Task<IActionResult> PopupPromotionheader()
        {
            PopupCustomPromotion popupCustom = new PopupCustomPromotion();
            popupCustom.PopupActiveInActive = Convert.ToBoolean(_settingService.GetSetting("PopupActiveInActive").Value);
            if (popupCustom.PopupActiveInActive == true)
            {

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

                popupCustom.PopupPromotionContent = _settingService.GetSetting("PopupPromotionContent").Value;
                popupCustom.PopupPromotionUrl = _settingService.GetSetting("PopupPromotionUrl").Value;
                popupCustom.StartWeek = _settingService.GetSetting("StartWeek").Value;
                popupCustom.EndWeek = _settingService.GetSetting("EndWeek").Value;
                var customer = _workContext.GetCurrentCustomerAsync().Result.Email;
                DateTime now = DateTime.Now;
                DayOfWeek currentDay = now.DayOfWeek;
                int currentDayOfWeek = GetDayOfWeekValue(Convert.ToString(currentDay));
                int fromDayOfWeek = GetDayOfWeekValue(popupCustom.StartWeek);
                int toDayOfWeek = GetDayOfWeekValue(popupCustom.EndWeek);

                if (GetallRoles != null && customer == null && fromDayOfWeek <= currentDayOfWeek && toDayOfWeek >= currentDayOfWeek)
                {

                    return Json(new { Result = popupCustom });
                }
                else
                {
                    return Json(new { Result = 0 });
                }
            }
            else
            {
                return Json(new { Result = 0 });
            }

        }
        static int GetDayOfWeekValue(string weekDay)
        {
            return weekDay switch
            {

                "Monday" => 1,
                "Tuesday" => 2,
                "Wednesday" => 3,
                "Thursday" => 4,
                "Friday" => 5,
                "Saturday" => 6,
                "Sunday" => 7,
                _ => throw new ArgumentException("Invalid weekday")
            };
        }

        public virtual ActionResult CustomerOrdersGraph()
        {
            IActionResult datavalue = OfflineTransactions();
            if (datavalue is ViewResult viewResult)
            {
                var model = viewResult.Model as Nop.Web.Models.OfflineTransaction.OfflineTransactionModel;
                ViewBag.TotalOutstandingPayment = model.TotalOutstandingPayment;
                ViewBag.OutstandingInvoices = model.OutstandingInvoices;
                ViewBag.AccountCredits = model.AccountCredits;
                ViewBag.currcustomer = _workContext.GetCurrentCustomerAsync().Result.Email;
            }

            var reward = _orderModelFactory.PrepareCustomerRewardPointsAsync(null);
            ViewBag.RewardPointsBalance = reward.Result.RewardPointsBalance;
            ViewBag.RewardPointsAmount = reward.Result.RewardPointsAmount;

            var graphlist = new List<Graph>();
            Graph graph = new Graph();
            graph.GraphName = "orders";
            graph.Prefix = "order-statistics";
            graph.TotalNumber = 1;
            graphlist.Add(graph);
            //Graph graph1 = new Graph();
            //graph1.GraphName = "PaymentInvoice";
            //graph1.Prefix = "PaymentInvoice-statistics";
            //graph1.TotalNumber = 2;
            //graphlist.Add(graph1);
            Graph graph2 = new Graph();
            graph2.GraphName = "AccountBalance";
            graph2.Prefix = "AccountBalance-statistics";
            graph2.TotalNumber = 3;
            graphlist.Add(graph2);
            _staticCacheManager.ClearAsync();
            ViewBag.customerBpcode = _workContext.GetCurrentCustomerAsync().Result.BPCode;
            return View(graphlist);
        }
        public virtual ActionResult CustomerOrdersGraph1()
        {
            return PartialView("~/Views/Customer/_CustomerOrdersGraph1.cshtml");

        }
        public class DataTableRequest
        {
            public int Draw { get; set; } // Draw counter for DataTables
            public int Start { get; set; } // Start index of data
            public int Length { get; set; } // Number of records to fetch
            public DataTableSearch Search { get; set; } // Search term
            public List<DataTableColumn> Columns { get; set; } // Columns details
            public List<DataTableOrder> Order { get; set; } // Order details
        }

        public class DataTableSearch
        {
            public string Value { get; set; } // Search value
            public bool Regex { get; set; } // Use regex or not
        }

        public class DataTableColumn
        {
            public string Data { get; set; } // Column name
            public string Name { get; set; }
            public bool Searchable { get; set; }
            public bool Orderable { get; set; }
            public DataTableSearch Search { get; set; }
        }

        public class DataTableOrder
        {
            public int Column { get; set; } // Column index
            public string Dir { get; set; } // Direction: asc or desc
        }

        public virtual async Task<IActionResult> Latestorders(DataTableRequest request)
        {


            try
            {
                // Extract parameters
                int skip = request.Start; // Start index for pagination
                int take = request.Length; // Number of records to fetch
                string searchValue = request.Search?.Value; // Search term
                                                            // string sortColumn = request.Columns[request.Order[0].Column].Data; // Column to sort by
                                                            // string sortDirection = request.Order[0].Dir; // Sort direction: asc/desc
                OrderSearchModel orderSearchModel = new OrderSearchModel();
                // Query the database
                //var query = _dbContext.Orders.AsQueryable();
                var query = _orderModelFactory.PrepareCustomerOrderListModelAsync().Result.Orders;
                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(x =>
                        x.CustomOrderNumber.Contains(searchValue) ||
                        x.OrderStatus.Contains(searchValue)).ToList();
                }

                // Apply sorting
                //query = sortDirection == "asc"
                //    ? query.OrderBy(e => EF.Property<object>(e, sortColumn))
                //    : query.OrderByDescending(e => EF.Property<object>(e, sortColumn));

                // Get total records and paginated data
                int totalRecords = query.Count();
                var data = query.Skip(skip).Take(take).ToList();

                foreach (var product in data)
                {
                    product.CustomerEmail = _workContext.GetCurrentCustomerAsync().Result.Email; // Adding a new column to each product
                }
                // Return the data in DataTables format
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords, // Adjust this if filtering
                    data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message
                });
            }

        }


        public virtual async Task<IActionResult> BestsellerbyQuantity(DataTableRequest request)
        {


            try
            {
                // Extract parameters
                int skip = request.Start; // Start index for pagination
                int take = request.Length; // Number of records to fetch
                string searchValue = request.Search?.Value; // Search term
                                                            // string sortColumn = request.Columns[request.Order[0].Column].Data; // Column to sort by
                                                            // string sortDirection = request.Order[0].Dir; // Sort direction: asc/desc
                BestsellerBriefSearchModel searchModel = new BestsellerBriefSearchModel();
                var curr = _workContext.GetCurrentCustomerAsync().Result.Id;
                // Query the database
                //var query = _dbContext.Orders.AsQueryable();
                //var query = _bestsellerFactory.PrepareBestsellerBriefListModelAsync(searchModel).Result.Data;

                var connection = DataSettingsManager.LoadSettings().ConnectionString;

                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@CustomerId", curr);

                    var query = conn1.Query<bestsellerquantity>("sp_getbestseller", param: ds, commandType: CommandType.StoredProcedure);

                    query = query.Where(x => x.ProductName != null).ToList();









                    var datasettings = DataSettingsManager.LoadSettings();
                    var connstring = datasettings.ConnectionString;


                    // Use Query<string> to ensure the result is a List<string>
                    // var query = conn.Query<bestsellerquantity>(dataa).ToList();


                    foreach (var i in query)
                    {
                        var productDetail = _productService.GetProductByIdAsync(i.parentproductid).Result;
                        var ProductSeName = _urlRecordService.GetSeNameAsync(productDetail).Result;
                        i.seoname = ProductSeName + "," + i.ProductId;
                        i.TotalAmount = "$" + i.TotalAmount;
                    }



                    // Apply filtering
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        query = query.Where(x =>
                        x.ProductName.Contains(searchValue)).ToList();
                    }

                    // Apply sorting
                    //query = sortDirection == "asc"
                    //    ? query.OrderBy(e => EF.Property<object>(e, sortColumn))
                    //    : query.OrderByDescending(e => EF.Property<object>(e, sortColumn));

                    // Get total records and paginated data
                    int totalRecords = query.Count();
                    var data = query.Skip(skip).Take(take).ToList();


                    // Return the data in DataTables format
                    return Json(new
                    {
                        draw = request.Draw,
                        recordsTotal = totalRecords,
                        recordsFiltered = totalRecords, // Adjust this if filtering
                        data
                    });

                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message
                });
            }

        }
        //[HttpPost]
        public virtual async Task<IActionResult> Whislistdashboard(string data)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var val = new object();

            try
            {
                using (var conn = new SqlConnection(connstring))
                {
                    conn.Open();
                    var dataa = "UPDATE ShoppingCartItem SET ShoppingCartTypeId = 1 WHERE Id = " + data;

                    // Execute the update query
                    await conn.ExecuteAsync(dataa);

                    _staticCacheManager.ClearAsync();


                    val = new { Value = "1" };
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                val = new { Value = "0" };
            }

            return Json(val);
        }


        public class Graph
        {
            public string GraphName { get; set; }
            public string Prefix { get; set; }
            public int TotalNumber { get; set; }
        }
        public class bestsellerquantity
        {
            public string ProductName { get; set; }
            public int TotalQuantity { get; set; }
            public int parentproductid { get; set; }
            public string TotalAmount { get; set; }
            public int ProductId { get; set; }
            public string seoname { get; set; }
        }



        public virtual async Task<IActionResult> PaymentInvoiceStatistics(string period, string Feature)
        {
            var curremail = _workContext.GetCurrentCustomerAsync().Result.Email;
            var connection = DataSettingsManager.LoadSettings().ConnectionString;
            var result = new List<object>();

            using (IDbConnection conn1 = new SqlConnection(connection))
            {
                DynamicParameters ds = new DynamicParameters();
                ds.Add("@Email", curremail);
                ds.Add("@data", period);
                ds.Add("@Feature", Feature);

                var rows = conn1.Query("GetPyamentinvoicegraph", param: ds, commandType: CommandType.StoredProcedure);

                foreach (var row in rows)
                {
                    var dynamicRow = (IDictionary<string, object>)row;
                    result.Add(new
                    {
                        InvoiceDateFormatted = dynamicRow["InvoiceDateFormatted"]?.ToString(),
                        TotalInvoices = Convert.ToInt32(dynamicRow["TotalInvoices"])
                    });
                }
            }

            return Json(result);
        }
        public virtual async Task<IActionResult> AccountBalanceStatistics(string period)
        {
            var result = new List<object>();
            IActionResult datavalue = OfflineTransactions();

            if (datavalue is ViewResult viewResult)
            {
                var model = viewResult.Model as Nop.Web.Models.OfflineTransaction.OfflineTransactionModel;

                // Separate invoices based on balance
                var offlineorder = model.Invoices.Where(x => x.Balance != "$0.00").ToList();
                var offlineorder1 = model.Invoices.Where(x => x.Balance == "$0.00").ToList();

                // Get culture info
                var culture = new CultureInfo((await _workContext.GetWorkingLanguageAsync()).LanguageCulture);

                // Helper method to process orders

                DateTime nowDt = DateTime.Now;

                switch (period)
                {
                    case "year":
                        DateTime oneYearAgo = nowDt.AddYears(-1).AddMonths(1);
                        var searchYearDateUser = new DateTime(oneYearAgo.Year, oneYearAgo.Month, 1);

                        for (var i = 0; i <= 12; i++)
                        {
                            var monthData = offlineorder.Where(y =>
                            {
                                var invoiceDate = DateTime.ParseExact(y.InvoiceDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                                return invoiceDate >= searchYearDateUser &&
                                       invoiceDate < searchYearDateUser.AddMonths(1);
                            }).ToList();
                            var monthData1 = offlineorder1.Where(y =>
                            {
                                var invoiceDate = DateTime.ParseExact(y.InvoiceDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                                return invoiceDate >= searchYearDateUser &&
                                       invoiceDate < searchYearDateUser.AddMonths(1);
                            }).ToList();

                            result.Add(new
                            {
                                date = searchYearDateUser.Date.ToString("Y", culture),
                                value = $"Non-Zero: {monthData.Count}, Zero: {monthData1.Count}",
                            });

                            searchYearDateUser = searchYearDateUser.AddMonths(1);
                        }
                        break;

                    case "month":
                        var monthAgoDt = nowDt.AddDays(-30);
                        var searchMonthDateUser = new DateTime(monthAgoDt.Year, monthAgoDt.Month, monthAgoDt.Day);

                        for (var i = 0; i <= 30; i++)
                        {
                            var monthData = offlineorder.Where(y =>
                            {
                                var invoiceDate = DateTime.ParseExact(y.InvoiceDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                                return invoiceDate >= searchMonthDateUser &&
                                       invoiceDate < searchMonthDateUser.AddDays(1);
                            }).ToList();
                            var monthData1 = offlineorder1.Where(y =>
                            {
                                var invoiceDate = DateTime.ParseExact(y.InvoiceDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                                return invoiceDate >= searchMonthDateUser &&
                                       invoiceDate < searchMonthDateUser.AddDays(1);
                            }).ToList();

                            result.Add(new
                            {
                                date = searchMonthDateUser.Date.ToString("M", culture),
                                value = $"Non-Zero: {monthData.Count}, Zero: {monthData1.Count}",
                            });

                            searchMonthDateUser = searchMonthDateUser.AddDays(1);
                        }
                        break;

                    case "week":
                    default:
                        var weekAgoDt = nowDt.AddDays(-7);
                        var searchWeekDateUser = new DateTime(weekAgoDt.Year, weekAgoDt.Month, weekAgoDt.Day);

                        for (var i = 0; i <= 7; i++)
                        {
                            var monthData = offlineorder.Where(y =>
                            {
                                var invoiceDate = DateTime.ParseExact(y.InvoiceDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                                return invoiceDate >= searchWeekDateUser &&
                                       invoiceDate < searchWeekDateUser.AddDays(1);
                            }).ToList();
                            var monthData1 = offlineorder1.Where(y =>
                            {
                                var invoiceDate = DateTime.ParseExact(y.InvoiceDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                                return invoiceDate >= searchWeekDateUser &&
                                       invoiceDate < searchWeekDateUser.AddDays(1);
                            }).ToList();

                            result.Add(new
                            {
                                date = searchWeekDateUser.Date.ToString("d dddd", culture),
                                value = $"Non-Zero: {monthData.Count}, Zero: {monthData1.Count}",
                            });

                            searchWeekDateUser = searchWeekDateUser.AddDays(1);
                        }
                        break;
                }
            }

            //// Process both sets of invoices
            //ProcessOrders(offlineorder, "Non-Zero: ");
            //ProcessOrders(offlineorder1, "Zero: ");


            return Json(result);

        }
        public virtual async Task<IActionResult> LoadOrderStatistics(string period)
        {
            //if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
            //    return Content(string.Empty);

            //a vendor doesn't have access to this report
            if (await _workContext.GetCurrentVendorAsync() != null)
                return Content(string.Empty);
            var curremail = _workContext.GetCurrentCustomerAsync().Result.Id;
            var result = new List<object>();

            var nowDt = await _dateTimeHelper.ConvertToUserTimeAsync(DateTime.Now);
            var timeZone = await _dateTimeHelper.GetCurrentTimeZoneAsync();

            var culture = new CultureInfo((await _workContext.GetWorkingLanguageAsync()).LanguageCulture);

            switch (period)
            {
                case "year":
                    //year statistics
                    var yearAgoDt = nowDt.AddYears(-1).AddMonths(1);
                    var searchYearDateUser = new DateTime(yearAgoDt.Year, yearAgoDt.Month, 1);
                    for (var i = 0; i <= 12; i++)
                    {
                        result.Add(new
                        {
                            date = searchYearDateUser.Date.ToString("Y", culture),
                            value = (await _orderService.SearchOrdersAsync(
                                customerId: curremail,
                                createdFromUtc: _dateTimeHelper.ConvertToUtcTime(searchYearDateUser, timeZone),
                                createdToUtc: _dateTimeHelper.ConvertToUtcTime(searchYearDateUser.AddMonths(1), timeZone),
                                pageIndex: 0,
                                pageSize: 1, getOnlyTotalCount: true)).TotalCount.ToString()
                        });

                        searchYearDateUser = searchYearDateUser.AddMonths(1);
                    }

                    break;
                case "month":
                    //month statistics
                    var monthAgoDt = nowDt.AddDays(-30);
                    var searchMonthDateUser = new DateTime(monthAgoDt.Year, monthAgoDt.Month, monthAgoDt.Day);
                    for (var i = 0; i <= 30; i++)
                    {
                        result.Add(new
                        {
                            date = searchMonthDateUser.Date.ToString("M", culture),
                            value = (await _orderService.SearchOrdersAsync(
                                customerId: curremail,
                                createdFromUtc: _dateTimeHelper.ConvertToUtcTime(searchMonthDateUser, timeZone),
                                createdToUtc: _dateTimeHelper.ConvertToUtcTime(searchMonthDateUser.AddDays(1), timeZone),
                                pageIndex: 0,
                                pageSize: 1, getOnlyTotalCount: true)).TotalCount.ToString()
                        });

                        searchMonthDateUser = searchMonthDateUser.AddDays(1);
                    }

                    break;
                case "week":
                default:
                    //week statistics
                    var weekAgoDt = nowDt.AddDays(-7);
                    var searchWeekDateUser = new DateTime(weekAgoDt.Year, weekAgoDt.Month, weekAgoDt.Day);
                    for (var i = 0; i <= 7; i++)
                    {
                        result.Add(new
                        {
                            date = searchWeekDateUser.Date.ToString("d dddd", culture),
                            value = (await _orderService.SearchOrdersAsync(
                                customerId: curremail,
                                createdFromUtc: _dateTimeHelper.ConvertToUtcTime(searchWeekDateUser, timeZone),
                                createdToUtc: _dateTimeHelper.ConvertToUtcTime(searchWeekDateUser.AddDays(1), timeZone),
                                pageIndex: 0,
                                pageSize: 1, getOnlyTotalCount: true)).TotalCount.ToString()
                        });

                        searchWeekDateUser = searchWeekDateUser.AddDays(1);
                    }

                    break;
            }

            return Json(result);
        }

        #region My account / My documents

        public virtual async Task<IActionResult> MyCustomDocuments()
        {
            // get current customer
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            // check valid customer
            if (!await _customerService.IsRegisteredAsync(currentCustomer))
                return Challenge();

            // Verify that the My Documents feature is available for the customer
            if (!await _customerService.IsMyDocumentsVisibleForBpCodeAsync(currentCustomer))
                return RedirectToRoute("Homepage");

            // Get customer documents
            var documents = await _customDocumentService
                .GetAllCustomDocumentsByBpCodeAsync(currentCustomer.BPCode);

            var model = new CustomDocumentsModel
            {
                MainCategories = documents
                .GroupBy(x => x.MainCategory)
                .OrderBy(g => g.Min(x => x.DisplayOrder))
                .Select(mainGroup => new MainCategoryModel
                {
                    MainCategory = mainGroup.Key,
                    MainCategoryClass = $"doc-{Regex.Replace(mainGroup.Key ?? string.Empty, @"[^a-zA-Z0-9]", "").ToLowerInvariant()}",

                    Items = mainGroup
                    .OrderBy(x => x.DisplayOrder)
                    .Select(document => 
                    {
                        var imageLink = !string.IsNullOrWhiteSpace(document.VideoLink)
                        ? document.VideoLink : document.DownloadLink;

                        return new CustomDocumentItemModel
                        {
                            Id = document.Id,
                            Category = document.Category,
                            SubCategory = document.SubCategory,
                            FileName = document.FileName,
                            DownloadLink = document.DownloadLink,
                            VideoLink = document.VideoLink ?? string.Empty,
                            DisplayOrder = document.DisplayOrder,

                            Picture = document.SubCategory.Equals("Image", StringComparison.OrdinalIgnoreCase)
                                ? new PictureModel
                                {
                                    ImageUrl = imageLink,
                                    ThumbImageUrl = imageLink,
                                    FullSizeImageUrl = imageLink,
                                    Title = document.Category,
                                    AlternateText = document.Category
                                }
                                : null
                        };
                    }).ToList()
                }).ToList()
            };

            return View(model);
        }

        #endregion

        #region Pre-Oders

        public virtual async Task<IActionResult> RegularAndPreOrders()
        {
            // get current customer
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            // check valid customer
            if (!await _customerService.IsRegisteredAsync(currentCustomer))
                return Challenge();

            // Verify that the pre-order feature is available for the customer
            if (!await _productService.IsPreOrderAvailableForCustomerOrProductAsync(currentCustomer, includeProducts: false))
                return RedirectToRoute("Homepage");

            var model = new ProductDetailsModel();

            // prepare delivery dates
            await PreparePreOrderSettingsAsync(model);

            return View(model);
        }

        [HttpGet]
        public virtual async Task<IActionResult> RegularAndPreOrdersList(DataTableRequest request, bool isPreOrder)
        {
            try
            {
                // Extract parameters
                int skip = request.Start; // Start index for pagination
                int take = request.Length; // Number of records to fetch
                string searchValue = request.Search?.Value; // Search term

                // current customer 
                var customer = await _workContext.GetCurrentCustomerAsync();

                // Get customer's orders filtered by preorder/regular
                var query = await _orderService.GetCustomerOrdersAsync(customer.Id, isPreOrder);

                var data = new List<OrderDetailsModel>();

                foreach (var order in query)
                {
                    var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

                    var preorderItem = orderItems.FirstOrDefault(x => x.IsPreOrder);

                    data.Add(new OrderDetailsModel
                    {
                        Id = order.Id,
                        CustomOrderNumber = order.CustomOrderNumber,
                        OrderStatus = order.OrderStatus.ToString(),
                        CustomerEmail = customer.Email,
                        CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(order.CreatedOnUtc, DateTimeKind.Utc),
                        PreferredDeliveryDate = preorderItem?.PreferredDeliveryDate
                    });
                }

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    data = data.Where(x =>
                            x.CustomOrderNumber.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                            x.OrderStatus.Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var totalRecords = data.Count;

                data = data.Skip(skip).Take(take).ToList();

                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message
                });
            }
        }

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


        [HttpPost]
        public virtual async Task<IActionResult> CancelPreOrder(int orderId)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();

            var order = await _orderService.GetOrderByIdAsync(orderId);

            if (order == null || order.CustomerId != customer.Id)
            {
                return Json(new
                {
                    success = false,
                    message = "Order not found."
                });
            }

            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

            if (!orderItems.Any(x => x.IsPreOrder))
            {
                return Json(new
                {
                    success = false,
                    message = "This order is not a preorder."
                });
            }

            try
            {
                // Check whether the order can be cancelled
                if (!_orderProcessingService.CanCancelOrder(order))
                {
                    return Json(new
                    {
                        success = false,
                        message = "This preorder cannot be cancelled."
                    });
                }

                await _orderProcessingService.CancelOrderAsync(order, true);

                await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "Preorder cancelled by customer.",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                return Json(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        [HttpPost]
        public virtual async Task<IActionResult> SendPreOrderEnquiryEmail(string subject, string message)
        {
            try
            {
                var customer = await _workContext.GetCurrentCustomerAsync();

                await SendPreOrderEnquiryAsync(customer, subject, message);

                return Json(new
                {
                    success = true,
                    message = "Your enquiry has been sent successfully."
                });
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Error sending preorder enquiry.", ex);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while sending your enquiry."
                });
            }
        }

        public virtual async Task SendPreOrderEnquiryAsync(Customer customer, string subject, string message)
        {
            var emailAccount = await _emailAccountService
                .GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);

            if (emailAccount == null)
                return;

            var body = $@"<p><strong>Customer Name:</strong> {await _customerService.GetCustomerFullNameAsync(customer)}</p>
                            <p><strong>Customer Email:</strong> {customer.Email}</p>
                            <p><strong>Subject:</strong> {subject}</p>
                            <br />
                          <p>{WebUtility.HtmlEncode(message).Replace("\n", "<br />")}</p>";

            var queuedEmail = new QueuedEmail
            {
                PriorityId = 5,
                EmailAccountId = emailAccount.Id,

                From = emailAccount.Email,
                FromName = emailAccount.DisplayName,

                // Send to store owner
                To = emailAccount.Email,
                ToName = emailAccount.DisplayName,

                // Optional: Reply goes to customer
                ReplyTo = customer.Email,
                ReplyToName = await _customerService.GetCustomerFullNameAsync(customer),

                Subject = $"PreOrder Enquiry - {subject}",
                Body = body,
                CreatedOnUtc = DateTime.UtcNow
            };

            await _queuedEmailService.InsertQueuedEmailAsync(queuedEmail);
        }

        #endregion

        #endregion
    }
}
