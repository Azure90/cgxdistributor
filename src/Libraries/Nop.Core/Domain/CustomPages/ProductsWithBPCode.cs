using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.CustomPages
{
    public class ProductsWithBPCode : BaseEntity
    {
        public int ProductId { get; set; }

        public string CustomerBPCodes { get; set; } = string.Empty;
    }
}
