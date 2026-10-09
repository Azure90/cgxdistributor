using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.CustomPage
{
    public record CustomDocumentsModel : BaseNopEntityModel
    {

        #region Ctor 

        public CustomDocumentsModel()
        {
            AvailableCustomers = new List<SelectListItem>();
        }

        #endregion

        public string MainCategory { get; set; }

        public string FileName { get; set; }

        public string Category { get; set; }

        public string SubCategory { get; set; }

        public string BPCode { get; set; }

        public string DownloadLink { get; set; }

        public string VideoLink { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        public bool ActiveAccountMenu { get; set; }

        public bool ActiveMegaMenu { get; set; }

        public string MegaMenuLabel { get; set; }
        public IList<SelectListItem> AvailableCustomers { get; set; }

    }
}
