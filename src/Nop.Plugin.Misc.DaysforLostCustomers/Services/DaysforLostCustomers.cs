using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Services.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Nop.Core.Domain.Messages;
using Nop.Core.Events;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.DiscountPromotion;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.ScheduleTasks;
using Nop.Services.Stores;
using Microsoft.Extensions.Logging;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Model;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.DiscountPromotions;


namespace Nop.Plugin.Misc.DaysforLostCustomers.Services
{
    public partial class DaysforLostCustomers : IScheduleTask
    {
        private readonly Nop.Services.Logging.ILogger _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly INotificationService _notificationService;

        private readonly IStoreService _storeService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IMessageTokenProvider _messageTokenProvider;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPictureService _pictureService;
        private readonly ICustomerService _customerService;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly ILocalizationService _localizationService;
        private readonly IDiscountPromotionService _DiscountPromotionsRepository;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly ITokenizer _tokenizer;
        private readonly ISettingService _settingService;
        public DaysforLostCustomers(
           Nop.Services.Logging.ILogger logger,
           IEventPublisher eventPublisher,
           IOrderService orderService,
           IProductService productService,
           INotificationService notificationService,
           IStoreService storeService,
           IEmailAccountService emailAccountService,
           IMessageTokenProvider messageTokenProvider,
           IWorkflowMessageService workflowMessageService,
           IHttpContextAccessor httpContextAccessor,
           IPictureService pictureService,
           ICustomerService customerService,
           EmailAccountSettings emailAccountSettings,
           IQueuedEmailService queuedEmailService,
           IDiscountPromotionService DiscountPromotionsRepository,
           IMessageTemplateService messageTemplateService,
           ITokenizer tokenizer,
             ISettingService settingService,
           ILocalizationService localizationService
           )
        {
            _logger = logger;
            _eventPublisher = eventPublisher;
            _orderService = orderService;
            _productService = productService;
            _notificationService = notificationService;
            _storeService = storeService;
            _emailAccountService = emailAccountService;
            _messageTokenProvider = messageTokenProvider;
            _workflowMessageService = workflowMessageService;
            _httpContextAccessor = httpContextAccessor;
            _pictureService = pictureService;
            _customerService = customerService;
            _queuedEmailService = queuedEmailService;
            _emailAccountSettings = emailAccountSettings;
            _DiscountPromotionsRepository = DiscountPromotionsRepository;
            _localizationService = localizationService;
            _messageTemplateService = messageTemplateService;
            _settingService = settingService;
            _tokenizer = tokenizer;
        }
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            await lostcustomer();
            await remindermailcustomer();

        }
        private async System.Threading.Tasks.Task lostcustomer()
        {
            // strat the product review plugin 
            await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Information, "Product Review Task Plugin Execution Start at : " + DateTime.Now.ToString());

            // ********* Get The List of Emails from database, those due date are tomorror or past
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var discountcode = _DiscountPromotionsRepository.GetAllDiscountPromotions().ToList();
            var dt = new DataTable();
            var conn = new SqlConnection(connstring);
            var da = new SqlDataAdapter();
            conn.Open();
            //var cmd = new SqlCommand("SELECT distinct(CustomerId) FROM [order]  " +
            //         " WHERE DATEDIFF(DAY, CreatedOnUtc, GETDATE()) > (select value from Setting where name ='DaysforLostCustomers')", conn)
            //var cmd = new SqlCommand("SELECT   o.CustomerID, MAX(o.CreatedOnUtc) AS LastOrderDate FROM [Order] o WHERE   " +
            //     " DATEDIFF(DAY, o.CreatedOnUtc, GETDATE()) >  (SELECT Value FROM Setting WHERE Name = 'DaysforLostCustomers') " +
            //           " AND NOT EXISTS (  SELECT 1 FROM QueuedEmail qe    WHERE qe.CustomerId = o.CustomerId " +
            //      " AND qe.Discountcode in (select Discountcode from DiscountPromotions where DiscountPromotionType=2 )  )GROUP BY o.CustomerID", conn)
            var cmd = new SqlCommand("SELECT o.CustomerID, MAX(o.CreatedOnUtc) AS LastOrderDate,(SELECT TOP 1 UsePercentage "
                                   + "FROM Discount WHERE CouponCode IN (SELECT DiscountCode FROM DiscountPromotions WHERE DiscountPromotionType = 2) "
                                   + "ORDER BY ID DESC) AS UsePercentage,(SELECT TOP 1 DiscountPercentage FROM Discount WHERE CouponCode IN "
                                   + "(SELECT DiscountCode FROM DiscountPromotions WHERE DiscountPromotionType = 2) ORDER BY ID DESC) AS DiscountPercentage, "
                                   + "(SELECT TOP 1 DiscountCode FROM DiscountPromotions WHERE DiscountPromotionType = 2 ORDER BY ID DESC) AS DiscountCode, "
                                   + "(SELECT AddDays FROM DiscountPromotions WHERE DiscountPromotionType = 2) AS DiscountAddDays "
                                   + "FROM [Order] o WHERE DATEDIFF(DAY, o.CreatedOnUtc, GETDATE()) >  (SELECT Value FROM Setting WHERE Name = 'DaysforLostCustomers') "
                                   + "AND NOT EXISTS (SELECT 1 FROM QueuedEmail qe WHERE qe.CustomerId = o.CustomerId AND qe.DiscountCode IN "
                                   + "(SELECT DiscountCode FROM DiscountPromotions WHERE DiscountPromotionType = 2))GROUP BY o.CustomerID;", conn)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };
            try
            {
                da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        var custId = Convert.ToInt32(dr["CustomerId"]);

                        // kulvinder 21/02/2025
                        var lastorderdate =dr["LastOrderDate"];
                        var usepercentage = Convert.ToInt32(dr["UsePercentage"]);
                        var discountpercentage = Convert.ToInt32(dr["DiscountPercentage"]);
                        var discountadddays = Convert.ToInt32(dr["DiscountAddDays"]);
                        var discountCode = Convert.ToInt32(dr["DiscountCode"]);
                        var discountpercentageFormatted = "";

                        if (usepercentage == 1)
                        {
                            discountpercentageFormatted = $"{discountpercentage}%"; 
                        }
                        else
                        {
                            discountpercentageFormatted = $"${discountpercentage}"; 
                        }


                        var customer = await _customerService.GetCustomerByIdAsync(custId);

                        if (customer.FirstName == "HANSAmed HJ" || customer.Email == "h.woo@hansamed.net" || customer.Email == "officialtpss@gmail.com")
                        {
                           // var customerdiscountcode = discountcode.Where(x => x.DiscountPromotionType == 2).FirstOrDefault();
                           
                                DateTime currentDate = DateTime.Now.Date;
                                DateTime expiredate = currentDate.AddDays(Convert.ToDouble(discountadddays)).Date;
                                string formattedExpireDate = expiredate.ToString("yyyy-MM-dd");

                                var subjectReplaced = "";
                                var bodyReplaced = "";
                                var template = _workflowMessageService.DaysForLostCustomer(customer, 1, Convert.ToString(discountCode), Convert.ToString(discountadddays), Convert.ToString(formattedExpireDate), discountpercentageFormatted).Result;
                                subjectReplaced = _tokenizer.Replace(template[0].MessageTemplate.Subject, template[0].Tokens, false);
                                bodyReplaced = _tokenizer.Replace(template[0].MessageTemplate.Body, template[0].Tokens, true);
                                await SendEmailToCustomerAsync(customer.Email, subjectReplaced, bodyReplaced, Convert.ToString(discountCode), customer.Id);
                           
                        }
                    }
                }
                else
                {
                    await _logger.ErrorAsync("Product Review Plugin : There is no data in the table.");
                    return;
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Err: Product Review Plugin runs with an Exception ", ex);
                return;
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }

            await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Information, "Product Review Plugin Execution End's at : " + DateTime.Now.ToString());
        }
        private async System.Threading.Tasks.Task remindermailcustomer()
        {
            // strat the product review plugin 
            await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Information, "Product Review Task Plugin Execution Start at : " + DateTime.Now.ToString());

            // ********* Get The List of Emails from database, those due date are tomorror or past
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            var dt = new DataTable();
            var conn = new SqlConnection(connstring);
            var da = new SqlDataAdapter();
            conn.Open();
            var discountcode = _DiscountPromotionsRepository.GetAllDiscountPromotions().Where(x => x.DiscountPromotionType == 2).FirstOrDefault();

            if (discountcode == null)
            {
                discountcode = new DiscountPromotions();
            }
            var cmd = new SqlCommand("SELECT Distinct(q.CustomerId),IIF(  EXISTS ( SELECT TOP 1 1   FROM [order] o  " +
                           " WHERE o.CustomerId = q.CustomerId  AND o.CreatedOnUtc > q.SentOnUtc " +
                           " ORDER BY o.id DESC ), 'yes', 'no' ) AS HasRecentOrder  into #temp " +
                           " FROM QueuedEmail q WHERE q.CustomerId IS NOT NULL AND q.DiscountCode = (SELECT DiscountCode FROM DiscountPromotions WHERE DiscountPromotionType = 2) and " +
                           "  DATEDIFF(DAY, q.SentOnUtc, GETDATE()) = (SELECT ReminderDays FROM DiscountPromotions WHERE DiscountPromotionType = 2) AND " +
                           " q.SentOnUtc IS NOT NULL;  " +
                           " select Distinct(CustomerId),(select HasRecentOrder from #temp where customerid =q.CustomerId)    " +
                           " AS HasRecentOrder,(SELECT TOP 1 UsePercentage  " +
                           " FROM Discount WHERE CouponCode IN (SELECT DiscountCode FROM DiscountPromotions WHERE DiscountPromotionType = 2)  " +
                           " ORDER BY ID DESC) AS UsePercentage,(SELECT TOP 1 DiscountPercentage FROM Discount WHERE CouponCode IN  " +
                           " (SELECT DiscountCode FROM DiscountPromotions WHERE DiscountPromotionType = 2) ORDER BY ID DESC) AS DiscountPercentage " +
                           " from QueuedEmail q where customerid in (select customerid from #temp) and Discountcode !=(SELECT DiscountCode FROM DiscountPromotions WHERE DiscountPromotionType = 2)  ", conn)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            try
            {
                da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        var custId = Convert.ToInt32(dr["CustomerId"]);
                        var usepercentage = Convert.ToInt32(dr["UsePercentage"]);
                        var discountpercentage = Convert.ToInt32(dr["DiscountPercentage"]);
                        var customer = await _customerService.GetCustomerByIdAsync(custId);
                        var discountpercentageFormatted = "";

                        if (usepercentage == 1)
                        {
                            discountpercentageFormatted = $"{discountpercentage}%"; 
                        }
                        else
                        {
                            discountpercentageFormatted = $"${discountpercentage}"; 
                        }
                        if (customer.FirstName == "HANSAmed HJ" || customer.Email == "h.woo@hansamed.net" || customer.Email == "officialtpss@gmail.com")
                        {

                            var hasRecentOrder = dr["HasRecentOrder"];
                            if (hasRecentOrder == "no")
                            {
                                DateTime currentDate = DateTime.Now.Date;
                                DateTime expiredate = currentDate.AddDays(Convert.ToDouble(discountcode.ReminderDays)).Date;
                                string formattedExpireDate = expiredate.ToString("yyyy-MM-dd");

                                var subjectReplaced = "";
                                var bodyReplaced = "";

                                var template = _workflowMessageService.RemindertCustomer(customer, 1, discountcode.DiscountCode, discountcode.AddDays, Convert.ToString(formattedExpireDate), discountpercentageFormatted).Result;
                                subjectReplaced = _tokenizer.Replace(template[0].MessageTemplate.Subject, template[0].Tokens, false);
                                bodyReplaced = _tokenizer.Replace(template[0].MessageTemplate.Body, template[0].Tokens, true);
                                await SendEmailToCustomerAsync(customer.Email, subjectReplaced, bodyReplaced, discountcode.DiscountCode, customer.Id);
                            }
                        }
                    }
                }
                else
                {
                    await _logger.ErrorAsync("Product Review Plugin : There is no data in the table.");
                    return;
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Err: Product Review Plugin runs with an Exception ", ex);
                return;
            }
            finally
            {
                da.Dispose();
                conn.Close();
            }

            await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Information, "Product Review Plugin Execution End's at : " + DateTime.Now.ToString());
        }
        private async System.Threading.Tasks.Task SendEmailToCustomerAsync(string recipientEmail, string subject, string body, string discountcode, int customerid)
        {

            QueuedEmail Queued = new QueuedEmail();
            var emailAccount = _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId).Result;
            var apiInstance = new TransactionalEmailsApi();
            List<SendSmtpEmailAttachment> Attachment = new List<SendSmtpEmailAttachment>();
            string SenderName = string.IsNullOrEmpty(emailAccount.DisplayName) ? null : emailAccount.DisplayName;
            string SenderEmail = emailAccount.Email;
            SendSmtpEmailSender Email = new SendSmtpEmailSender(SenderName, SenderEmail);
            List<SendSmtpEmailTo> To = new List<SendSmtpEmailTo>();
            List<SendSmtpEmailBcc> Bcc = new List<SendSmtpEmailBcc>();
            string ToEmail = recipientEmail;
            string ToName = string.IsNullOrEmpty(recipientEmail) ? null : recipientEmail;

            ToName = ToName.ToUpper();

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
            SendSmtpEmailBcc smtpEmailbcc = new SendSmtpEmailBcc("h.woo@hansamed.net", "HJ");
            Bcc.Add(smtpEmailbcc);

            var NAme = _customerService.GetCustomerByEmailAsync(recipientEmail).Result.FirstName + " " + _customerService.GetCustomerByEmailAsync(recipientEmail).Result.LastName;


            var BccMail = _localizationService.GetResourceAsync("Customer.ProductReviewBcc").Result;


            var email = new QueuedEmail
            {
                Priority = QueuedEmailPriority.High,
                EmailAccountId = emailAccount.Id,
                FromName = emailAccount.DisplayName,
                From = emailAccount.Email,
                ToName = ToName,
                To = ToEmail,
                Bcc = BccMail,
                Subject = subject,
                Body = body,
                CustomerId = customerid,
                Discountcode = discountcode,
                CreatedOnUtc = DateTime.UtcNow,
                DontSendBeforeDateUtc = null
            };
            await _queuedEmailService.InsertQueuedEmailAsync(email);

        }

    }
}
