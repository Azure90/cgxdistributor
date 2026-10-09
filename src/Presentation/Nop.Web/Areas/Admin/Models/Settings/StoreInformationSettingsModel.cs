using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Settings
{
    /// <summary>
    /// Represents a store information settings model
    /// </summary>
    public partial record StoreInformationSettingsModel : BaseNopModel, ISettingsModel
    {
        #region Ctor

        public StoreInformationSettingsModel()
        {
            AvailableStoreThemes = new List<ThemeModel>();
        }

        #endregion

        #region Properties

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.StoreClosed")]
        public bool StoreClosed { get; set; }
        public bool StoreClosed_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DefaultStoreTheme")]
        public string DefaultStoreTheme { get; set; }
        public string chatbotfreeaccess { get; set; }
        public bool ChatBotButtonBeforeLoginCheckbox { get; set; }
        public string chatbotfreeaccessBefore { get; set; }
        public int ChatBotnumberofquestionsBefore { get; set; }
        public string ChatBotprompt { get; set; }
        public string ChatBotAccessButton { get; set; }
        public int ChatBotTime { get; set; }
        public int ChatBotnumberofquestions { get; set; }
        public string ChatbotAskme { get; set; }
        public string ChatbotAskbutton { get; set; }
        public string ChatbotLabelColor { get; set; }
        public string ChatbotProductinfotab { get; set; }
        public string ChatbotOtherinfotab { get; set; }
        public string Chatbotistyping { get; set; }
        public string Chatbotheader { get; set; }
        public string ImagePath { get; set; }
        public IFormFile ImageFile { get; set; }
        public bool DefaultStoreTheme_OverrideForStore { get; set; }
        public IList<ThemeModel> AvailableStoreThemes { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.AllowCustomerToSelectTheme")]
        public bool AllowCustomerToSelectTheme { get; set; }
        public bool AllowCustomerToSelectTheme_OverrideForStore { get; set; }

        [UIHint("Picture")]
        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.Logo")]
        public int LogoPictureId { get; set; }
        public bool LogoPictureId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DisplayEuCookieLawWarning")]
        public bool DisplayEuCookieLawWarning { get; set; }
        public bool DisplayEuCookieLawWarning_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.FacebookLink")]
        public string FacebookLink { get; set; }
        public bool FacebookLink_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.TwitterLink")]
        public string TwitterLink { get; set; }
        public bool TwitterLink_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.YoutubeLink")]
        public string YoutubeLink { get; set; }
        public bool YoutubeLink_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.InstagramLink")]
        public string InstagramLink { get; set; }
        public bool InstagramLink_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.SubjectFieldOnContactUsForm")]
        public bool SubjectFieldOnContactUsForm { get; set; }
        public bool SubjectFieldOnContactUsForm_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.UseSystemEmailForContactUsForm")]
        public bool UseSystemEmailForContactUsForm { get; set; }
        public bool UseSystemEmailForContactUsForm_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.PopupForTermsOfServiceLinks")]
        public bool PopupForTermsOfServiceLinks { get; set; }
        public bool PopupForTermsOfServiceLinks_OverrideForStore { get; set; }

        #endregion

        #region Nested classes

        public partial record ThemeModel
        {
            public string SystemName { get; set; }
            public string FriendlyName { get; set; }
            public string PreviewImageUrl { get; set; }
            public string PreviewText { get; set; }
            public bool SupportRtl { get; set; }
            public bool Selected { get; set; }
        }

        #endregion
    }
}