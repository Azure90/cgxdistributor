using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;

namespace Nop.Web.Models.Common
{
    public partial record ContactUsModel : BaseNopModel
    {
        [DataType(DataType.EmailAddress)]
        [NopResourceDisplayName("ContactUs.Email")]
        public string Email { get; set; }

        [NopResourceDisplayName("ContactUs.Subject")]
        public string Subject { get; set; }
        public bool SubjectEnabled { get; set; }

        [NopResourceDisplayName("ContactUs.Enquiry")]
        public string Enquiry { get; set; }

        [NopResourceDisplayName("ContactUs.FullName")]
        public string FullName { get; set; }

        public bool SuccessfullySent { get; set; }
        public string Result { get; set; }
        public List<String> Specialty { get; set; }
        public IList<SelectListItem> Salutationlist { get; set; }
        public IList<SelectListItem> countrylist { get; set; }
        public IList<SelectListItem> PrefferedLanguageList { get; set; }
        public bool DisplayCaptcha { get; set; }
        public string Salutation { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FieldSpecialty { get; set; }
        public string country { get; set; }
        public string ZipCode { get; set; }
        public string Preferredlanguage { get; set; }

        public ContactUsModel()
        {
          
            countrylist = new List<SelectListItem>();
         
        }
    }
}