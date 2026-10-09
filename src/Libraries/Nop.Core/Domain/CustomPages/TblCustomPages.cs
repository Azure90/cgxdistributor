using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.CustomPages
{
    public partial class TblCustomPages : BaseEntity
    {
        /// <summary>
        /// Gets or sets the language identifier
        /// </summary>
        public string LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the tittle
        /// </summary>
        public string Tittle { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the image
        /// </summary>
        public string Image { get; set; }
    }
}
