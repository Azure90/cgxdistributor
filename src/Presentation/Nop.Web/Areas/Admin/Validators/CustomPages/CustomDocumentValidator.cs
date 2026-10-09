using FluentValidation;
using Nop.Web.Areas.Admin.Models.CustomPage;
using Nop.Web.Framework.Validators;

namespace Nop.Web.Areas.Admin.Validators.CustomPages
{
    public class CustomDocumentValidator : BaseNopValidator<CustomDocumentsModel>
    {
        public CustomDocumentValidator()
        {
            RuleFor(x => x.MainCategory)
                .NotEmpty()
                .WithMessage("Main Category is required.")
                .MaximumLength(200);

            RuleFor(x => x.SubCategory)
                .MaximumLength(200);

            RuleFor(x => x.Category)
                .MaximumLength(200);

            RuleFor(x => x.FileName)
                .NotEmpty()
                .WithMessage("File Name is required.")
                .MaximumLength(510);

            RuleFor(x => x.DownloadLink)
                .NotEmpty()
                .WithMessage("Download Link is required.");

            RuleFor(x => x.BPCode)
                .NotEmpty()
                .WithMessage("BP Code is required.")
                .MaximumLength(100);

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Display Order must be greater than or equal to 0.");
        }
    }
}
