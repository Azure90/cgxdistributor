using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.ChatBotHistory;
using Nop.Core.Domain.CustomRewardPoint;
using Nop.Data;

namespace Nop.Services.ChatBotHistory
{
    public class chatbothistory : Ichatbothistory
    {
        private readonly IRepository<Nop.Core.Domain.ChatBotHistory.chatbothistory> _ChatBotHistoryRepository;


        public chatbothistory(IRepository<Nop.Core.Domain.ChatBotHistory.chatbothistory> ChatBotHistoryRepository)
        {
            _ChatBotHistoryRepository = ChatBotHistoryRepository;
        }

        public IList<Nop.Core.Domain.ChatBotHistory.chatbothistory> GetAllChatbothistory()
        {
            var query = _ChatBotHistoryRepository.Table;

            //if (IsPriceRange)
            //    query = query.Where(x => x.IsPriceRange == IsPriceRange);

            //if (IsShoppingItem)
            //    query = query.Where(x => x.IsShoppingItem == IsShoppingItem);

            //query = query.OrderBy(v => v.Id).ThenBy(v => v.Id);
            //var promo = new PagedList<ChatBotHistory>(query.ToList(), pageIndex, pageSize);
            return query.ToList();
        }

        /// <summary>
        /// Gets a ChatBotHistory with id identifier 
        /// </summary>
        /// <param name="promoId">Identifier</param>
        /// <returns>
        /// Returns a ChatBotHistory
        /// </returns>
        public Nop.Core.Domain.ChatBotHistory.chatbothistory GetChatbothistoryById(int historyid)
        {
            if (historyid == 0)
                return null;

            return _ChatBotHistoryRepository.GetById(historyid);
        }


        /// <summary>
        /// Insert a ChatBotHistory
        /// </summary>
        /// <param name="promo">ChatBotHistory</param>
        public void InsertChatbothistory(Nop.Core.Domain.ChatBotHistory.chatbothistory promo)
        {

            if (promo == null)
                throw new ArgumentNullException("ChatBotHistory");

            _ChatBotHistoryRepository.Insert(promo);


        }


        /// <summary>
        /// Update a ChatBotHistory
        /// </summary>
        /// <param name="promo">ChatBotHistory</param>
        public void UpdateChatBotHistory(Nop.Core.Domain.ChatBotHistory.chatbothistory promo)
        {
            if (promo == null)
                throw new ArgumentNullException("ChatBotHistory");

            _ChatBotHistoryRepository.Update(promo);
        }


        /// <summary>
        /// Delete a ChatBotHistory
        /// </summary>
        /// <param name="promo">ChatBotHistory</param>
        public void DeleteChatBotHistory(Nop.Core.Domain.ChatBotHistory.chatbothistory promo)
        {
            if (promo == null)
                throw new ArgumentNullException("ChatBotHistory");

            _ChatBotHistoryRepository.Delete(promo);
        }

    }
}
