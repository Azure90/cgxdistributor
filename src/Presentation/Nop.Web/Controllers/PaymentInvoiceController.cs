using System;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Data;
using Nop.Services.Messages;
using Nop.Services.Logging;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Newtonsoft.Json;
using System.Linq;
using Nop.Core.Domain.Invoices;
using Nop.Services.Events;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Nop.Services.Customers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Services.Localization;
using System.Net;
using System.IO;
using System.Text.Json;
using Nop.Services.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Azure;
using Nop.Core.Events;
using Microsoft.Data.SqlClient;
using DocumentFormat.OpenXml.EMMA;
using Nop.Core.Domain.Messages;

namespace Nop.Web.Controllers
{

    public class PaymentInvoiceController : BasePublicController
    {

        #region Properties
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPaymentService _paymentService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IMessageTokenProvider _messageTokenProvider;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IStoreService _storeService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IConfiguration _configuration;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly EmailAccountSettings _emailAccountSettings;


        private string ErrorMailTemplate = "PaymentInvoice.PaymentCompleted";
        private int DefaultStoreId = 1;

        #endregion

        #region Constructor
        public PaymentInvoiceController(
            IPaymentPluginManager paymentPluginManager,
            IPaymentService paymentService,
            IStoreContext storeContext,
            IWebHelper webHelper,
            IWorkContext workContext,
            ILogger logger,
            INotificationService notificationService,
            IWorkflowMessageService workflowMessageService,
            IMessageTokenProvider messageTokenProvider,
            IMessageTemplateService messageTemplateService,
            IEmailAccountService emailAccountService,
            IStoreService storeService,
            IEventPublisher eventPublisher,
            ICustomerService customerService,
            ILocalizationService localizationService,
            ISettingService settingService,
            IConfiguration configuration,
            EmailAccountSettings emailAccountSettings,
             IQueuedEmailService queuedEmailService

            )
        {
            _paymentPluginManager = paymentPluginManager;
            _paymentService = paymentService;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _workContext = workContext;
            _notificationService = notificationService;
            _workflowMessageService = workflowMessageService;
            _messageTokenProvider = messageTokenProvider;
            _messageTemplateService = messageTemplateService;
            _storeService = storeService;
            _logger = logger;
            _emailAccountService = emailAccountService;
            _eventPublisher = eventPublisher;
            _customerService = customerService;
            _localizationService = localizationService;
            _settingService = settingService;
            _configuration = configuration;
            _emailAccountSettings = emailAccountSettings;
            _queuedEmailService = queuedEmailService;
        }

        #endregion

        // GET: PaymentInvoiceController

        public ActionResult Index(string email,string site)
        {
            // AT THIS POINT NOT GETTING ANY INVOICES INSTEAD GETTING CUSTOMERS BASED ON EMAIL

            //When customer use the link:
            //https://hansamed43-staging.azurewebsites.net/paymentinvoice
            //We need to check if the customer is logged in already.
            //1.If the customer is logged in, you will add the account email as a parameter into the url, like below:
            //https://hansamed43-staging.azurewebsites.net/paymentinvoice?email=c.wang@ha…
            //2.If the customer is not logged in, the customer is considered as a guest, 
            //and the customer can edit the customer number, email etc.
            var isAlreadyLoginCustomer = _customerService.IsRegisteredAsync(_workContext.GetCurrentCustomerAsync().Result).Result;
            if (isAlreadyLoginCustomer)
            {
               email = _workContext.GetCurrentCustomerAsync().Result.Email;
            }

            var PaymentInv = new PaymentInvoiceModel();
            if (String.IsNullOrEmpty(email))
            {
                // comming from Payment Invoice Link
                PaymentInv.CustomerNumber = "";
                PaymentInv.Email = "";
                PaymentInv.FirstName = "";
                PaymentInv.LastName = "";
                PaymentInv.WebPortalTransactionId = "";
                PaymentInv.Invoice = "";
                PaymentInv.InvoiceDetails = new List<PaymentInvoiceItemModel>();
                PaymentInv.CustomerNumbers = new List<SelectListItem>();
                return View(PaymentInv);
            }

            // there is no First Name and Last Name in SAPInvoice table
            // only getting First Name and Last Name from Login Customer from Web Shop
            var custName = GetCutomerNameByEmail(email);
            var firstName = custName.Split(",")[0];
            var lastName = custName.Split(",")[1];

            PaymentInv.Email = email;
            PaymentInv.FirstName = firstName;
            PaymentInv.LastName = lastName;

            List<SelectListItem> ObjCustomers = GetCustomersByEmail(email);

            if (ObjCustomers.Count > 1)
            {
                //insert element at first position
                ObjCustomers.Insert(0, new SelectListItem() { Value = "Select Customer", Text = "Select Customer" });
            }
            if(site== "Citagenix")
            {
                PaymentInv.IsCitagenix = "IsCitagenix";
            }
            PaymentInv.CustomerNumbers = ObjCustomers;
            return View(PaymentInv);
        }

        //public JsonResult GetInvoicesAmount(string emailId)
        //{
        //    // Guest Customer cannot be here

        //    List<Core.Domain.Invoices.Invoice> ObjInv = GetPdfInvoices(emailId);
        //    // Create a List of PaymentInvoiceItemModel  
        //    List<PaymentInvoiceItemModel> InvItem = new List<PaymentInvoiceItemModel>();

        //    foreach (Core.Domain.Invoices.Invoice inv in ObjInv)
        //    {
        //        //InvItem.Add(new PaymentInvoiceItemModel { Number = inv.DocumentNumber, Amount = Convert.ToDecimal(inv.TotalAfterDownPayment) });
        //        InvItem.Add(new PaymentInvoiceItemModel 
        //        { 
        //            Number = inv.DocumentNumber, 
        //            Amount = inv.InvoiceType== "Credit Memo" ? - Convert.ToDecimal(inv.Balance) : Convert.ToDecimal(inv.Balance),
        //            InvoiceType = inv.InvoiceType
        //        });
        //    }

        //    return Json(InvItem);
        //}

        public JsonResult GetInvoicesAmountByCustNumber(string custNumber)
        {
            List<Core.Domain.Invoices.Invoice> ObjInv = GetPdfInvoicesByCustomerNumber(custNumber);
            // Create a List of PaymentInvoiceItemModel  
            List<PaymentInvoiceItemModel> InvItem = new List<PaymentInvoiceItemModel>();

            foreach (Core.Domain.Invoices.Invoice inv in ObjInv)
            {
                InvItem.Add(new PaymentInvoiceItemModel
                {
                    Number = inv.DocumentNumber,
                    Amount = inv.InvoiceType == "Credit Memo" ? -Convert.ToDecimal(inv.Balance) : Convert.ToDecimal(inv.Balance),
                    InvoiceType = inv.InvoiceType
                });
            }

            return Json(InvItem);
        }
        //public JsonResult CheckValidInvoices(string customer, string invoices)
        //{
        //    // this is only for guest customer validation

        //    //invoices = "23456,456345,4774138"; // for testing
        //    var invoiceArray = invoices.Split(",");

        //    // Convert string array to String List,
        //    // so we can remove item easly, if exist in system
        //    List<string> invoiceList = new List<string>();
        //    foreach (var item in invoiceArray)
        //    {
        //        if (item != "")
        //        {
        //            invoiceList.Add(item);
        //        }
        //    }

        //    string ret = "";
        //    var datasettings = DataSettingsManager.LoadSettings();
        //    var connstring = datasettings.ConnectionString;

        //    var conn = new SqlConnection(connstring);
        //    SqlDataAdapter da = new SqlDataAdapter();
        //    conn.Open();
        //    var cmd = new SqlCommand("select DocumentNumber from SapInvoices where CustomerNo = '"+ customer + "' and Balance > 0 AND Paid = 'N'", conn);
        //    cmd.CommandType = CommandType.Text;
        //    cmd.CommandTimeout = 0;
        //    try
        //    {
        //        da = new SqlDataAdapter(cmd);
        //        var dt = new DataTable();
        //        da.Fill(dt);

        //        if (dt.Rows.Count > 0)
        //        {
        //            foreach (DataRow r in dt.Rows)
        //            {
        //                var document = r["DocumentNumber"].ToString();
        //                if (invoiceList.Contains(document))
        //                {
        //                    // Remove valid invoices
        //                    // show just invalid invoices to view
        //                    invoiceList.Remove(document);
        //                }
        //            }
        //            // convert List of string to string Array
        //            //ret = string.Join("<br/>", invoiceList);
        //            ret = string.Join(", ", invoiceList);
        //        }
        //        else
        //        {
        //            ret = "No Document Number for this customer.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json("Error: " + ex.Message);
        //    }
        //    return Json(ret);
        //}

        public JsonResult CheckValidCustomer(string customer)
        {
            string ret = "";
            string Currency = "";
            var datasettings = DataSettingsManager.LoadSettings();
           // var connstring = datasettings.ConnectionString;
            var connstring = _configuration["Nop:HansamedConnectionistring"];
            var conn = new SqlConnection(connstring);

            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();
            var cmd = new SqlCommand("select CustomerNo,currency from SapInvoices where CustomerNo = '" + customer + "' and Balance > 0 AND Paid = 'N'", conn);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;
            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    Currency = (dt.Rows[0]["Currency"]).ToString();

                    if (Currency == "$")
                    {
                        var IsMonerisConfigure = GetSettingdetailsForMoneris("ForIsactive");
                        for (var i = 0; i < IsMonerisConfigure.Count; i++)
                        {
                            if (IsMonerisConfigure[i].ItemArray[1].ToString().Contains("FoxNetSoft.MonerisCheckout"))
                            {
                                ret = "MonerisIsNotActive";
                            }

                        }
                        if (ret != "MonerisIsNotActive")
                        {
                            ret = "ForMoneris";
                        }
                    }
                    else
                    {
                        ret = "YES";
                    }
                }
                else
                {
                    ret = "NO";
                }
            }
            catch (Exception ex)
            {
                return Json("Error: " + ex.Message);
            }
            return Json(ret);
        }

        public JsonResult GetCompanyName(string customer)
        {
            string ret = "";
            var datasettings = DataSettingsManager.LoadSettings();
            //var connstring = datasettings.ConnectionString;
            var connstring = _configuration["Nop:HansamedConnectionistring"];
            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();
            //var cmd = new SqlCommand("select top 1 CompanyName from SapInvoices where CustomerNo = '" + customer + "'", conn);
            var cmd = new SqlCommand("select top 1 CompanyName from SapInvoices where CustomerNo = '" + customer + "' Order by DocumentDate desc ", conn);

            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;
            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    ret = dt.Rows[0][0].ToString();
                }
                else
                {
                    ret = "";
                }
            }
            catch (Exception ex)
            {
                return Json("Error: " + ex.Message);
            }
            return Json(ret);
        }

        public IActionResult PaymentComplete()
        {
            return View();
        }

        [IgnoreAntiforgeryToken]
        public IActionResult ConfirmPayment(IFormCollection form)
        {
            // GET PAYMENT TRANSACTION BY CUSTOMER NUMBER, NOT EMAIL ADDRESS
            // AT THIS POINT
            try
            {

                string Message = "Customer Number:" + form["PaymentCustomerNumber"].ToString() + " Token:" + form["Token"] + " ";

                var emailAccount = _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId).Result;
                //var email = new QueuedEmail
                //{
                //    Priority = QueuedEmailPriority.High,
                //    EmailAccountId = emailAccount.Id,
                //    FromName = emailAccount.DisplayName,
                //    From = emailAccount.Email,
                //    Bcc = "h.woo@hansamed.net",
                //    ToName = "Kulvindertpss",
                //    To = "Kulvindertpss@gmail.com",
                //    Subject = "Testing PaymentInvoice:",
                //    Body = Message,
                //    CreatedOnUtc = DateTime.UtcNow,
                //};
                //_queuedEmailService.InsertQueuedEmailAsync(email);

                var processPaymentRequest = new ProcessPaymentRequest();

                processPaymentRequest.StoreId = _storeContext.GetCurrentStore().Id;
                //processPaymentRequest.CustomerId = _workContext.CurrentCustomer.Id;
                processPaymentRequest.CustomValues.Add("CustomerNumber", form["PaymentCustomerNumber"].ToString());
                processPaymentRequest.PaymentMethodSystemName = "Payments.Moneris";
                processPaymentRequest.OrderGuid = System.Guid.NewGuid();
                processPaymentRequest.OrderTotal = decimal.Parse(form["PaymentTotal"]);

                //Add the token required for Moneris to post
                processPaymentRequest.CustomValues.Add("Token", form["Token"]);

                string WebShopTransactionID = "";
                if (processPaymentRequest.OrderTotal > 0)
                {
                    // if Invoice Total > 0 then the Total will be remaining Total
                    processPaymentRequest.OrderTotal = decimal.Parse(form["PaymentTotal"]);

                    var paymentMethod = _paymentPluginManager.LoadPluginBySystemNameAsync("Payments.Moneris", _workContext.GetCurrentCustomerAsync().Result, _storeContext.GetCurrentStore().Id).Result;
                    var paymentresult = paymentMethod.ProcessPaymentAsync(processPaymentRequest).Result;
                     

                    // If Moneris is Decline, then show error to User
                    // By default assigned empty Error, so can handle on view as empty
                    HttpContext.Session.SetString("PaymentAmountError", string.Empty);
                    if (paymentresult.Success == false)
                    {
                        string paymentErr = "";
                        foreach (var paymentError in paymentresult.Errors)
                        {
                            paymentErr = paymentErr + ", " + paymentError;
                        }

                        var errFormat = string.Format(_localizationService.GetResourceAsync("Checkout.PaymentError").Result, paymentErr);
                        _logger.Error("Offline transaction: " + errFormat, null, _workContext.GetCurrentCustomerAsync().Result);

                        HttpContext.Session.SetString("PaymentAmountError", errFormat);

                        // Return as success even this is Fail, So can show this messege on
                        // PaymentComplete view, but as Moneris error, not sucess
                        //return Json(new { success = 0, message = errFormat });
                        return Json(new { error = 1, message = errFormat });
                    }
                }
                else
                {
                    // if Invoice Total <= 0 then Total will be 0
                    // because did not pay anything
                    processPaymentRequest.OrderTotal = 0;

                    // Generate Menoris Payment Response here
                    // In this Case, we are SKIP Moneris Payment Transaction
                    // this is will be added in PaymentInvoice Table

                    WebShopTransactionID = DateTime.Now.Ticks.ToString();

                    XElement manualXML =
                        new XElement("response",
                        new XElement("receipt",
                        new XElement("DataKey", ""),
                        new XElement("ReceiptId", WebShopTransactionID),
                        new XElement("ReferenceNum", WebShopTransactionID),
                        new XElement("ResponseCode", ""),
                        new XElement("ISO", ""),
                        new XElement("AuthCode", ""),
                        new XElement("Message", "SKIP Moneris Payment Transaction, generating XML Manually."),
                        new XElement("TransTime", ""),
                        new XElement("TransDate", ""),
                        new XElement("TransType", ""),
                        new XElement("Complete", "false"),
                        new XElement("TransAmount", 0),  // Total amount, If customer pay by Crdit instead Visa Card
                        new XElement("CardType", ""),
                        new XElement("TransID", ""),
                        new XElement("TimedOut", "false"),
                        new XElement("CorporateCard", "false"),
                        new XElement("RecurSuccess", null),
                        new XElement("AvsResultCode", null),
                        new XElement("CvdResultCode", ""),
                        new XElement("ResSuccess", "true"),
                        new XElement("PaymentType", ""),
                        new XElement("IsVisaDebit", "false"),
                        // Nested
                        new XElement("ResolveData",
                        new XElement("cust_id", ""),
                        new XElement("phone", ""),
                        new XElement("email", ""),
                        new XElement("note", ""),
                        new XElement("expdate", ""),
                        new XElement("masked_pan", "")
                      )));
                    processPaymentRequest.CustomValues.Add("PaymentResponse", manualXML);
                }

                var paymentresponse = processPaymentRequest.CustomValues["PaymentResponse"].ToString();
                var respmodel = new PaymentReponse();
                respmodel.Amount = processPaymentRequest.OrderTotal;

                var xmlresponse = new XmlDocument();
                xmlresponse.LoadXml(paymentresponse);

                XmlNodeList xmlNodeList = xmlresponse.SelectNodes("/response/receipt/ReferenceNum");
                if (xmlNodeList.Count > 0)
                {
                    respmodel.TransactionId = xmlNodeList[0].InnerText;
                }

                // Storing in Session, Then showing these valies in PaymentComplete View
                HttpContext.Session.SetString("PaymentAmount", form["PaymentTotal"]);
                HttpContext.Session.SetString("PaymentTranId", respmodel.TransactionId);

                string jsontraninfo = form["PaymentTranInfo"];

                // if Total <= 0 then update the jsontraninfo
                // Update AmountPaid = 0
                // Sum of all Amount in detail where InvoiceType= Invoice Number
                // update the Credit Amount to Sum of All amount in detail where InvoiceType= Invoice Number
                if (processPaymentRequest.OrderTotal <= 0)
                {
                    jsontraninfo = updateJsonPost(jsontraninfo);
                }

                //Post the data to the database
                var datasettings = DataSettingsManager.LoadSettings();
                //var connstring = datasettings.ConnectionString;
                var connstring = _configuration["Nop:HansamedConnectionistring"];
                //Get a connection, easier (and faster) than trying to use the EF methods for this simple execute
                var conn = new SqlConnection(connstring);
                conn.Open();

                var cmdNewUser = new SqlCommand
                {
                    Connection = conn,
                    CommandType = CommandType.StoredProcedure,
                    CommandText = "dbo.spAddPaymentInvoice"
                };

                cmdNewUser.Parameters.AddWithValue("@PaymentInfo", jsontraninfo); // this is saving as Json, this response send to Payment SAP API
                cmdNewUser.Parameters.AddWithValue("@PayResult", paymentresponse); // Generated from Meoris Payment Response
                cmdNewUser.Parameters.Add("@RetId", SqlDbType.Int);
                cmdNewUser.Parameters["@RetId"].Direction = ParameterDirection.Output;

                cmdNewUser.ExecuteScalar();
                int PaymentId = Convert.ToInt32(cmdNewUser.Parameters["@RetId"].Value);
                conn.Close();

                PaymentInvoiceModel payment = JsonConvert.DeserializeObject<PaymentInvoiceModel>(jsontraninfo);
                payment.PaymentId = PaymentId; // add PaymentId in Object

                // *************** Update SapInvoices table
                // Update Balance Columns in SapInvoices and SapCreditMemos Table
                // Call SP GetOfflineTransaction_ApplyPayment

                // get List of all invoices by Customre Number. 
                // doesn't matter, if Guest Customer OR Login Customer
                // to get the existing Balance for each invoice

                List<Invoice> invoices = new List<Invoice>();
                var CustNumber = form["PaymentCustomerNumber"].ToString();
                invoices = GetPdfInvoicesByCustomerNumber(CustNumber);

                // if there is no invoice then dont update the balance in SAPInvoice Table
                // SKIP the following Step

                if (invoices.Count > 0)
                {
                    foreach (PaymentInvoiceItemModel item in payment.InvoiceDetails)
                    {
                        var conn2 = new SqlConnection(connstring);
                        conn2.Open();
                        var cmd = new SqlCommand
                        {
                            Connection = conn2,
                            CommandType = CommandType.StoredProcedure,
                            CommandText = "dbo.GetOfflineTransaction_ApplyPayment"
                        };

                        cmd.Parameters.AddWithValue("@InvNumber", item.Number);

                        // To get the Balance from each Invoice by Invoice Number
                        Invoice objInvoice = invoices.Where(i => i.DocumentNumber == item.Number).FirstOrDefault();

                        decimal existingBalance = Convert.ToDecimal(objInvoice.Balance);

                        // Current Balance need to update in Balance Column 
                        // in both table SAPInvoices, SAPCreditMemos
                        decimal currentBalance;

                        // if value is -ve like Credit Memo
                        // in PaymentInvoiceDetail table the Amount is -ve in case of CREDIT MEMO
                        if (item.Amount < 0)
                        {
                            currentBalance = existingBalance + item.Amount;
                        }
                        else
                        {
                            currentBalance = existingBalance - item.Amount;
                        }

                        // in both table SAPInvoices, SAPCreditMemos balance will be +ve
                        // dont need to put any condition
                        cmd.Parameters.AddWithValue("@Payment", currentBalance);
                        cmd.Parameters.AddWithValue("@InvoiceType", item.InvoiceType);

                        var fullAmount = "Y";

                        if (currentBalance > 0)
                        {
                            fullAmount = "N";
                        }

                        if (item.InvoiceType == "Credit Memo")
                        {
                            // in XML Post, Credit Memo Amount need to keep positive in case of Credit Memo
                            item.Amount = item.Amount * -1;
                        }

                        cmd.Parameters.AddWithValue("@FullAmount", fullAmount);
                        var retval2 = cmd.ExecuteScalar();
                        conn2.Close();
                    }
                    // *************** Update SapInvoices table
                }

                // in case of Customer Pay without login / by link then, 
                // they meed to entr First Name and Last Name, 
                // otherwise, it will be Empty
                string custname = payment.FirstName + " " + payment.LastName;

                //Send the email message
                // For Testing
                //payment.Email = "r.khan@hansamed.net";

                SendCompletionEmail(payment.Email, custname, form["PaymentCustomerTag"], form["PaymentInvoiceTag"], respmodel.TransactionId);

                // assign Transaction ID
                payment.WebPortalTransactionId = respmodel.TransactionId;

                // Get Credit Card
                var CreditCard = "";
                XmlNodeList xmlNodeCreditCardList = xmlresponse.SelectNodes("/response/receipt/ResolveData/masked_pan");
                if (xmlNodeCreditCardList.Count > 0)
                {
                    if (xmlNodeCreditCardList[0].InnerText.Length > 0)
                    {
                        CreditCard = xmlNodeCreditCardList[0].InnerText.Substring(xmlNodeCreditCardList[0].InnerText.Length - 4);
                    }
                    else
                    {
                        CreditCard = "";
                    }
                }

                // Update PaymentPostXml
                string xmlPaymentPost = GeneratePaymentPostXML(payment, CreditCard);

                // Update PaymentInvoice table with XML Payment Post and TransactionID
                if (processPaymentRequest.OrderTotal > 0) // then Payment through Moneris
                {
                    UpdatePaymentPostXml(payment.PaymentId, xmlPaymentPost, payment.WebPortalTransactionId, true);
                }
                else // then Payment through Customer Credit
                {
                    UpdatePaymentPostXml(payment.PaymentId, xmlPaymentPost, payment.WebPortalTransactionId, false);
                }

                //raise event
                _eventPublisher.Publish(new PaymentPlacedEvent(payment));

                //success
                return Json(new
                {
                    success = 1,
                    transactionid = respmodel.TransactionId,
                    amount = respmodel.Amount
                });


                //if (processPaymentRequest.OrderTotal > 0)
                //{
                //    return Json(new
                //    {
                //        success = 1,
                //        transactionid = respmodel.TransactionId,
                //        amount = respmodel.Amount
                //    });
                //}
                //else
                //{
                //    return Json(new
                //    {
                //        success = 1,
                //        transactionid = respmodel.TransactionId,
                //        amount = respmodel.Amount
                //    });
                //}
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.GetCurrentCustomerAsync().Result);
                return Json(new { error = 1, message = exc.Message });
            }
        }
        private void SendCompletionEmail(string email, string emailname, string customerTag, string invoiceTag, string transactionid)
        {
            // var store = _storeContext.CurrentStore;
            // languageId = EnsureLanguageIsActive(languageId, store.Id);

            //var settings = _settingService.LoadSetting<SAPOrderSettings>();
            //var emailtoaddresses = settings.ErrorEmailAddress.Split(",");

            var messageTemplates = _messageTemplateService.GetMessageTemplatesByNameAsync(ErrorMailTemplate, 1).Result;
            var store = _storeService.GetStoreByIdAsync(DefaultStoreId).Result;

            if (messageTemplates.Count == 0)
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Error, "Template named '" + ErrorMailTemplate + "' not found", "Post error warning email not sent");
                return;
            }

            foreach (var template in messageTemplates)
            {
                var commonTokens = new List<Token>();
                var emailAccount = _emailAccountService.GetEmailAccountByIdAsync(template.EmailAccountId).Result;
                _messageTokenProvider.AddStoreTokensAsync(commonTokens, store, emailAccount);
                commonTokens.Add(new Token("PaymentInvoice.Customer", customerTag, true));
                commonTokens.Add(new Token("PaymentInvoice.Invoice", invoiceTag, true));
                commonTokens.Add(new Token("PaymentInvoice.TransactionId", transactionid));

                _workflowMessageService.SendNotificationAsync(template, emailAccount, 1, commonTokens, email.Trim(), emailname);

            }
        }
        #region offLineTransactions
        //private List<Core.Domain.Invoices.Invoice> GetPdfInvoices(string email)
        //{
        //    // Only for Login Customer
        //    List<Core.Domain.Invoices.Invoice> ObjInvoices = new List<Core.Domain.Invoices.Invoice>();

        //    if (string.IsNullOrEmpty(email)) return ObjInvoices;

        //    var datasettings = DataSettingsManager.LoadSettings();
        //    var connstring = datasettings.ConnectionString;

        //    var conn = new SqlConnection(connstring);
        //    SqlDataAdapter da = new SqlDataAdapter();
        //    conn.Open();
        //    var cmd = new SqlCommand("select * from ViewOfflineTransactionGetInvoice where (APEmail ='" + email + "' OR ContactEmail ='"+ email + "') AND Paid = 'N'", conn);

        //    cmd.CommandType = CommandType.Text;
        //    cmd.CommandTimeout = 0;

        //    try
        //    {
        //        da = new SqlDataAdapter(cmd);
        //        var dt = new DataTable();
        //        da.Fill(dt);

        //        ObjInvoices = (from DataRow dr in dt.Rows
        //        select new Core.Domain.Invoices.Invoice
        //        {
        //            DocEntry = dr["DocEntry"].ToString(),
        //            DocumentNumber = dr["DocumentNumber"].ToString(),
        //            DocumentDate = dr["DocumentDate"].ToString(),
        //            DueDate = dr["DueDate"].ToString(),
        //            CustomerNo = dr["CustomerNo"].ToString(),
        //            ReferencePoNo = dr["ReferencePoNo"].ToString(),
        //            AccountManager = dr["AccountManager"].ToString(),
        //            ServiceContractNo = dr["ServiceContractNo"].ToString(),
        //            BillToName = dr["BillToName"].ToString(),
        //            BillToAddress = dr["BillToAddress"].ToString(),
        //            ShipToName = dr["ShipToName"].ToString(),
        //            ShipToAddress = dr["ShipToAddress"].ToString(),
        //            ShippingType = dr["ShippingType"].ToString(),
        //            FreightCharge = Convert.ToDouble(dr["FreightCharge"]).ToString("0.##"),
        //            TotalTaxAmount = Convert.ToDouble(dr["TotalTaxAmount"]).ToString("0.##"),
        //            TotalAfterDownPayment = Convert.ToDouble(dr["TotalAfterDownPayment"]).ToString("0.##"),
        //            Downpayment = Convert.ToDouble(dr["Downpayment"]).ToString("0.##"),
        //            Currency = dr["Currency"].ToString(),
        //            APEmail = dr["APEmail"].ToString(),
        //            DescriptionFreight = dr["DescriptionFreight"].ToString(),
        //            NetFreight = Convert.ToDouble(dr["NetFreight"]).ToString("0.##"),
        //            TaxCodeFreight = dr["TaxCodeFreight"].ToString(),
        //            TaxPrcFreight = Convert.ToDouble(dr["TaxPrcFreight"]).ToString("0.##"),
        //            TaxFreight = Convert.ToDouble(dr["TaxFreight"]).ToString("0.##"),
        //            GrossFreight = Convert.ToDouble(dr["GrossFreight"]).ToString("0.##"),
        //            Paid = dr["Paid"].ToString(),
        //            Balance = Convert.ToDouble(dr["Balance"]).ToString("0.##"),

        //            OrderId = dr["OrderId"].ToString(),
        //            DocStatus = dr["DocStatus"].ToString(),
        //            CreateDate = dr["CreateDate"].ToString(),
        //            UpdateDate = dr["UpdateDate"].ToString(),
        //            InvoiceType = dr["InvoiceType"].ToString(),

        //    }).ToList();

        //        return ObjInvoices;
        //    }
        //    catch (Exception)
        //    {
        //        return new List<Core.Domain.Invoices.Invoice>();
        //    }
        //    finally
        //    {
        //        da.Dispose();
        //        conn.Close();
        //    }
        //}

        private List<Core.Domain.Invoices.Invoice> GetPdfInvoicesByCustomerNumber(string CustNumber)
        {
            List<Core.Domain.Invoices.Invoice> ObjInvoices = new List<Core.Domain.Invoices.Invoice>();

            if (string.IsNullOrEmpty(CustNumber))
                return ObjInvoices;

            var datasettings = DataSettingsManager.LoadSettings();
           var connstring =   _configuration["Nop:HansamedConnectionistring"];
            //var connstring = datasettings.ConnectionString;

            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();
            var cmd = new SqlCommand("select * from ViewOfflineTransactionGetInvoice where CustomerNo='" + CustNumber + "' AND Paid = 'N'", conn);

            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;

            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                ObjInvoices = (from DataRow dr in dt.Rows
                               select new Core.Domain.Invoices.Invoice
                               {
                                   DocEntry = dr["DocEntry"].ToString(),
                                   DocumentNumber = dr["DocumentNumber"].ToString(),
                                   DocumentDate = dr["DocumentDate"].ToString(),
                                   DueDate = dr["DueDate"].ToString(),
                                   CustomerNo = dr["CustomerNo"].ToString(),
                                   ReferencePoNo = dr["ReferencePoNo"].ToString(),
                                   AccountManager = dr["AccountManager"].ToString(),
                                   ServiceContractNo = dr["ServiceContractNo"].ToString(),
                                   BillToName = dr["BillToName"].ToString(),
                                   BillToAddress = dr["BillToAddress"].ToString(),
                                   ShipToName = dr["ShipToName"].ToString(),
                                   ShipToAddress = dr["ShipToAddress"].ToString(),
                                   ShippingType = dr["ShippingType"].ToString(),
                                   FreightCharge = Convert.ToDouble(dr["FreightCharge"]).ToString("0.##"),
                                   TotalTaxAmount = Convert.ToDouble(dr["TotalTaxAmount"]).ToString("0.##"),
                                   TotalAfterDownPayment = Convert.ToDouble(dr["TotalAfterDownPayment"]).ToString("0.##"),
                                   Downpayment = Convert.ToDouble(dr["Downpayment"]).ToString("0.##"),
                                   Currency = dr["Currency"].ToString(),
                                   APEmail = dr["APEmail"].ToString(),
                                   DescriptionFreight = dr["DescriptionFreight"].ToString(),
                                   NetFreight = Convert.ToDouble(dr["NetFreight"]).ToString("0.##"),
                                   TaxCodeFreight = dr["TaxCodeFreight"].ToString(),
                                   TaxPrcFreight = Convert.ToDouble(dr["TaxPrcFreight"]).ToString("0.##"),
                                   TaxFreight = Convert.ToDouble(dr["TaxFreight"]).ToString("0.##"),
                                   GrossFreight = Convert.ToDouble(dr["GrossFreight"]).ToString("0.##"),
                                   Paid = dr["Paid"].ToString(),
                                   Balance = Convert.ToDouble(dr["Balance"]).ToString("0.##"),

                                   OrderId = dr["OrderId"].ToString(),
                                   DocStatus = dr["DocStatus"].ToString(),
                                   CreateDate = dr["CreateDate"].ToString(),
                                   UpdateDate = dr["UpdateDate"].ToString(),
                                   InvoiceType = dr["InvoiceType"].ToString(),
                               }).ToList();
                return ObjInvoices;
            }
            catch (Exception)
            {
                return new List<Core.Domain.Invoices.Invoice>();
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }
        #endregion
        private string GeneratePaymentPostXML(PaymentInvoiceModel PostPayment, string creditCard)
        {
            // *********** Generate XML for Payment API

            XmlDocument doc = new XmlDocument();
            //the xml declaration is recommended, but not mandatory
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            //Root Element NewPayment, string.Empty makes cleaner code
            XmlElement NewPaymentElement = doc.CreateElement(string.Empty, "NewPayment", string.Empty);
            XmlAttribute newPaymentVersionCode = doc.CreateAttribute("version-code");
            newPaymentVersionCode.Value = "2";
            NewPaymentElement.Attributes.Append(newPaymentVersionCode);
            doc.AppendChild(NewPaymentElement);

            // 1st Element AmountPaid
            XmlElement AmountPaid = doc.CreateElement(string.Empty, "AmountPaid", string.Empty);
            XmlText textAmountPaid = doc.CreateTextNode(PostPayment.TotalAmount.ToString());
            AmountPaid.AppendChild(textAmountPaid);
            NewPaymentElement.AppendChild(AmountPaid);

            // 2nd Element WebPortalTransactionId
            XmlElement WebPortalTransactionId = doc.CreateElement(string.Empty, "WebPortalTransactionId", string.Empty);
            XmlText textWebPortalTransactionIdd = doc.CreateTextNode(PostPayment.WebPortalTransactionId);
            WebPortalTransactionId.AppendChild(textWebPortalTransactionIdd);
            NewPaymentElement.AppendChild(WebPortalTransactionId);

            // 3rd Element PaymentCreated
            XmlElement PaymentCreated = doc.CreateElement(string.Empty, "PaymentCreated", string.Empty);
            XmlText textPaymentCreated = doc.CreateTextNode(DateTime.Now.ToString());
            PaymentCreated.AppendChild(textPaymentCreated);
            NewPaymentElement.AppendChild(PaymentCreated);

            //****** Element AdditionalFields
            //<AdditionalFields>
            //<AdditionalField type = "STRING" id = "card-num" > 5454545454545454 </ AdditionalField>
            //</AdditionalFields>

            XmlElement AdditionalFields = doc.CreateElement(string.Empty, "AdditionalFields", string.Empty);
            XmlElement AdditionalField = doc.CreateElement(string.Empty, "AdditionalField", string.Empty);

            //****** 4th Element card-num

            XmlAttribute TextType = doc.CreateAttribute("type");
            TextType.Value = "STRING";

            XmlAttribute TextId = doc.CreateAttribute("id");
            TextId.Value = "card-num";

            XmlText textVisaCard = doc.CreateTextNode(creditCard);
            AdditionalField.AppendChild(textVisaCard);

            AdditionalField.Attributes.Append(TextType);
            AdditionalField.Attributes.Append(TextId);

            AdditionalFields.AppendChild(AdditionalField);

            //NewPaymentElement.AppendChild(AdditionalFields);
            //****** 4th Element card-num

            //****** 5th Element card-type
            
            //XmlElement AdditionalFields2 = doc.CreateElement(string.Empty, "AdditionalFields", string.Empty);
            XmlElement AdditionalField2 = doc.CreateElement(string.Empty, "AdditionalField", string.Empty);

            XmlAttribute TextType2 = doc.CreateAttribute("type");
            TextType2.Value = "STRING";

            XmlAttribute TextId2 = doc.CreateAttribute("id");
            TextId2.Value = "card-type";

            XmlText textCardTyped = doc.CreateTextNode("3");
            AdditionalField2.AppendChild(textCardTyped);

            AdditionalField2.Attributes.Append(TextType2);
            AdditionalField2.Attributes.Append(TextId2);

            AdditionalFields.AppendChild(AdditionalField2);

            //****** 5th Element card-type

            //********* Adding 4th and 5th Element in Main AdditionalFields Node
            NewPaymentElement.AppendChild(AdditionalFields);

            //****** Element AdditionalFields

            foreach (PaymentInvoiceItemModel item in PostPayment.InvoiceDetails)
            {
                // Nested Root Node
                XmlElement rowNestedElement = doc.CreateElement(string.Empty, "row", string.Empty);
                NewPaymentElement.AppendChild(rowNestedElement);

                // ******** Nested DocumentType

                var documentType = "";
                if (item.InvoiceType == "Invoice Number")
                {
                    documentType = "INV";
                }
                else
                {
                    documentType = "CREDIT";
                }
                XmlElement DocumentType = doc.CreateElement(string.Empty, "DocumentType", string.Empty);
                XmlText textDocumentType = doc.CreateTextNode(documentType);
                DocumentType.AppendChild(textDocumentType);
                rowNestedElement.AppendChild(DocumentType);

                // Nested Document Number
                XmlElement DocumentNumber = doc.CreateElement(string.Empty, "DocumentNumber", string.Empty);
                XmlText textDocumentNumber = doc.CreateTextNode(item.Number);
                DocumentNumber.AppendChild(textDocumentNumber);
                rowNestedElement.AppendChild(DocumentNumber);

                // Nested Applied Amount
                XmlElement AppliedAmount = doc.CreateElement(string.Empty, "AppliedAmount", string.Empty);
                XmlText textAppliedAmount = doc.CreateTextNode(item.Amount.ToString());
                AppliedAmount.AppendChild(textAppliedAmount);
                rowNestedElement.AppendChild(AppliedAmount);
            }

            return doc.InnerXml;
        }
        private void UpdatePaymentPostXml(int PaymentId, string PaymentPostXML, string TransactionId, bool isMonerisTransactionId)
        {
            var datasettings = DataSettingsManager.LoadSettings();
 var connstring = _configuration["Nop:HansamedConnectionistring"];
           // var connstring = datasettings.ConnectionString;

            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();

            try
            {
                conn.Open();
                SqlCommand cmd;

                // if is Moneris TransactionId then update TransactionId_Moneris column
                // otherwise Update TransactionId_WebShop column
                if (isMonerisTransactionId)
                {
                    cmd = new SqlCommand("Update PaymentInvoice SET SAPPaymentPost ='" + PaymentPostXML + "', TransactionId_Moneris='" + TransactionId + "' where Id = " + PaymentId + "", conn);

                }
                else
                {
                    cmd = new SqlCommand("Update PaymentInvoice SET SAPPaymentPost ='" + PaymentPostXML + "', TransactionId_WebShop='" + TransactionId + "' where Id = " + PaymentId + "", conn);
                }

                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;
                var retval = cmd.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                _logger.Error(exc.Message, exc, _workContext.GetCurrentCustomerAsync().Result);
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }
        private string GetCutomerNameByEmail(string email)
        {
            DataTable dt = new DataTable();
            if (string.IsNullOrEmpty(email))
                return ",";

            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = _configuration["Nop:HansamedConnectionistring"];
           // var connstring = datasettings.ConnectionString;
            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();
            var cmd = new SqlCommand("GetCustomerNameByEmail", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 0;
            cmd.Parameters.AddWithValue("@Email", email);
            try
            {
                da = new SqlDataAdapter(cmd);
                dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    var firstName = dt.Rows[0]["FirstName"].ToString();
                    var lastName = dt.Rows[0]["LastName"].ToString();

                    return firstName + "," + lastName;
                }
                else
                {
                    return ",";
                }
            }
            catch (Exception)
            {
                return ",";
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }
        private string updateJsonPost(string jsontraninfo)
        {
            //if (databaseUpdate)
            //{
            PaymentInvoiceModel updateJson = JsonConvert.DeserializeObject<PaymentInvoiceModel>(jsontraninfo);
            updateJson.TotalAmount = 0;

            List<PaymentInvoiceItemModel> objInvDetail = updateJson.InvoiceDetails;

            decimal totalCreadit = 0;
            foreach (PaymentInvoiceItemModel item in objInvDetail.Where(a => a.InvoiceType == "Invoice Number"))
            {
                totalCreadit = totalCreadit + item.Amount;
            }

            foreach (PaymentInvoiceItemModel item in objInvDetail)
            {
                if (item.InvoiceType == "Credit Memo")
                {
                    item.Amount = -totalCreadit;
                }
            }

            jsontraninfo = JsonConvert.SerializeObject(updateJson);
            TempData["InvoicePaidAmount"] = totalCreadit;
            return jsontraninfo;

            // }
            //else
            //{
            //    // Save as XML with +ve Amount of Credit Memo
            //    PaymentInvoiceModel updateJson = JsonConvert.DeserializeObject<PaymentInvoiceModel>(jsontraninfo);
            //    List<PaymentInvoiceItemModel> objInvDetail = updateJson.InvoiceDetails;

            //    foreach (PaymentInvoiceItemModel item in objInvDetail)
            //    {
            //        if (item.InvoiceType == "Credit Memo")
            //        {
            //            item.Amount = item.Amount;
            //        }
            //    }
            //}


        }
        private List<SelectListItem> GetCustomersByEmail(string email)
        {
            // Only for Login Customer
            List<SelectListItem> ObjCustomrs = new List<SelectListItem>();

            if (string.IsNullOrEmpty(email))
                return ObjCustomrs;

            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = _configuration["Nop:HansamedConnectionistring"];
           // var connstring = datasettings.ConnectionString;

            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();
            var cmd = new SqlCommand("select distinct CustomerNo from ViewOfflineTransactionGetInvoice where (APEmail ='" + email + "' OR ContactEmail ='" + email + "') AND Paid = 'N' AND Balance > 0 ", conn);

            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;

            try
            {
                da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    ObjCustomrs.Add(new SelectListItem()
                    {
                        Text = row["CustomerNo"].ToString(),
                        Value = row["CustomerNo"].ToString()
                    });
                }

                return ObjCustomrs;
            }
            catch (Exception)
            {
                return new List<SelectListItem>();
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }
        }
        //code by TPSS

        public IActionResult PaymentGate(string hdnvalue, string CustomerNo)
        {
            var model = new MainResponse();
            // string Qa_CheckoputUrl = "https://gateway.moneris.com/chkt/request/request.php";
            string Qa_CheckoputUrl = "";
            //string Store_id = "store3";
            //string Api_token = "yesguy";
            //string Checkout_id = "chkt23NGFtore3";
            //string Environment = "qa";
            string Store_id = "";
            string Api_token = "";
            string Checkout_id = "";
            string Environment = "";
            string txn_total = hdnvalue;
            var sandbox = false;
            var data = GetSettingdetailsForMoneris("ForMonerisDetails");
            Response obj = new Response();
            //for (var i = 0; i < data.Count; i++)
            //{
            //    if (Convert.ToString(data[i].ItemArray[0]) == "monerischeckoutsettings.storeid")
            //    {
            //        //Store_id = Convert.ToString(data[i].ItemArray[1]);
            //        obj.StoreId = Convert.ToString(data[i].ItemArray[1]);
            //    }
            //    if (Convert.ToString(data[i].ItemArray[0]) == "monerischeckoutsettings.apitoken")
            //    {
            //        //Api_token = Convert.ToString(data[i].ItemArray[1]);
            //        obj.ApiToken = Convert.ToString(data[i].ItemArray[1]);
            //    }
            //    //if (Convert.ToString(data[i].ItemArray[0]) == "monerischeckoutsettings.checkoutid")
            //    //{
            //    //    Checkout_id = Convert.ToString(data[i].ItemArray[1]);

            //    //  Checkout_id =   _configuration.GetSection("Nop")["Nop"];

            //    //}
            //    if (Convert.ToString(data[i].ItemArray[0]) == "monerischeckoutsettings.usesandbox")
            //    {
            //        //Environment = Convert.ToString(data[i].ItemArray[1]) == "True" ? "qa" : "prod";
            //        obj.Sandbox = Convert.ToString(data[i].ItemArray[1]) == "True" ? "qa" : "prod";
            //        Qa_CheckoputUrl = obj.Sandbox == "prod" ? "https://gateway.moneris.com/chkt/request/request.php" : "https://gatewayt.moneris.com/chkt/request/request.php";
            //        sandbox = Convert.ToBoolean(data[i].ItemArray[1]);
            //    }
            //}
            obj.StoreId = _configuration["Nop:MonerisStoreID"];
            obj.ApiToken = _configuration["Nop:MonerisApiToken"];
            obj.Sandbox = "prod";
            Qa_CheckoputUrl = "https://gateway.moneris.com/chkt/request/request.php";
            sandbox = false;
            Checkout_id = _configuration["Nop:MonerisConfiguration"];
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(Qa_CheckoputUrl);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                // prod
                //string json = "{\"store_id\":\"gwca035736\",\"api_token\":\"r49dsrVDVhH26MW7gF6A\",\"checkout_id\":\"chkt8X4TZ35736\",\"txn_total\":\"2.00\",\"environment\":\"prod\",\"action\":\"preload\"}";

                //qa
                string json = "{\"store_id\":\"" + obj.StoreId + "\",\"api_token\":\"" + obj.ApiToken + "\",\"checkout_id\":\"" + Checkout_id + "\",\"txn_total\":\"" + txn_total + "\",\"environment\":\"" + obj.Sandbox + "\",\"action\":\"preload\"}";
                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                model = MainResponse.FromJson(result);
                model.Response.ApiToken = obj.ApiToken;
                model.Response.StoreId = obj.StoreId;
                model.Response.Sandbox = obj.Sandbox;
                model.Response.UseSandbox = sandbox;
            }
            return PartialView("PaymentGate", model);
        }
        [IgnoreAntiforgeryToken]
        public IActionResult PaymentReceipt(string ticketId, string CustomerNo, string TotalAmount, string PaymentTranInfo, string PaymentInvoiceTag, string PaymentCustomerTag, string storeid, string apitoken, string usesandbox)
        {
            var result = string.Empty;
            string Qa_CheckoputUrl = "";
            //string Store_id = "store3";
            //string Api_token = "yesguy";
            //string Checkout_id = "chkt23NGFtore3";
            //string Environment = "qa";
            //
            string Store_id = "";
            string Api_token = "";
            string Checkout_id = "";
            string Environment = "";

            //var data = GetSettingdetailsForMoneris();
            //for (var i = 0; i < data.Count; i++)
            //{
            //    if (Convert.ToString(data[i].ItemArray[0]) == "monerischeckoutsettings.storeid")
            //    {
            //        Store_id = Convert.ToString(data[i].ItemArray[1]);
            //    }
            //    if (Convert.ToString(data[i].ItemArray[0]) == "monerischeckoutsettings.apitoken")
            //    {
            //        Api_token = Convert.ToString(data[i].ItemArray[1]);
            //    }
            //    if (Convert.ToString(data[i].ItemArray[0]) == "monerischeckoutsettings.checkoutid")
            //    {
            //        Checkout_id = Convert.ToString(data[i].ItemArray[1]);
            //    }
            //    if (Convert.ToString(data[i].ItemArray[0]) == "monerischeckoutsettings.usesandbox")
            //    {
            //        Environment = Convert.ToString(data[i].ItemArray[1])=="True"?"qa": "prod";
            //    }
            //}
            Checkout_id = _configuration["Nop:MonerisConfiguration"];
            Qa_CheckoputUrl = usesandbox == "prod" ? "https://gateway.moneris.com/chkt/request/request.php" : "https://gatewayt.moneris.com/chkt/request/request.php";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(Qa_CheckoputUrl);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                 string json = "{\"store_id\":\"" + storeid + "\",\"api_token\":\"" + apitoken + "\",\"checkout_id\":\"" + Checkout_id + "\",\"ticket\":\"" + ticketId + "\",\"environment\":\"" + usesandbox + "\",\"action\":\"receipt\"}";
                //string json = "{\"store_id\":\"" + storeid + "\",\"api_token\":\"" + apitoken + "\",\"checkout_id\":\"" + Checkout_id + "\",\"ticket\":\"" + ticketId + "\",\"environment\":\"" + usesandbox + "\",\"action\":\"receipt\",\"browser_ip_address\":\"" + browserIpAddress + "\",\"browser_screen_height\":\"" + browserScreenHeight + "\",\"browser_screen_width\":\"" + browserScreenWidth + "\",\"cardholder_name\":\"" + cardholderName + "\",\"cardholder_phone\":\"" + cardholderPhone + "\",\"cardholder_email\":\"" + cardholderEmail + "\"}";
                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            // Convert Json to XMl
            XNode node = JsonConvert.DeserializeXNode(result);
            var response= ConfirmPaymentforMoneris(ticketId, CustomerNo, TotalAmount, PaymentTranInfo, PaymentInvoiceTag, PaymentCustomerTag, node);
            

            if (response == false)
            {
                return Json(new { response });
            }
            else
            {
                return Json(new { response = result });
            }
        }

        public DataRowCollection GetSettingdetailsForMoneris(string value)
        {
            string Currency = null;
            Boolean Currencycode = false;
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = _configuration["Nop:HansamedConnectionistring"];
           // var connstring = datasettings.ConnectionString;
            var dt = new DataTable();

            var conn = new SqlConnection(connstring);
            if (value == "ForMonerisDetails")
            {
                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();
                var cmd = new SqlCommand("SELECT Name,Value FROM Setting where Name like '%monerischeckoutsettings%'", conn);
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;

                da = new SqlDataAdapter(cmd);
                dt = new DataTable();
                da.Fill(dt);

                ///if (dt.Rows.Count > 0)
                {
                    var result = dt.Rows;
                    //Currency = (dt.Rows[0]["Currency"]).ToString();
                    //if (Currency == "$")
                    //{
                    //    Currencycode = true;
                    //}
                    return result;
                }
            }
            else if (value == "ForIsactive")
            {
                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();
                var cmd = new SqlCommand("SELECT Name,Value FROM Setting where Name like '%paymentsettings.activepaymentmethodsystemnames%'", conn);
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;

                da = new SqlDataAdapter(cmd);
                dt = new DataTable();
                da.Fill(dt);

                ///if (dt.Rows.Count > 0)
                {
                    var result = dt.Rows;
                    //Currency = (dt.Rows[0]["Currency"]).ToString();
                    //if (Currency == "$")
                    //{
                    //    Currencycode = true;
                    //}
                    return result;
                }


            }
            return null;

        }

        public string GetCurrencyForPaymentInvoice(string CustomerNo)
        {
            string Currency = null;
            string Currencycode = "false";



            var datasettings = DataSettingsManager.LoadSettings();
            //var connstring = datasettings.ConnectionString;
            var connstring = _configuration["Nop:HansamedConnectionistring"];
            var dt = new DataTable();

            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();
            var cmd = new SqlCommand("SELECT Currency FROM SapInvoices where CustomerNo ='" + CustomerNo + "'", conn);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;
            try
            {
                da = new SqlDataAdapter(cmd);
                dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    Currency = (dt.Rows[0]["Currency"]).ToString();
                    if (Currency == "$")
                    {
                        var IsMonerisConfigure = GetSettingdetailsForMoneris("ForIsactive");
                        for (var i = 0; i < IsMonerisConfigure.Count; i++)
                        {
                            if (!IsMonerisConfigure[i].ItemArray[1].ToString().Contains("FoxNetSoft.MonerisCheckout"))
                            {
                                Currencycode = "MonerisIsNotActive";
                            }

                        }
                        if (Currencycode != "MonerisIsNotActive")
                        {
                            Currencycode = "true";
                        }
                    }
                }
                return Currencycode;
            }

            catch (Exception ex)
            {
                return "false";
            }

            return Currencycode;
        }

        public class APIresponse
        {
            public bool Paymentsuccess { get; set; }
            public string Paymenttransactionid { get; set; }
            public decimal Paymentamount { get; set; }
        }

        public bool ConfirmPaymentforMoneris(string ticketId, string CustomerNo, string TotalAmount, string PaymentTranInfo, string PaymentInvoiceTag, string PaymentCustomerTag, XNode resultjson)
        {
            // GET PAYMENT TRANSACTION BY CUSTOMER NUMBER, NOT EMAIL ADDRESS
            // AT THIS POINT
            APIresponse model = new APIresponse();
            model.Paymentsuccess = false;
            model.Paymenttransactionid = null;
            model.Paymentamount = 0;
            try
            {
                string Message = "Customer Number:" + CustomerNo + " Token:" + ticketId + " ";

                var emailAccount = _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId).Result;
                //var email = new QueuedEmail
                //{
                //    Priority = QueuedEmailPriority.High,
                //    EmailAccountId = emailAccount.Id,
                //    FromName = emailAccount.DisplayName,
                //    From = emailAccount.Email,
                //    Bcc = "h.woo@hansamed.net",
                //    ToName = "Kulvindertpss",
                //    To = "Kulvindertpss@gmail.com",
                //    Subject = "Testing PaymentInvoice:",
                //    Body = Message,
                //    CreatedOnUtc = DateTime.UtcNow,
                //};
                //_queuedEmailService.InsertQueuedEmailAsync(email);

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(Convert.ToString(resultjson));

                XmlNode responseNode = xmlDoc.SelectSingleNode("/response");
                XmlNode receiptNode = xmlDoc.SelectSingleNode("/response/receipt");

                //if (responseNode != null && receiptNode != null)


                string receiptXml = receiptNode.OuterXml;



                // var paymentresponse = processPaymentRequest.CustomValues["PaymentResponse"].ToString();
                var paymentresponse = receiptXml;
                

                var xmlresponse = new XmlDocument();
                xmlresponse.LoadXml(paymentresponse);
                XmlNodeList xmlNodeListresponse_code = xmlresponse.SelectNodes("//cc/response_code");
                XmlNodeList xmlNodeListapproval_code = xmlresponse.SelectNodes("//cc/approval_code");
                if (xmlNodeListresponse_code[0].InnerText != "421" && xmlNodeListapproval_code[0].InnerText != "000000")
                {


                    var processPaymentRequest = new ProcessPaymentRequest();

                    processPaymentRequest.StoreId = _storeContext.GetCurrentStore().Id;
                    //processPaymentRequest.CustomerId = _workContext.CurrentCustomer.Id;
                    processPaymentRequest.CustomValues.Add("CustomerNumber", CustomerNo);
                    //processPaymentRequest.PaymentMethodSystemName = "FoxNetSoft.MonerisCheckout";
                    processPaymentRequest.OrderGuid = System.Guid.NewGuid();
                    processPaymentRequest.OrderTotal = decimal.Parse(TotalAmount);

                    //Add the token required for Moneris to post
                    processPaymentRequest.CustomValues.Add("Token", ticketId);

                    string WebShopTransactionID = "";

                    var respmodel = new PaymentReponse();
                    respmodel.Amount = processPaymentRequest.OrderTotal;

                    XmlNodeList xmlNodeList = xmlresponse.SelectNodes("//cc/reference_no");
                    if (xmlNodeList.Count > 0)
                    {
                        respmodel.TransactionId = xmlNodeList[0].InnerText;
                    }

                    // Storing in Session, Then showing these valies in PaymentComplete View
                    HttpContext.Session.SetString("PaymentAmount", TotalAmount);
                    HttpContext.Session.SetString("PaymentTranId", respmodel.TransactionId);

                    string jsontraninfo = PaymentTranInfo;



                    // Serialize the C# object back to a nicely formatted JSON string

                    // if Total <= 0 then update the jsontraninfo
                    // Update AmountPaid = 0
                    // Sum of all Amount in detail where InvoiceType= Invoice Number
                    // update the Credit Amount to Sum of All amount in detail where InvoiceType= Invoice Number
                    if (processPaymentRequest.OrderTotal <= 0)
                    {
                        jsontraninfo = updateJsonPost(jsontraninfo);
                    }
                    else
                    {
                        jsontraninfo = JsonConvert.DeserializeObject(jsontraninfo).ToString();
                    }
                    //Post the data to the database
                    var datasettings = DataSettingsManager.LoadSettings();
                    //var connstring = datasettings.ConnectionString;
                    var connstring = _configuration["Nop:HansamedConnectionistring"];
                    //Get a connection, easier (and faster) than trying to use the EF methods for this simple execute
                    var conn = new SqlConnection(connstring);
                    conn.Open();

                    var cmdNewUser = new SqlCommand
                    {
                        Connection = conn,
                        CommandType = CommandType.StoredProcedure,
                        CommandText = "dbo.spAddPaymentInvoice"
                    };

                    cmdNewUser.Parameters.AddWithValue("@PaymentInfo", jsontraninfo); // this is saving as Json, this response send to Payment SAP API
                    cmdNewUser.Parameters.AddWithValue("@PayResult", paymentresponse); // Generated from Meoris Payment Response
                    cmdNewUser.Parameters.Add("@RetId", SqlDbType.Int);
                    cmdNewUser.Parameters["@RetId"].Direction = ParameterDirection.Output;

                    cmdNewUser.ExecuteScalar();
                    int PaymentId = Convert.ToInt32(cmdNewUser.Parameters["@RetId"].Value);
                    conn.Close();

                    PaymentInvoiceModel payment = JsonConvert.DeserializeObject<PaymentInvoiceModel>(jsontraninfo);
                    payment.PaymentId = PaymentId; // add PaymentId in Object

                    // *************** Update SapInvoices table
                    // Update Balance Columns in SapInvoices and SapCreditMemos Table
                    // Call SP GetOfflineTransaction_ApplyPayment

                    // get List of all invoices by Customre Number. 
                    // doesn't matter, if Guest Customer OR Login Customer
                    // to get the existing Balance for each invoice

                    List<Invoice> invoices = new List<Invoice>();
                    var CustNumber = CustomerNo;
                    invoices = GetPdfInvoicesByCustomerNumber(CustNumber);

                    // if there is no invoice then dont update the balance in SAPInvoice Table
                    // SKIP the following Step

                    if (invoices.Count > 0)
                    {
                        foreach (PaymentInvoiceItemModel item in payment.InvoiceDetails)
                        {
                            var conn2 = new SqlConnection(connstring);
                            conn2.Open();
                            var cmd = new SqlCommand
                            {
                                Connection = conn2,
                                CommandType = CommandType.StoredProcedure,
                                CommandText = "dbo.GetOfflineTransaction_ApplyPayment"
                            };

                            cmd.Parameters.AddWithValue("@InvNumber", item.Number);

                            // To get the Balance from each Invoice by Invoice Number
                            Invoice objInvoice = invoices.Where(i => i.DocumentNumber == item.Number).FirstOrDefault();

                            decimal existingBalance = Convert.ToDecimal(objInvoice.Balance);

                            // Current Balance need to update in Balance Column 
                            // in both table SAPInvoices, SAPCreditMemos
                            decimal currentBalance;

                            // if value is -ve like Credit Memo
                            // in PaymentInvoiceDetail table the Amount is -ve in case of CREDIT MEMO
                            if (item.Amount < 0)
                            {
                                currentBalance = existingBalance + item.Amount;
                            }
                            else
                            {
                                currentBalance = existingBalance - item.Amount;
                            }

                            // in both table SAPInvoices, SAPCreditMemos balance will be +ve
                            // dont need to put any condition
                            cmd.Parameters.AddWithValue("@Payment", currentBalance);
                            cmd.Parameters.AddWithValue("@InvoiceType", item.InvoiceType);

                            var fullAmount = "Y";

                            if (currentBalance > 0)
                            {
                                fullAmount = "N";
                            }

                            if (item.InvoiceType == "Credit Memo")
                            {
                                // in XML Post, Credit Memo Amount need to keep positive in case of Credit Memo
                                item.Amount = item.Amount * -1;
                            }

                            cmd.Parameters.AddWithValue("@FullAmount", fullAmount);
                            var retval2 = cmd.ExecuteScalar();
                            conn2.Close();
                        }
                        // *************** Update SapInvoices table
                    }

                    // in case of Customer Pay without login / by link then, 
                    // they meed to entr First Name and Last Name, 
                    // otherwise, it will be Empty
                    string custname = payment.FirstName + " " + payment.LastName;

                    //Send the email message
                    // For Testing
                    //payment.Email = "r.khan@hansamed.net";

                    SendCompletionEmail(payment.Email, custname, PaymentCustomerTag, PaymentInvoiceTag, respmodel.TransactionId);

                    // assign Transaction ID
                    payment.WebPortalTransactionId = respmodel.TransactionId;

                    // Get Credit Card
                    var CreditCard = "";
                    XmlNodeList xmlNodeCreditCardList = xmlresponse.SelectNodes("//cc/tokenize/first4last4");

                    if (xmlNodeCreditCardList.Count > 0)
                    {
                        if (xmlNodeCreditCardList[0].InnerText.Length > 0)
                        {
                            CreditCard = xmlNodeCreditCardList[0].InnerText.Substring(xmlNodeCreditCardList[0].InnerText.Length - 4);
                        }
                        else
                        {
                            CreditCard = "";
                        }
                    }

                    // Update PaymentPostXml
                    string xmlPaymentPost = GeneratePaymentPostXML(payment, CreditCard);

                    // Update PaymentInvoice table with XML Payment Post and TransactionID
                    if (processPaymentRequest.OrderTotal > 0) // then Payment through Moneris
                    {
                        UpdatePaymentPostXml(payment.PaymentId, xmlPaymentPost, payment.WebPortalTransactionId, true);
                    }
                    else // then Payment through Customer Credit
                    {
                        UpdatePaymentPostXml(payment.PaymentId, xmlPaymentPost, payment.WebPortalTransactionId, false);
                    }

                    //raise event
                    _eventPublisher.Publish(new PaymentPlacedEvent(payment));
                   
                   
                  
                    //success
                    return true;

                    //if (processPaymentRequest.OrderTotal > 0)
                    //{
                    //    return Json(new
                    //    {
                    //        success = 1,
                    //        transactionid = respmodel.TransactionId,
                    //        amount = respmodel.Amount
                    //    });
                    //}
                    //else
                    //{
                    //    return Json(new
                    //    {
                    //        success = 1,
                    //        transactionid = respmodel.TransactionId,
                    //        amount = respmodel.Amount
                    //    });
                    //}
                }
                else
                {                   
                    return false;
                    //return Json(new { success = 0, });
                }
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.GetCurrentCustomerAsync().Result);               
                return false;
            }
        }

    }
}

