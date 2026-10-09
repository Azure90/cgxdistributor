using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;
using System;
using Nop.Core.Domain.Orders.PreOrders;

namespace Nop.Services.Orders
{
    public partial interface IPreOrderService
    {
        /// <summary>
        /// Gets all pre-order configurations.
        /// </summary>
        /// <returns>A list of pre-order configurations.</returns>
        Task<IList<PreOrder>> GetAllPreOrdersAsync();

        /// <summary>
        /// Gets a paged list of pre-order configurations.
        /// </summary>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>A paged list of pre-order configurations.</returns>
        Task<IPagedList<PreOrder>> GetAllPreOrdersAsync(string searchText = null,
            int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets a pre-order configuration by identifier.
        /// </summary>
        /// <param name="preOrderId">Pre-order identifier.</param>
        /// <returns>The pre-order configuration if found; otherwise, null.</returns>
        Task<PreOrder> GetPreOrderByIdAsync(int preOrderId);

        /// <summary>
        /// Inserts a pre-order configuration.
        /// </summary>
        /// <param name="preOrder">The pre-order configuration.</param>
        Task InsertPreOrderAsync(PreOrder preOrder);

        /// <summary>
        /// Inserts multiple pre-order configurations.
        /// </summary>
        /// <param name="preOrders">The pre-order configurations.</param>
        Task InsertPreOrdersAsync(IList<PreOrder> preOrders);

        /// <summary>
        /// Updates a pre-order configuration.
        /// </summary>
        /// <param name="preOrder">The pre-order configuration.</param>
        Task UpdatePreOrderAsync(PreOrder preOrder);

        /// <summary>
        /// Deletes a pre-order configuration.
        /// </summary>
        /// <param name="preOrder">The pre-order configuration.</param>
        Task DeletePreOrderAsync(PreOrder preOrder);

        /// <summary>
        /// Deletes all pre-order configurations.
        /// </summary>
        Task ClearPreOrdersAsync();


        #region Blackout dates

        /// <summary>
        /// Gets all blackout dates.
        /// </summary>
        /// <returns>A list of blackout dates.</returns>
        Task<IList<BlackoutDates>> GetAllBlackoutDatesAsync();

        /// <summary>
        /// Gets a paged list of blackout dates.
        /// </summary>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>A paged list of blackout dates.</returns>
        Task<IPagedList<BlackoutDates>> GetAllBlackoutDatesAsync(int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets a blackout date by identifier.
        /// </summary>
        /// <param name="blackoutDateId">Blackout date identifier.</param>
        /// <returns>The blackout date if found; otherwise, null.</returns>
        Task<BlackoutDates> GetBlackoutDateByIdAsync(int blackoutDateId);

        /// <summary>
        /// Inserts a blackout date.
        /// </summary>
        /// <param name="blackoutDate">The blackout date.</param>
        Task InsertBlackoutDateAsync(BlackoutDates blackoutDate);

        /// <summary>
        /// Inserts multiple blackout dates.
        /// </summary>
        /// <param name="blackoutDates">The blackout dates.</param>
        Task InsertBlackoutDatesAsync(IList<BlackoutDates> blackoutDates);

        /// <summary>
        /// Deletes a blackout date.
        /// </summary>
        /// <param name="blackoutDate">The blackout date.</param>
        Task DeleteBlackoutDateAsync(BlackoutDates blackoutDate);

        /// <summary>
        /// Determines whether the specified date falls within an active blackout period.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns>True if the date is within an active blackout period; otherwise, false.</returns>
        Task<bool> IsBlackoutDateAsync(DateTime date);

        #endregion

        #region PreOrder Templates

        /// <summary>
        /// Gets the pre-order reminder settings.
        /// </summary>
        /// <returns>The pre-order reminder settings if found; otherwise, null.</returns>
        Task<PreOrderReminderSettings> GetPreOrderReminderSettingsAsync();

        /// <summary>
        /// Inserts pre-order reminder settings.
        /// </summary>
        /// <param name="settings">The pre-order reminder settings.</param>
        Task InsertPreOrderReminderSettingsAsync(PreOrderReminderSettings settings);

        /// <summary>
        /// Updates pre-order reminder settings.
        /// </summary>
        /// <param name="settings">The pre-order reminder settings.</param>
        Task UpdatePreOrderReminderSettingsAsync(PreOrderReminderSettings settings);

        #endregion
    }
}
