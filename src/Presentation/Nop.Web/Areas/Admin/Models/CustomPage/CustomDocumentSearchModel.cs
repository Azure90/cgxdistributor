using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.CustomPage
{
    public partial record CustomDocumentSearchModel : BaseSearchModel
    {
        public string SearchText { get; set; }
        public bool ActiveAccountMenu { get; set; }

        public bool ActiveMegaMenu { get; set; }

        public string MegaMenuLabel { get; set; }
    }
}
