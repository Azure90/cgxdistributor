using System;
using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Orders.PreOrders
{
    /// <summary>
    /// Represents a blackout date model.
    /// </summary>
    public record BlackoutDateModel : BaseNopEntityModel
    {
        [UIHint("DateNullable")]
        public DateTime? StartDate { get; set; }

        [UIHint("DateNullable")]
        public DateTime? EndDate { get; set; }

        public string Reason { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedOnUtc { get; set; }
    }
}
