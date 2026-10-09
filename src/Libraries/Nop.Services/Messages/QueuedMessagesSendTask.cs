using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using DocumentFormat.OpenXml.EMMA;
using Newtonsoft.Json.Linq;
using Nop.Core.Domain.Messages;
using Nop.Services.Logging;
using Nop.Services.ScheduleTasks;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Model;

namespace Nop.Services.Messages
{
    /// <summary>
    /// Represents a task for sending queued message 
    /// </summary>
    public partial class QueuedMessagesSendTask : IScheduleTask
    {
        #region Fields

        private readonly IEmailAccountService _emailAccountService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly EmailAccountSettings _emailAccountSettings;

        #endregion

        #region Ctor

        public QueuedMessagesSendTask(IEmailAccountService emailAccountService,
            IEmailSender emailSender,
            ILogger logger,
            IQueuedEmailService queuedEmailService,
            EmailAccountSettings emailAccountSettings)
        {
            _emailAccountService = emailAccountService;
            _emailSender = emailSender;
            _logger = logger;
            _queuedEmailService = queuedEmailService;
            _emailAccountSettings = emailAccountSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Executes a task
        /// </summary>
        public virtual async System.Threading.Tasks.Task ExecuteAsync()
        {
            var maxTries = 3;
            var queuedEmails = await _queuedEmailService.SearchEmailsAsync(null, null, null, null,null,
                true, true, maxTries, false, 0, 500);
            foreach (var queuedEmail in queuedEmails)
            {
                var bcc = string.IsNullOrWhiteSpace(queuedEmail.Bcc)
                            ? null
                            : queuedEmail.Bcc.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                var cc = string.IsNullOrWhiteSpace(queuedEmail.CC)
                            ? null
                            : queuedEmail.CC.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    //code by tp
                    // if (queuedEmail.Subject.Contains("Reset your password"))
                    //{
                    //IList<EmailAccount> emailAccount = _emailAccountService.GetAllEmailAccounts();
                    var emailAccount = _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId).Result;
                    var ApiKey = "";
                    var DisplayName = "";
                    var emailAccountEmail = "";
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
                        string ToEmail = queuedEmail.To;
                        string ToName = string.IsNullOrEmpty(queuedEmail.ToName) ? null : queuedEmail.ToName;



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


                        List<SendSmtpEmailBcc> BccTO = new List<SendSmtpEmailBcc>();
                        foreach (var i in bcc)
                        {
                            string tobcc = i;
                            SendSmtpEmailBcc smtpbcc = new SendSmtpEmailBcc(tobcc, null);
                            BccTO.Add(smtpbcc);
                        }
                        //List<SendSmtpEmailCc> ccTO = new List<SendSmtpEmailCc>();
                        //foreach (var i in cc)
                        //{
                        //    string tocc = i;
                        //    SendSmtpEmailCc smtpcc = new SendSmtpEmailCc(tocc, null);
                        //    ccTO.Add(smtpcc);
                        //}

                        var body = queuedEmail.Body;
                        var Subject = queuedEmail.Subject;
                        Newtonsoft.Json.Linq.JObject Headers = new Newtonsoft.Json.Linq.JObject();
                        // Headers.Add(emailAccountEmail, Password);
                        Headers.Add("content-type", "application/json");
                        Headers.Add("api-key", ApiKey);
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        // var sendSmtpEmail = new SendSmtpEmail(Email, To, null, null, body, null, Subject, null, null, Headers, null, null, null, null);
                        var sendSmtpEmail = new SendSmtpEmail(Email, To, BccTO, null, body, null, Subject, null, null, Headers, null, null, null, null);
                        CreateSmtpEmail result = apiInstance.SendTransacEmail(sendSmtpEmail);
                        var messageid = result.MessageId;

                        queuedEmail.MessageCode = messageid;
                        queuedEmail.SentOnUtc = DateTime.UtcNow;
                        queuedEmail.Status = "Queued";
                    }



                    //await _emailSender.SendEmailAsync(await _emailAccountService.GetEmailAccountByIdAsync(queuedEmail.EmailAccountId),
                    //    queuedEmail.Subject,
                    //    queuedEmail.Body,
                    //   queuedEmail.From,
                    //   queuedEmail.FromName,
                    //   queuedEmail.To,
                    //   queuedEmail.ToName,
                    //   queuedEmail.ReplyTo,
                    //   queuedEmail.ReplyToName,
                    //   bcc,
                    //   cc,
                    //   queuedEmail.AttachmentFilePath,
                    //   queuedEmail.AttachmentFileName,
                    //   queuedEmail.AttachedDownloadId);

                    //queuedEmail.SentOnUtc = DateTime.UtcNow;
                }
                catch (Exception exc)
                {
                    await _logger.ErrorAsync($"Error sending e-mail. {exc.Message}", exc);
                }
                finally
                {
                    queuedEmail.SentTries += 1;
                    await _queuedEmailService.UpdateQueuedEmailAsync(queuedEmail);
                }
            }
        }

        #endregion
    }
}