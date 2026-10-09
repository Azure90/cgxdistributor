using System;

namespace Nop.Core.Domain.CustomPages
{
    public class CustomDocuments : BaseEntity
    {
        public string MainCategory { get; set; }

        public string FileName { get; set; }

        public string Category { get; set; }

        public string SubCategory { get; set; }

        public string BPCode { get; set; }

        public string DownloadLink { get; set; }

        public string VideoLink { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime? UpdatedOnUtc { get; set; }
    }
}
