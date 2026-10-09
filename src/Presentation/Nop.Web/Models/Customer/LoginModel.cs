using System.ComponentModel.DataAnnotations;
using Nop.Core.Domain.Customers;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Nop.Web.Models.Customer
{
    public partial record LoginModel : BaseNopModel
    {
        public bool CheckoutAsGuest { get; set; }

        [DataType(DataType.EmailAddress)]
        [NopResourceDisplayName("Account.Login.Fields.Email")]
        public string Email { get; set; }

        public bool UsernamesEnabled { get; set; }
        public bool SuccessfullySent { get; set; }

        public UserRegistrationType RegistrationType { get; set; }

        [NopResourceDisplayName("Account.Login.Fields.Username")]
        public string Username { get; set; }

        [DataType(DataType.Password)]
        [NopResourceDisplayName("Account.Login.Fields.Password")]
        public string Password { get; set; }

        [NopResourceDisplayName("Account.Login.Fields.RememberMe")]
        public bool RememberMe { get; set; }

        public bool DisplayCaptcha { get; set; }

        public string firstlogin { get; set; }
        public string GUIDCustomer { get; set; }
        public string FullName { get; set; }
        public IList<SelectListItem> countrylist { get; set; }
        public string Subject { get; set; }
        public string country { get; set; }
        public string Enquiry { get; set; }

    }
}