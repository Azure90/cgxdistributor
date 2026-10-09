using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;

namespace Nop.Web.Framework.Mvc.Filters
{ /// <summary>
  /// Represents a filter attribute that restricts access to actions
  /// </summary>
    public sealed class RequireRegisteredCustomerAttribute : TypeFilterAttribute
    {
        #region Ctor

        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        public RequireRegisteredCustomerAttribute() : base(typeof(RequireRegisteredCustomerFilter))
        {
        }

        #endregion

        #region Nested filter

        public class RequireRegisteredCustomerFilter : IAsyncAuthorizationFilter
        {
            #region Fields

            private readonly IWorkContext _workContext;
            private readonly ICustomerService _customerService;

            #endregion

            #region Ctor

            public RequireRegisteredCustomerFilter(
                IWorkContext workContext,
                ICustomerService customerService)
            {
                _workContext = workContext;
                _customerService = customerService;
            }

            #endregion

            #region Methods

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                var customer = await _workContext.GetCurrentCustomerAsync();

                if (customer == null || !await _customerService.IsRegisteredAsync(customer))
                {
                    context.Result = new RedirectToRouteResult("Homepage", null);
                }
            }

            #endregion
        }

        #endregion
    }
}
