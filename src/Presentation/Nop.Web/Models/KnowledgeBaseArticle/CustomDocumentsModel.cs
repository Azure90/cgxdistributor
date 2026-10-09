using System.Collections.Generic;
using Nop.Web.Framework.Models;
using Nop.Web.Models.Media;

namespace Nop.Web.Models.KnowledgeBaseArticle
{
    public partial record CustomDocumentsModel : BaseNopModel
    {
        public CustomDocumentsModel()
        {
            MainCategories = new List<MainCategoryModel>();
        }

        public IList<MainCategoryModel> MainCategories { get; set; }
    }

    public class MainCategoryModel
    {
        public MainCategoryModel()
        {
            Items = new List<CustomDocumentItemModel>();
        }

        public string MainCategory { get; set; }

        public string MainCategoryClass { get; set; }

        public List<CustomDocumentItemModel> Items { get; set; } = new();
    }

    public class VideoDownloadModel
    {
        public string Url { get; set; }
        public string FileName { get; set; }
    }
    public partial record CustomDocumentItemModel : BaseNopModel
    {
        public int Id { get; set; }

        public string Category { get; set; }

        public string FileName { get; set; }

        public string SubCategory { get; set; }

        public string DownloadLink { get; set; }

        public string VideoLink { get; set; }

        public int DisplayOrder { get; set; }

        public PictureModel Picture { get; set; }
    }
}
