using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.ProductEmbeddings
{
    public class ProductEmbeddings : BaseEntity
    {

        public int Id { get; set; }


        public int ProductId { get; set; }
        public int IsSample { get; set; }


        public string TextData { get; set; } = string.Empty; // Product name + description


        public string Embedding { get; set; } = string.Empty; // JSON array of floats

        public string Url { get; set; } = string.Empty; // JSON array of floats


        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
