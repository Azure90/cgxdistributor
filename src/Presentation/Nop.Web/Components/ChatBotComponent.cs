using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Controllers;
using Nop.Web.Framework.Components;
using static Nop.Web.Controllers.MedicalChatController;
using Nop.Services.Configuration;
using Nop.Services.ChatBotHistory;
using Nop.Core;
using System.Linq;
using System;
using Nop.Services.Localization;

namespace Nop.Web.Components
{
    public partial class ChatBotComponent : NopViewComponent
    {
        private readonly MedicalChatController _chatController;

        private readonly Ichatbothistory _chatbothistory;
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        public ChatBotComponent(ISettingService settingService, IWorkContext workContext, ILocalizationService localizationService, MedicalChatController chatController, Ichatbothistory chatbothistory)
        {
            _chatController = chatController;
            _settingService = settingService;
            _chatbothistory = chatbothistory;
            _localizationService = localizationService;
            _workContext = workContext;
        }
        public IViewComponentResult Invoke()
        {
            var allchat = _chatbothistory.GetAllChatbothistory();
            var custid = _workContext.GetCurrentCustomerAsync().Result.Id;

            var chatBotPrompt = "Chatbot.ChatBotprompt";
            var chatBotNumberOfQuestions = "Chatbot.ChatBotnumberofquestions";
            var chatBotWarning = "Chatbot.chatbotfreeaccess";

            var lastQuestionTime = allchat
                .Where(x => x.CustID == custid && x.IsAI == true)
                .OrderByDescending(x => x.DateandTime)
                .Select(x => x.DateandTime)
                .FirstOrDefault();

            if (_workContext.GetCurrentCustomerAsync().Result.Email == null)
            {
                chatBotWarning = "Chatbot.chatbotfreeaccessBefore";
                chatBotNumberOfQuestions = "Chatbot.ChatBotnumberofquestionsBefore";
            }

            var getdate = _localizationService.GetResourceAsync("Chatbot.ChatBotTime").Result;
            ViewBag.chatbotimage = Convert.ToString(_localizationService.GetResourceAsync("Chatbot.Chatbotimage").Result);

            if (int.TryParse(getdate, out int minutes) && lastQuestionTime.HasValue)
            {
                var lastTimeWithAddedMinutes = lastQuestionTime.Value.AddMinutes(minutes);

                if (lastTimeWithAddedMinutes < DateTime.Now)
                {
                    var delehistory = allchat.Where(x => x.CustID == custid).ToList();
                    foreach (var i in delehistory)
                    {
                        _chatbothistory.DeleteChatBotHistory(i);
                    }
                    allchat = _chatbothistory.GetAllChatbothistory();
                }
            }

            var chatbotlimit = Convert.ToInt32(_localizationService.GetResourceAsync(chatBotNumberOfQuestions).Result);
            ViewBag.ChatBotButtoncheckbox = _settingService.GetSetting("ChatBotButtoncheckboxActive/Inactive").Value.ToLower();

            if (allchat.Where(x => x.CustID == custid && x.IsAI == true).Count() >= chatbotlimit)
            {
                ViewBag.chatbotlimit = "Over";
            }

            ViewBag.chatbotwarning = Convert.ToString(_localizationService.GetResourceAsync(chatBotWarning).Result);

            // ✅ Pass the list of chatbothistory to the view
            var model = allchat.Where(x => x.CustID == custid).ToList();
            return View(model);
        }
    }
}
