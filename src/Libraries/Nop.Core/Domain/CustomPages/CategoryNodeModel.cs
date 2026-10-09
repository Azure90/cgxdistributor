
namespace Nop.Core.Domain.CustomPages
{
    public class CategoryNodeModel
    {
        public string ParentCategory { get; set; }
        public string SubCategory { get; set; }
        public string ChildCategory { get; set; }
        public int? ParentCategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public int? ChildCategoryId { get; set; }
        public string SeName { get; set; }
    }
}
