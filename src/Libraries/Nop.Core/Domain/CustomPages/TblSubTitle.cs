using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.CustomPages
{
    public partial class TblSubTitle : BaseEntity
    {
        /// <summary>
        /// Gets or sets the language identifier
        /// </summary>
        public string LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the sub tittle
        /// </summary>
        public string SubTittle { get; set; }
    }
}
