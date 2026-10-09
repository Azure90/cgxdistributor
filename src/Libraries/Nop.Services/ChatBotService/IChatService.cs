using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain;
using Nop.Core.Domain.ProductEmbeddings;

namespace Nop.Services.ChatBotService
{
    public interface IChatService
    {
        Task AddOrUpdateAsync(int productId,int IsSample, string textData, string embeddingJson, string url);
        Task<List<ProductEmbeddings>> GetAllEmbeddingsAsync();
        //Task<ProductEmbedding?> GetByProductIdAsync(int productId);
        Task RebuildAllProductEmbeddingsAsync(); // optional, for rebuilding                                            
    }
}
