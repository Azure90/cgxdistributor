using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.CustomPages;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Common
{
    public class CustomPromotionModel : BaseEntity
    {
        public CustomPromotionModel()
        {
            AvailableCategories = new List<SelectListItem>();
            AvailableManufacturers = new List<SelectListItem>();
            AvailableList= new List<CustomPromotionModel>();
            AvailableCustomerRoles = new List<SelectListItem>();
            CustomerRoleid = new List<int>();
            DiscountCustomerRoleId = new List<int>();
            DiscountRoleItemModel = new List<DiscountRoleItems>();
            DisableCartCustomerRoles = new List<SelectListItem>();
            DiscountCustomerRoles = new List<SelectListItem>();
        }

        public int FromRange { get; set; }

        public int ToRange { get; set; }

        public string ProductId { get; set; }

        public string ProductName { get; set; }

        [NopResourceDisplayName("Admin.Customers.Customers.Fields.CustomerRoles")]
        public IList<int> CustomerRoleid { get; set; }

        public IList<int> DisableCartRoleId { get; set; }
        public IList<int> DiscountCustomerRoleId { get; set; }
        public string CustomerRoleName { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public bool IsEnabledCustomMegaMenu { get; set; }

        public bool IsDisabledCartItems { get; set; }

        public string DDlValue { get; set; }

        public int CategoryId { get; set; }

        public int DesignerId { get; set; }

        public int SingleProductId { get; set; }

        public string SingleProductName { get; set; }

        public string CartButtonText { get; set; }
        public string DiscountHeaderText { get; set; }
        public string PGCartButtonText { get; set; }

        public string CatDesName { get; set; }
        [UIHint("DateNullable")]
        public DateTime? PromotionDateFrom { get; set; }

        [UIHint("DateNullable")]
        public DateTime? PromotionDateTo { get; set; }
        public string SelectedOption { get; set; }
        public IList<SelectListItem> AvailableCategories { get; set; }

        public IList<SelectListItem> AvailableManufacturers { get; set; }
        public IList<CustomPromotionModel> AvailableList { get; set; }

        public string MainMenu { get; set; }
        public string DiscountPriceMenu { get; set; }
        public string SpecialPriceMenu { get; set; }
        public string SpecialPriceHeaderText { get; set; }
        public string PromotionBackgroundColor { get; set; }
        public string SavingAmountBackgroundColor { get; set; }
        public IList<SelectListItem> AvailableCustomerRoles { get; set; }
        public IList<SelectListItem> DisableCartCustomerRoles { get; set; }
        public IList<SelectListItem> DiscountCustomerRoles { get; set; }
        // new property for multiple rows
        public IList<DiscountRoleItems> DiscountRoleItemModel { get; set; }
    }
}
