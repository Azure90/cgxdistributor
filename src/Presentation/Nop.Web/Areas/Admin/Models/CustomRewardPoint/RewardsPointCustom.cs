using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Web.Framework.Models;
namespace Nop.Web.Areas.Admin.Models.CustomRewardPoint
{
    public partial record RewardsPointCustom : BaseNopEntityModel
    {
        public string WeekDays { get; set; }
        public string SelectedType { get; set; }
        public int RewardPoints { get; set; }
        public string CategoryIds { get; set; }
        public string BrandIds { get; set; }
        public string ProductIds { get; set; }
        public string StartWeek { get; set; }
        public string EndWeek { get; set; }
        public string ProductName { get; set; }
        public bool Active { get; set; }

        public string DDlValue { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string BrandName { get; set; }
        public int DesignerId { get; set; }
        public int ddlCategory { get; set; }
        public int SingleProductId { get; set; }

        public string SingleProductName { get; set; }
        public IList<int> CustomerRoleid { get; set; }
        public string CustomerRoleName { get; set; }
        public string CatDesName { get; set; }
        public List<RewardsPointCustom> ModelList { get; set; }
        public IList<SelectListItem> AvailableCategories { get; set; }
        public IList<SelectListItem> AvailableCustomerRoles { get; set; }
        public IList<SelectListItem> AvailableManufacturers { get; set; }
        public RewardsPointCustom()
        {
            AvailableCategories = new List<SelectListItem>();
            AvailableManufacturers = new List<SelectListItem>();
            ModelList = new List<RewardsPointCustom>();
            AvailableCustomerRoles = new List<SelectListItem>();
            CustomerRoleid = new List<int>();
        }
    }
}
