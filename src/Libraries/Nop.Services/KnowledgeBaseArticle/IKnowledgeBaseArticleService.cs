using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.CustomRewardPoint;

namespace Nop.Services.KnowledgeBaseArticle
{
    public partial interface IKnowledgeBaseArticleService
    {

        IList<Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle> GetAllKnowledgeBaseArticle();
        Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle GetKnowledgeBaseArticleById(int promoId);
        void InsertCustomReward(Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle promo);
        void UpdateKnowledgeBaseArticle(Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle promo); 
        void DeleteKnowledgeBaseArticle(Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle promoId);
    }
}
