using FluentValidation;
using Nop.Web.Areas.Admin.Models.Orders.PreOrders;
using Nop.Web.Framework.Validators;

namespace Nop.Web.Areas.Admin.Validators.CustomPages
{
    public class PreOrderValidator : BaseNopValidator<PreOrderModel>
    {
        public PreOrderValidator()
        {
            RuleFor(x => x.SelectedCustomerRoleIds)
                .NotEmpty()
                .WithMessage("Customer role is required.");

            RuleFor(x => x.ProductSkus)
                .NotEmpty()
                .WithMessage("At least one product SKU is required.");

            RuleFor(x => x.MinimumLeadTime)
                .GreaterThan(0)
                .WithMessage("Minimum lead time must be greater than 0.");

            RuleFor(x => x.DeliveryEndDate)
                .NotNull()
                .WithMessage("Delivery End Date is required.");

            RuleFor(x => x.ProductMinimumQuantity)
                .GreaterThan(0)
                .WithMessage("Minimum quantity must be greater than 0.");

            RuleFor(x => x.ProductMaximumQuantity)
                .GreaterThanOrEqualTo(x => x.ProductMinimumQuantity)
                .WithMessage("Maximum quantity must be greater than or equal to minimum quantity.");
        }
    }
}
