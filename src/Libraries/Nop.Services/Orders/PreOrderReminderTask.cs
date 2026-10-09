using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Orders.PreOrders;
using Nop.Services.Customers;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.ScheduleTasks;
using Nop.Services.Stores;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Model;
using Task = System.Threading.Tasks.Task;

namespace Nop.Services.Orders
{
    /// <summary>
    ///  Represents a task for sending reminder messages
    /// </summary>
    public class PreOrderReminderTask : IScheduleTask
    {

        #region Fields

        private readonly ILogger _logger;
        private readonly IPreOrderService _preOrderService;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ICustomerService _customerService;
        private readonly IMessageTokenProvider _messageTokenProvider;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly ITokenizer _tokenizer;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly IStoreService _storeService;
        private readonly IOrderService _orderService;

        #endregion

        #region Ctor

        public PreOrderReminderTask(ILogger logger,
            IPreOrderService preOrderService,
            IMessageTemplateService messageTemplateService,
            IEmailAccountService emailAccountService,
            ICustomerService customerService,
            IMessageTokenProvider messageTokenProvider,
            EmailAccountSettings emailAccountSettings,
            ITokenizer tokenizer,
            IQueuedEmailService queuedEmailService,
            IStoreService storeService,
            IOrderService orderService)
        {
            _logger = logger;
            _preOrderService = preOrderService;
            _messageTemplateService = messageTemplateService;
            _emailAccountService = emailAccountService;
            _customerService = customerService;
            _messageTokenProvider = messageTokenProvider;
            _emailAccountSettings = emailAccountSettings;
            _tokenizer = tokenizer;
            _queuedEmailService = queuedEmailService;
            _storeService = storeService;
            _orderService = orderService;
        }

        #endregion


        #region Utilities

        private async Task<EmailSendResult> SendReminderEmailAsync(Customer customer, Order order, int messageTemplateId,
            PreOrderReminderPhase reminderPhase, List<Token> tokens)
        {
            var result = new EmailSendResult();

            try
            {
                if (customer == null)
                    throw new ArgumentNullException(nameof(customer));

                var template = await _messageTemplateService.GetMessageTemplateByIdAsync(messageTemplateId);

                if (template == null)
                {
                    await _logger.ErrorAsync($"PreOrder reminder template not found. TemplateId: {messageTemplateId}");

                    result.Error = "Message template not found.";
                    return result;
                }

                // get email account
                var emailAccount = await GetEmailAccountAsync(template.EmailAccountId);
                if (emailAccount == null)
                {
                    await _logger.ErrorAsync("Email account not found.", customer: customer);

                    result.Error = "Email account not found.";
                    return result;
                }

                // customer full name 
                var customerName = await _customerService.GetCustomerFullNameAsync(customer);

                // standard nopCommerce tokens

                // customer tokens
                await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);

                // store tokens
                await _messageTokenProvider.AddStoreTokensAsync(tokens,
                    await _storeService.GetStoreByIdAsync(order.StoreId), emailAccount);

                // order tokens
                await _messageTokenProvider.AddOrderTokensAsync(tokens, order, order.CustomerLanguageId);

                // pre-order tokens
                await AddPreOrderTokensAsync(tokens, order);

                // Replace tokens
                var subject = _tokenizer.Replace(template.Subject, tokens, false);
                var body = _tokenizer.Replace(template.Body, tokens, true);

                // Configure Brevo API key safely
                sib_api_v3_sdk.Client.Configuration.Default.ApiKey["api-key"] = emailAccount.ApiKey;

                var apiInstance = new TransactionalEmailsApi();

                var sender = new SendSmtpEmailSender(emailAccount.DisplayName, emailAccount.Email);

                var recipients = new List<SendSmtpEmailTo>
                {
                    new(customer.Email, customerName)
                };

                List<SendSmtpEmailBcc> bcc = null;

                if (!string.IsNullOrWhiteSpace(template.BccEmailAddresses))
                {
                    bcc = template.BccEmailAddresses
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => new SendSmtpEmailBcc(x.Trim()))
                        .ToList();
                }

                var email = new SendSmtpEmail(sender, recipients, bcc, null, body, null, subject);

                var apiResult = apiInstance.SendTransacEmail(email);

                if (apiResult == null || string.IsNullOrWhiteSpace(apiResult.MessageId))
                {
                    await _logger.ErrorAsync($"Failed to send PreOrder reminder to '{customer.Email}'. Brevo returned no MessageId.");

                    result.Success = false;
                    result.Error = "Brevo did not return a MessageId.";

                    return result;
                }

                // insert queue email
                await _queuedEmailService.InsertQueuedEmailAsync(new QueuedEmail
                {
                    PriorityId = (int)QueuedEmailPriority.High,
                    EmailAccountId = emailAccount.Id,
                    FromName = emailAccount.DisplayName,
                    From = emailAccount.Email,
                    To = customer.Email,
                    Subject = subject,
                    Body = body,
                    CreatedOnUtc = DateTime.UtcNow,
                    SentOnUtc = DateTime.UtcNow,
                    SentTries = 1,
                    DontSendBeforeDateUtc = DateTime.UtcNow,
                    MessageCode = apiResult.MessageId,
                    CustomerId = customer.Id,
                    OrderId = order.Id,
                    ReminderPhase = (int)reminderPhase,
                    Status = "Queued"
                });

                result.Success = true;
                result.ToEmail = customer.Email;
                result.MessageId = apiResult.MessageId;

                return result;
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error sending PreOrder " +
                    $"reminder email to '{customer?.Email}'.", ex);

                result.Success = false;
                result.Error = ex.Message;

                return result;
            }
        }

        protected virtual async Task<EmailAccount> GetEmailAccountAsync(int emailAccountId)
        {
            return await _emailAccountService.GetEmailAccountByIdAsync(emailAccountId)
                ?? await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId)
                ?? throw new NopException("Email account could not be loaded");
        }

        #endregion


        #region Methods

        public async Task ExecuteAsync()
        {
            try
            {
                await _logger.InformationAsync("PreOrder reminder task started.");

                await ProcessPreOrderRemindersAsync();

                await _logger.InformationAsync("PreOrder reminder task completed successfully.");
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Error while processing PreOrder reminders.", ex);

                throw;
            }
        }

        private async Task ProcessPreOrderRemindersAsync()
        {
            // load reminder settings
            var reminderSettings = await _preOrderService.GetPreOrderReminderSettingsAsync();

            if (reminderSettings == null)
                return;

            // get all eligible preorder order items
            var preOrderItems = await _orderService.GetPreOrderItemsForRemindersAsync();

            // process each preorder item
            foreach (var orderItem in preOrderItems)
            {
                try
                {
                    if (!orderItem.PreferredDeliveryDate.HasValue)
                        continue;

                    // check for valid order 
                    var order = await _orderService.GetOrderByIdAsync(orderItem.OrderId);

                    if (order == null)
                        continue;

                    // check for valid customer
                    var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
                    if (customer == null)
                        continue;

                    // calculate remaining days
                    var daysRemaining = (orderItem.PreferredDeliveryDate.Value.Date - DateTime.UtcNow.Date).Days;

                    // skip (-negative days)
                    if (daysRemaining < 0)
                        continue;

                    // Initial Reminder
                    var shouldSendInitialReminder = await ShouldSendReminderAsync(order, daysRemaining,
                        reminderSettings.InitialReminderDays, reminderSettings.InitialReminderActive, PreOrderReminderPhase.Initial);

                    if (shouldSendInitialReminder)
                        await SendInitialReminderAsync(customer, order, reminderSettings);

                    // Final Reminder
                    var shouldSendFinalReminder = await ShouldSendReminderAsync(order, daysRemaining,
                        reminderSettings.FinalReminderDays, reminderSettings.FinalReminderActive, PreOrderReminderPhase.Final);

                    if (shouldSendFinalReminder)
                        await SendFinalReminderAsync(customer, order, reminderSettings);

                    // Delivery Reminder
                    var shouldSendDeliveryReminder = await ShouldSendReminderAsync(order, daysRemaining,
                        reminderSettings.DeliveryReminderDays, reminderSettings.DeliveryReminderActive, PreOrderReminderPhase.Delivery);

                    if (shouldSendDeliveryReminder)
                        await SendDeliveryReminderAsync(customer, order, reminderSettings);
                }
                catch (Exception ex)
                {
                    await _logger.ErrorAsync($"Error processing PreOrder" +
                        $" reminders for OrderId: {orderItem.OrderId}.", ex);
                }
            }
        }

        private async Task<bool> ShouldSendReminderAsync(Order order, int daysRemaining, int reminderDays,
            bool isReminderActive, PreOrderReminderPhase phase)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            // template feature disabled
            if (!isReminderActive)
                return false;

            if (phase == PreOrderReminderPhase.Initial)
            {
                if (order.CreatedOnUtc.Date != DateTime.UtcNow.Date)
                    return false;
            }
            else
            {
                if (daysRemaining != reminderDays)
                    return false;
            }

            // Already queued
            if (await _queuedEmailService.IsReminderQueuedAsync(order.CustomerId, order.Id, phase))
                return false;

            // Enforce reminder order
            switch (phase)
            {
                case PreOrderReminderPhase.Initial:
                    return true;

                case PreOrderReminderPhase.Final:
                    return await _queuedEmailService.IsReminderQueuedAsync(
                        order.CustomerId,
                        order.Id,
                        PreOrderReminderPhase.Initial);

                case PreOrderReminderPhase.Delivery:
                    return await _queuedEmailService.IsReminderQueuedAsync(
                        order.CustomerId,
                        order.Id,
                        PreOrderReminderPhase.Final);

                default:
                    return false;
            }
        }

        public async Task<bool> SendInitialReminderAsync(Customer customer, Order order, PreOrderReminderSettings reminderSettings)
        {
            if (reminderSettings == null || reminderSettings.InitialReminderTemplateId <= 0)
                return false;

            var tokens = new List<Token>();

            var result = await SendReminderEmailAsync(customer, order, reminderSettings.InitialReminderTemplateId,
                PreOrderReminderPhase.Initial, tokens);

            return result.Success;
        }

        //public async Task<bool> SendEditableReminderAsync(Customer customer, Order order, PreOrderReminderSettings reminderSettings)
        //{
        //    if (reminderSettings == null || reminderSettings.EditableReminderTemplateId <= 0)
        //        return false;

        //    var tokens = new List<Token>();

        //    // send editable reminder
        //    var result = await SendReminderEmailAsync(customer, order,
        //        reminderSettings.EditableReminderTemplateId,
        //        PreOrderReminderPhase.Editable, tokens);

        //    return result.Success;
        //}

        public async Task<bool> SendFinalReminderAsync(Customer customer, Order order, PreOrderReminderSettings reminderSettings)
        {
            if (reminderSettings == null || reminderSettings.FinalReminderTemplateId <= 0)
                return false;

            var tokens = new List<Token>();

            // send final reminder 
            var result = await SendReminderEmailAsync(customer, order,
                reminderSettings.FinalReminderTemplateId,
                PreOrderReminderPhase.Final, tokens);

            return result.Success;
        }

        public async Task<bool> SendDeliveryReminderAsync(Customer customer, Order order, PreOrderReminderSettings reminderSettings)
        {
            if (reminderSettings == null || reminderSettings.DeliveryReminderTemplateId <= 0)
                return false;

            var tokens = new List<Token>();

            // send deliver reminder
            var result = await SendReminderEmailAsync(customer, order,
                reminderSettings.DeliveryReminderTemplateId,
                PreOrderReminderPhase.Delivery, tokens);

            return result.Success;
        }

        public virtual async Task AddPreOrderTokensAsync(IList<Token> tokens, Order order)
        {
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

            // get first preferred delivery date 
            var preOrderItem = orderItems.FirstOrDefault(x => x.PreferredDeliveryDate.HasValue);

            var preferredDeliveryDate = preOrderItem?.PreferredDeliveryDate;

            if (preferredDeliveryDate.HasValue)
            {
                tokens.Add(new Token(
                    "PreOrder.PreferredDeliveryDate",
                    preferredDeliveryDate.Value.ToShortDateString()));
            }

            // Add ChangeDeadline, ReminderPhase, etc.
        }

        #endregion
    }

    #region Nested Classes

    public class EmailSendResult
    {
        public bool Success { get; set; }
        public string ToEmail { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
    }

    // Editable = 2, // Removed 
    public enum PreOrderReminderPhase
    {
        None = 0,
        Initial = 1,
        Final = 2,
        Delivery = 3
    }

    #endregion
}
