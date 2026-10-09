using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.ChatBotHistory;

namespace Nop.Services.ChatBotHistory
{
    public partial interface Ichatbothistory
    {
        IList<Nop.Core.Domain.ChatBotHistory.chatbothistory> GetAllChatbothistory();
        Nop.Core.Domain.ChatBotHistory.chatbothistory GetChatbothistoryById(int historyid);
        void InsertChatbothistory(Nop.Core.Domain.ChatBotHistory.chatbothistory promo);
        void UpdateChatBotHistory(Nop.Core.Domain.ChatBotHistory.chatbothistory promo);
        void DeleteChatBotHistory(Nop.Core.Domain.ChatBotHistory.chatbothistory promo);
    }
}
