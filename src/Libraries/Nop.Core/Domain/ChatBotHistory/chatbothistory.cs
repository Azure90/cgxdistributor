using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.ChatBotHistory
{
    public class chatbothistory : BaseEntity
    {
        public int CustID { get; set; }
        public string question { get; set; }
        public string answer { get; set; }
        public bool IsAI { get; set; }
        public bool Isregistered { get; set; }
        public DateTime? DateandTime { get; set; }

    }
}
