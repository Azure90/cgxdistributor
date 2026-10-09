using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Orders.PreOrders;
using Nop.Data;

namespace Nop.Services.Orders
{
    public partial class PreOrderService : IPreOrderService
    {
        #region Fields

        private readonly IRepository<PreOrder> _preOrderRepository;
        private readonly IRepository<BlackoutDates> _blackoutDateRepository;
        private readonly IRepository<PreOrderReminderSettings> _preOrderReminderSettingsRepository;

        #endregion

        #region Ctor

        public PreOrderService(IRepository<PreOrder> preOrderRepository,
            IRepository<BlackoutDates> blackoutDateRepository,
            IRepository<PreOrderReminderSettings> preOrderReminderSettingsRepository)
        {
            _preOrderRepository = preOrderRepository;
            _blackoutDateRepository = blackoutDateRepository;
            _preOrderReminderSettingsRepository = preOrderReminderSettingsRepository;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all pre-order configurations.
        /// </summary>
        /// <returns>A list of pre-order configurations.</returns>
        public async Task<IList<PreOrder>> GetAllPreOrdersAsync()
        {
            return await _preOrderRepository.Table.ToListAsync();
        }

        /// <summary>
        /// Gets a paged list of pre-order configurations.
        /// </summary>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>A paged list of pre-order configurations.</returns>
        public async Task<IPagedList<PreOrder>> GetAllPreOrdersAsync(string searchText = null, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _preOrderRepository.Table;

            return await query.ToPagedListAsync(pageIndex, pageSize);
        }

        /// <summary>
        /// Gets a pre-order configuration by identifier.
        /// </summary>
        /// <param name="preOrderId">Pre-order identifier.</param>
        /// <returns>The pre-order configuration if found; otherwise, null.</returns>
        public async Task<PreOrder> GetPreOrderByIdAsync(int preOrderId)
        {
            if (preOrderId <= 0)
                return null;

            return await _preOrderRepository.GetByIdAsync(preOrderId);
        }

        /// <summary>
        /// Inserts a pre-order configuration.
        /// </summary>
        /// <param name="preOrder">The pre-order configuration.</param>
        public async Task InsertPreOrderAsync(PreOrder preOrder)
        {
            if (preOrder == null)
                throw new ArgumentNullException(nameof(preOrder));

            await _preOrderRepository.InsertAsync(preOrder);
        }

        /// <summary>
        /// Inserts multiple pre-order configurations.
        /// </summary>
        /// <param name="preOrders">The pre-order configurations.</param>
        public async Task InsertPreOrdersAsync(IList<PreOrder> preOrders)
        {
            if (preOrders == null)
                throw new ArgumentNullException(nameof(preOrders));

            await _preOrderRepository.InsertAsync(preOrders);
        }

        /// <summary>
        /// Updates a pre-order configuration.
        /// </summary>
        /// <param name="preOrder">The pre-order configuration.</param>
        public virtual async Task UpdatePreOrderAsync(PreOrder preOrder)
        {
            if (preOrder == null)
                throw new ArgumentNullException(nameof(preOrder));

            await _preOrderRepository.UpdateAsync(preOrder);
        }

        /// <summary>
        /// Deletes a pre-order configuration.
        /// </summary>
        /// <param name="preOrder">The pre-order configuration.</param>
        public virtual async Task DeletePreOrderAsync(PreOrder preOrder)
        {
            if (preOrder == null)
                throw new ArgumentNullException(nameof(preOrder));

            await _preOrderRepository.DeleteAsync(preOrder);
        }

        /// <summary>
        /// Clears all pre-order configurations.
        /// </summary>
        public virtual async Task ClearPreOrdersAsync()
        {
            await _preOrderRepository.TruncateAsync(true);
        }


        #region Blackout dates

        /// <summary>
        /// Gets all blackout dates.
        /// </summary>
        /// <returns>A list of blackout dates.</returns>
        public async Task<IList<BlackoutDates>> GetAllBlackoutDatesAsync()
        {
            return await _blackoutDateRepository.Table.ToListAsync();
        }

        /// <summary>
        /// Gets a paged list of blackout dates.
        /// </summary>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>A paged list of blackout dates.</returns>
        public async Task<IPagedList<BlackoutDates>> GetAllBlackoutDatesAsync(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _blackoutDateRepository.Table;

            query = query.OrderBy(x => x.StartDate)
                .ThenByDescending(x => x.CreatedOnUtc);

            return await query.ToPagedListAsync(pageIndex, pageSize);
        }

        /// <summary>
        /// Gets a blackout date by identifier.
        /// </summary>
        /// <param name="blackoutDateId">Blackout date identifier.</param>
        /// <returns>The blackout date if found; otherwise, null.</returns>
        public async Task<BlackoutDates> GetBlackoutDateByIdAsync(int blackoutDateId)
        {
            if (blackoutDateId <= 0)
                return null;

            return await _blackoutDateRepository.GetByIdAsync(blackoutDateId);
        }

        /// <summary>
        /// Inserts a blackout date.
        /// </summary>
        /// <param name="blackoutDate">The blackout date.</param>
        public async Task InsertBlackoutDateAsync(BlackoutDates blackoutDate)
        {
            if (blackoutDate == null)
                throw new ArgumentNullException(nameof(blackoutDate));

            await _blackoutDateRepository.InsertAsync(blackoutDate);
        }

        /// <summary>
        /// Inserts multiple blackout dates.
        /// </summary>
        /// <param name="blackoutDates">The blackout dates.</param>
        public async Task InsertBlackoutDatesAsync(IList<BlackoutDates> blackoutDates)
        {
            if (blackoutDates == null)
                throw new ArgumentNullException(nameof(blackoutDates));

            await _blackoutDateRepository.InsertAsync(blackoutDates);
        }

        /// <summary>
        /// Deletes a blackout date.
        /// </summary>
        /// <param name="blackoutDate">The blackout date.</param>
        public async Task DeleteBlackoutDateAsync(BlackoutDates blackoutDate)
        {
            if (blackoutDate == null)
                throw new ArgumentNullException(nameof(blackoutDate));

            await _blackoutDateRepository.DeleteAsync(blackoutDate);
        }

        /// <summary>
        /// Determines whether the specified date falls within an active blackout period.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns>True if the date is within an active blackout period; otherwise, false.</returns>
        public async Task<bool> IsBlackoutDateAsync(DateTime date)
        {
            return await _blackoutDateRepository.Table.AnyAsync(x =>
                x.IsActive && x.StartDate.Date <= date.Date &&
                x.EndDate.Date >= date.Date);
        }

        #endregion

        #region PreOrder templates

        /// <summary>
        /// Gets the pre-order reminder settings.
        /// </summary>
        /// <returns>The pre-order reminder settings if found; otherwise, null.</returns>
        public virtual async Task<PreOrderReminderSettings> GetPreOrderReminderSettingsAsync()
        {
            return await _preOrderReminderSettingsRepository.Table.FirstOrDefaultAsync();
        }


        /// <summary>
        /// Inserts pre-order reminder settings.
        /// </summary>
        /// <param name="settings">The pre-order reminder settings.</param>
        public virtual async Task InsertPreOrderReminderSettingsAsync(PreOrderReminderSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            await _preOrderReminderSettingsRepository.InsertAsync(settings);
        }

        /// <summary>
        /// Updates pre-order reminder settings.
        /// </summary>
        /// <param name="settings">The pre-order reminder settings.</param>
        public virtual async Task UpdatePreOrderReminderSettingsAsync(PreOrderReminderSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            await _preOrderReminderSettingsRepository.UpdateAsync(settings);
        }

        #endregion

        #endregion
    }
}
