using System.Threading.Tasks;
using Nop.Core.Domain.CustomPages;
using Nop.Data;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Data;
using Nop.Services.Logging;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Nop.Services.Catalog
{
    public class ProductsWithBPCodeService : IProductsWithBPCodeService
    {
        #region Fields    

        protected readonly ILogger _logger;
        protected readonly IRepository<ProductsWithBPCode> _repository;

        private readonly string _connectionString;

        #endregion

        #region Ctor
        public ProductsWithBPCodeService(ILogger logger,
            IRepository<ProductsWithBPCode> repository)
        {
            _logger = logger;
            _repository = repository;
            _connectionString = DataSettingsManager.LoadSettings().ConnectionString;
        }
        #endregion

        #region Methods   

        /// <summary>
        /// Gets all records from the ProductsWithBPCode table.
        /// This table defines product visibility rules per customer using a comma-separated customer ID list.
        /// </summary>
        /// <returns>
        /// A list of <see cref="ProductsWithBPCode"/> entries.
        /// </returns>
        public async Task<IList<ProductsWithBPCode>> GetAllProductWithBPCodeAsync()
        {
            return await _repository.GetAllAsync(q => q.OrderByDescending(x => x.Id));
        }

        /// <summary>
        /// Gets a ProductsWithBPCode record by its identifier.
        /// </summary>
        /// <param name="id">The record identifier.</param>
        /// <returns>
        /// The matching <see cref="ProductsWithBPCode"/> entity if found; otherwise null.
        /// </returns>
        public virtual async Task<ProductsWithBPCode> GetByProductIdAsync(int productId)
        {
            if (productId <= 0)
                return null;

            return await _repository.Table
                .Where(x => x.ProductId == productId).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a ProductsWithBPCode record by its identifier.
        /// </summary>
        /// <param name="id">The record identifier.</param>
        /// <returns>
        /// The matching <see cref="ProductsWithBPCode"/> entity if found; otherwise null.
        /// </returns>
        public async Task<ProductsWithBPCode> GetByIdAsync(int id)
        {
            if (id <= 0)
                return null;

            return await _repository.GetByIdAsync(id, cache => default);
        }

        /// <summary>
        /// Inserts a new ProductsWithBPCode record that defines
        /// which customers can see a specific product.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        public async Task InsertAsync(ProductsWithBPCode entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _repository.InsertAsync(entity);
        }

        /// <summary>
        /// Updates an existing ProductsWithBPCode record.
        /// Used when modifying the allowed customer list for a product.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        public async Task UpdateAsync(ProductsWithBPCode entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _repository.UpdateAsync(entity);
        }

        /// <summary>
        /// Deletes a ProductsWithBPCode record.
        /// After deletion, the product becomes visible to all customers.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        public virtual async Task DeleteAsync(ProductsWithBPCode entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _repository.DeleteAsync(entity);
        }


        #endregion

        #region Utilities

        /// <summary>
        /// Returns product IDs that are blocked for a given BPCode.
        /// If a product contains this BPCode, it will be hidden.
        /// </summary>
        /// <param name="bpCode">Customer BPCode</param>
        /// <param name="productId">Optional product filter</param>
        /// <returns>Blocked product IDs</returns>
        public async Task<List<int>> GetBlockedProductIdsForCustomerAsync(string bpCode, int? productId = null)
        {
            if (string.IsNullOrWhiteSpace(bpCode))
                return new List<int>(); // fail safe

            const string sql = @"
            SELECT r.ProductId
            FROM ProductsWithBPCode r
            WHERE
                (@ProductId IS NULL OR r.ProductId = @ProductId)
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM STRING_SPLIT(r.CustomerBPCodes, ',') s
                    WHERE LTRIM(RTRIM(s.value)) = @BPCode
                );";

            bpCode = bpCode?.Trim();

            try
            {
                using var connection = new SqlConnection(_connectionString);

                var result = await connection.QueryAsync<int>(sql, new
                {
                    BPCode = bpCode,
                    ProductId = productId
                });

                return result.ToList();
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Failed to get restricted products for BPCode {bpCode}.", ex);

                return new List<int>(); // fail safe
            }
        }

        #endregion

    }
}
