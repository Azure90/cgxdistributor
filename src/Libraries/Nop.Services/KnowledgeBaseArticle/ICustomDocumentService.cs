using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.CustomPages;

namespace Nop.Services.KnowledgeBaseArticle
{
    public interface ICustomDocumentService
    {
        /// <summary>
        /// Gets all custom documents.
        /// </summary>
        /// <returns>A list of custom documents.</returns>
        Task<IList<CustomDocuments>> GetAllCustomDocumentsAsync();

        /// <summary>
        /// Gets a paged list of custom documents.
        /// </summary>
        /// <param name="searchText">Global search text.</param>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>A paged list of custom documents.</returns>
        Task<IPagedList<CustomDocuments>> GetAllCustomDocumentsAsync(string searchText = null,
            int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets a custom document by identifier.
        /// </summary>
        /// <param name="docId">Document identifier.</param>
        /// <returns>The custom document if found; otherwise, null.</returns>
        Task<CustomDocuments> GetCustomDocumentByIdAsync(int docId);

        /// <summary>
        /// Determines whether any custom document exists for the specified BP Code.
        /// </summary>
        /// <param name="bpCode">Customer BP Code.</param>
        /// <returns>
        /// <c>true</c> if a matching document exists; otherwise, <c>false</c>.
        /// </returns>
        Task<bool> IsCustomDocumentAvailableForBpCodeAsync(string bpCode);

        /// <summary>
        /// Gets all custom documents for the specified BP Code.
        /// </summary>
        /// <param name="bpCode">The customer's BP Code.</param>
        /// <returns>A list of custom documents associated with the specified BP Code.</returns>
        Task<IList<CustomDocuments>> GetAllCustomDocumentsByBpCodeAsync(string bpCode);

        /// <summary>
        /// Inserts a custom document.
        /// </summary>
        /// <param name="document">The custom document.</param>
        Task InsertCustomDocumentAsync(CustomDocuments document);

        /// <summary>
        /// Inserts custom documents.
        /// </summary>
        /// <param name="documents">List of custom documents.</param>
        Task InsertCustomDocumentsAsync(List<CustomDocuments> documents);

        /// <summary>
        /// Updates a custom document.
        /// </summary>
        /// <param name="document">The custom document.</param>
        Task UpdateCustomDocumentAsync(CustomDocuments document);

        /// <summary>
        /// Deletes a custom document.
        /// </summary>
        /// <param name="document">The custom document.</param>
        Task DeleteCustomDocumentAsync(CustomDocuments document);

        /// <summary>
        /// Clears all customer documents from the table (truncates the table).
        /// </summary>
        Task ClearCustomDocumentsAsync();
    }
}
