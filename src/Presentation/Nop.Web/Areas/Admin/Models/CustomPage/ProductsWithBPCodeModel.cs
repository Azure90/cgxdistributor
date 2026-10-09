using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.CustomPage
{
    public partial record ProductsWithBPCodeModel : BaseNopEntityModel
    {
        #region Ctor

        public ProductsWithBPCodeModel() 
        {
            SelectedCustomerBpCodes = new List<string>();
            AvailableCustomers = new List<SelectListItem>();
        }    

        #endregion

        public int? ProductId { get; set; }
        public string ProductName { get; set; }
        public string Sku { get; set; }
        public string BpCode { get; set; }
        public string Email { get; set; }
        public IList<string> SelectedCustomerBpCodes { get; set; }
        public IList<SelectListItem> AvailableCustomers { get; set; }
    }
}
