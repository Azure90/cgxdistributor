using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.CustomPages;
using Nop.Data;

namespace Nop.Services.KnowledgeBaseArticle
{
    public class CustomDocumentService : ICustomDocumentService
    {
        #region Fields

        private readonly IRepository<CustomDocuments> _documentRepository;

        #endregion

        #region Ctor

        public CustomDocumentService(IRepository<CustomDocuments> documentRepository)
        {
            _documentRepository = documentRepository;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all custom documents.
        /// </summary>
        /// <returns>A list of custom documents.</returns>
        public async Task<IList<CustomDocuments>> GetAllCustomDocumentsAsync()
        {
            return await _documentRepository.Table.ToListAsync();
        }

        /// <summary>
        /// Gets a paged list of custom documents.
        /// </summary>
        /// <param name="searchText">Global search text.</param>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>A paged list of custom documents.</returns>
        public async Task<IPagedList<CustomDocuments>> GetAllCustomDocumentsAsync(string searchText = null,
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _documentRepository.Table;

            // Global search
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                // search keyword
                query = query.Where(x => x.MainCategory.Contains(searchText) ||
                    x.SubCategory.Contains(searchText) || x.Category.Contains(searchText) ||
                    x.FileName.Contains(searchText) || x.BPCode.Contains(searchText));
            }

            query = query.OrderBy(x => x.DisplayOrder);

            return await query.ToPagedListAsync(pageIndex, pageSize);
        }

        /// <summary>
        /// Gets a custom document by identifier.
        /// </summary>
        /// <param name="docId">Document identifier.</param>
        /// <returns>The custom document if found; otherwise, null.</returns>
        public async Task<CustomDocuments> GetCustomDocumentByIdAsync(int docId)
        {
            if (docId <= 0)
                return null;

            return await _documentRepository.GetByIdAsync(docId);
        }

        /// <summary>
        /// Determines whether any custom document exists for the specified BP Code.
        /// </summary>
        /// <param name="bpCode">Customer BP Code.</param>
        /// <returns>
        /// <c>true</c> if a matching document exists; otherwise, <c>false</c>.
        /// </returns>
        public virtual async Task<bool> IsCustomDocumentAvailableForBpCodeAsync(string bpCode)
        {
            if (string.IsNullOrWhiteSpace(bpCode))
                return false;

            bpCode = bpCode.Trim();

            return await _documentRepository.Table
                .AnyAsync(x => x.BPCode == bpCode);
        }

        /// <summary>
        /// Gets all custom documents for the specified BP Code.
        /// </summary>
        /// <param name="bpCode">The customer's BP Code.</param>
        /// <returns>A list of custom documents associated with the specified BP Code.</returns>
        public virtual async Task<IList<CustomDocuments>> GetAllCustomDocumentsByBpCodeAsync(string bpCode)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(bpCode))
                return new List<CustomDocuments>();

            bpCode = bpCode.Trim();

            // Get all documents matching the specified BP Code
            return await _documentRepository.Table
                .Where(x => x.BPCode == bpCode)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Inserts a custom document.
        /// </summary>
        /// <param name="document">The custom document.</param>
        public async Task InsertCustomDocumentAsync(CustomDocuments document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            await _documentRepository.InsertAsync(document);
        }

        /// <summary>
        /// Inserts custom documents.
        /// </summary>
        /// <param name="documents">List of custom documents.</param>
        public async Task InsertCustomDocumentsAsync(List<CustomDocuments> documents)
        {
            if (documents == null)
                throw new ArgumentNullException(nameof(documents));

            await _documentRepository.InsertAsync(documents);
        }

        /// <summary>
        /// Updates a custom document.
        /// </summary>
        /// <param name="document">The custom document.</param>
        public virtual async Task UpdateCustomDocumentAsync(CustomDocuments document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            await _documentRepository.UpdateAsync(document);
        }

        /// <summary>
        /// Deletes a custom document.
        /// </summary>
        /// <param name="document">The custom document.</param>
        public virtual async Task DeleteCustomDocumentAsync(CustomDocuments document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            await _documentRepository.DeleteAsync(document);
        }

        /// <summary>
        /// Clears all customer documents from the table (truncates the table).
        /// </summary>
        public virtual async Task ClearCustomDocumentsAsync()
        {
            await _documentRepository.TruncateAsync(true);
        }

        #endregion
    }
}
