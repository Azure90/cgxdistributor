using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.CustomPage
{
    public partial record ProductsWithBPCodeSearchModel : BaseSearchModel
    {

        #region Properties

        public int Id { get; set; }
        public int ProductId { get; set; }

        #endregion
    }
}
