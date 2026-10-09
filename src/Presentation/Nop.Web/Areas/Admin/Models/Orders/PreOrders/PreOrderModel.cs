using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Orders.PreOrders
{
    /// <summary>
    /// Represents a pre-order configuration model.
    /// </summary>
    public partial record PreOrderModel : BaseNopEntityModel
    {

        #region Ctor 

        public PreOrderModel()
        {
            SelectedCustomerRoleIds = new List<int>();
            AvailableCustomerRoles = new List<SelectListItem>();
        }

        #endregion

        public bool IsPreOrderEnabled { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.CustomerRoleIds")]
        public string CustomerRoles { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.ProductIds")]
        public string ProductIds { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.ProductSkus")]
        public string ProductSkus { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.CategoryIds")]
        public string CategoryIds { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.MinimumLeadTime")]
        public int MinimumLeadTime { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.AllowedDeliveryDays")]
        public int AllowedDeliveryDays { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.BlackoutDates")]
        public string BlackoutDates { get; set; }

        [UIHint("DateNullable")]
        [NopResourceDisplayName("Admin.PreOrders.Fields.DeliveryEndDate")]
        public DateTime? DeliveryEndDate { get; set; }

        [UIHint("DateNullable")]
        [NopResourceDisplayName("Admin.PreOrders.Fields.EndDate")]
        public DateTime? EndDate { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.ProductMinimumQuantity")]
        public int ProductMinimumQuantity { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.ProductMaximumQuantity")]
        public int ProductMaximumQuantity { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.CreatedOnUtc")]
        public DateTime CreatedOnUtc { get; set; }

        [NopResourceDisplayName("Admin.PreOrders.Fields.UpdatedOnUtc")]
        public DateTime? UpdatedOnUtc { get; set; }

        public BlackoutDateModel BlackoutModel { get; set; } = new();

        public PreOrderReminderSettingsModel ReminderSettings { get; set; } = new();

        // For the DataTable
        public BlackoutDateSearchModel BlackoutDateSearchModel { get; set; } = new();

        public PreOrderResourcesModel ResourcesModel { get; set; } = new();

        public bool IncludeExcludeProducts { get; set; }
        public IList<int> SelectedCustomerRoleIds { get; set; }
        public IList<SelectListItem> AvailableCustomerRoles { get; set; }
    }
}
