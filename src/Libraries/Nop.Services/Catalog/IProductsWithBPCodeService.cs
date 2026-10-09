using Nop.Data;
using Nop.Core.Domain.CustomPages;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Products With BPCode
    /// </summary>

    public interface IProductsWithBPCodeService
    {
        /// <summary>
        /// Gets all records from the ProductsWithBPCode table.
        /// This table defines product visibility rules per customer using a comma-separated customer ID list.
        /// </summary>
        /// <returns>
        /// A list of <see cref="ProductsWithBPCode"/> entries.
        /// </returns>
        Task<IList<ProductsWithBPCode>> GetAllProductWithBPCodeAsync();

        /// <summary>
        /// Returns product IDs that are blocked for a given BPCode.
        /// If a product contains this BPCode, it will be hidden.
        /// </summary>
        /// <param name="bpCode">Customer BPCode</param>
        /// <param name="productId">Optional product filter</param>
        /// <returns>Blocked product IDs</returns>
        Task<List<int>> GetBlockedProductIdsForCustomerAsync(string bpCode, int? productId = null);

        /// <summary>
        /// Gets a ProductsWithBPCode record by its identifier.
        /// </summary>
        /// <param name="id">The record identifier.</param>
        /// <returns>
        /// The matching <see cref="ProductsWithBPCode"/> entity if found; otherwise null.
        /// </returns>
        Task<ProductsWithBPCode> GetByIdAsync(int id);

        /// <summary>
        /// Inserts a new ProductsWithBPCode record that defines
        /// which customers can see a specific product.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        Task InsertAsync(ProductsWithBPCode entity);

        /// <summary>
        /// Updates an existing ProductsWithBPCode record.
        /// Used when modifying the allowed customer list for a product.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        Task UpdateAsync(ProductsWithBPCode entity);

        /// <summary>
        /// Deletes a ProductsWithBPCode record.
        /// After deletion, the product becomes visible to all customers.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        Task DeleteAsync(ProductsWithBPCode entity);
    }
}
