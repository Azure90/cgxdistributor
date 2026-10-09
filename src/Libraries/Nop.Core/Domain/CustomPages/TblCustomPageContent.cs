using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.CustomPages
{
    public partial class TblCustomPageContent : BaseEntity
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
        /// Gets or sets the question
        /// </summary>
        public string Question { get; set; }

        /// <summary>
        /// Gets or sets the answer
        /// </summary>
        public string Answer { get; set; }

        /// <summary>
        /// Gets or sets the is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the custom page identifier
        /// </summary>
        public int TblcustomPageId { get; set; }

        /// <summary>
        /// Gets or sets the sub title identifier
        /// </summary>
        public int SubTitleID { get; set; }

        /// <summary>
        /// Gets or sets the display
        /// </summary>
        public int Display { get; set; }
    }
}
