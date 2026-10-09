using Nop.Web.Models.Catalog;

namespace Nop.Web.Models.CustomDiscount
{
    public class DiscountAndSpecialProductsModel
    {
        public string Type { get; set; }
        public string BpCode { get; set; }
        public string DiscountIds { get; set; }
        public string CustomerRoleIds { get; set; }
        public CategoryNavigationModel CategoryNavigation { get; set; }
        public CatalogProductsModel CatalogProducts { get; set; }
    }
}
