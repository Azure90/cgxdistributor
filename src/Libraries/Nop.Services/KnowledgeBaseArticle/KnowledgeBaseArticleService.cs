using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Core.Domain.KnowledgeBaseArticle;
using Nop.Core.Domain.CustomRewardPoint;

namespace Nop.Services.KnowledgeBaseArticle
{
    public partial class KnowledgeBaseArticleService: IKnowledgeBaseArticleService
    {
        private readonly IRepository<Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle > _KnowledgeBaseArticleRepository;


        public KnowledgeBaseArticleService(IRepository<Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle> KnowledgeBaseArticleRepository)
        {
            _KnowledgeBaseArticleRepository = KnowledgeBaseArticleRepository;
        }
        public IList<Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle> GetAllKnowledgeBaseArticle()
        { 
            var query = _KnowledgeBaseArticleRepository.Table;

            //if (IsPriceRange)
            //    query = query.Where(x => x.IsPriceRange == IsPriceRange);

            //if (IsShoppingItem)
            //    query = query.Where(x => x.IsShoppingItem == IsShoppingItem);

            //query = query.OrderBy(v => v.Id).ThenBy(v => v.Id);
            //var promo = new PagedList<CustomKnowledgeBaseArticle>(query.ToList(), pageIndex, pageSize);
            return query.ToList();
        }

        /// <summary>
        /// Gets a CustomReward with id identifier 
        /// </summary>
        /// <param name="promoId">Identifier</param>
        /// <returns>
        /// Returns a CustomReward
        /// </returns>
        public Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle GetKnowledgeBaseArticleById(int promoId)
        {
            if (promoId == 0)
                return null;

            return _KnowledgeBaseArticleRepository.GetById(promoId);
        }


        /// <summary>
        /// Insert a CustomReward
        /// </summary>
        /// <param name="promo">KnowledgeBaseArticle</param>
        public void InsertCustomReward(Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle promo)
        {
            if (promo == null)
                throw new ArgumentNullException("KnowledgeBaseArticle");

            _KnowledgeBaseArticleRepository.Insert(promo);
        }


        /// <summary>
        /// Update a CustomReward
        /// </summary>
        /// <param name="promo">KnowledgeBaseArticle</param>
        public void UpdateKnowledgeBaseArticle(Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle promo)
        {
            if (promo == null)
                throw new ArgumentNullException("KnowledgeBaseArticle");

            _KnowledgeBaseArticleRepository.Update(promo);
        }


        /// <summary>
        /// Delete a KnowledgeBaseArticle
        /// </summary>
        /// <param name="promo">KnowledgeBaseArticle</param>
        public void DeleteKnowledgeBaseArticle(Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle promo)
        {
            if (promo == null)
                throw new ArgumentNullException("KnowledgeBaseArticle");




            _KnowledgeBaseArticleRepository.Delete(promo);
        }
    }
}
