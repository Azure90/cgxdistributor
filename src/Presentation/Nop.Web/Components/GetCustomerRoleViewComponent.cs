using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Web.Framework.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Web.Components
{
    public class GetCustomerRoleViewComponent : NopViewComponent
    {
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;
        public GetCustomerRoleViewComponent(ICustomerService customerService, IWorkContext workContext)
        {
            this._customerService = customerService;
            this._workContext = workContext;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            GetCustomer obj = new GetCustomer();
            //string CurrencyFlag = "My Account&nbsp;&nbsp;<span title='Prices are in Canadian Dollars'><img src='/images/flags/ca.png' />&nbsp;CAD</span>";
            obj.IsCADrole = true;
            if (_workContext != null)
            {
                var LoginEmail = _workContext.GetCurrentCustomerAsync().Result.Id;
                if (LoginEmail != null)
                {
                    var loginnedCustomer = _customerService.GetCustomerByIdAsync(LoginEmail);
                    var CustomerRoles = _customerService.GetCustomerRolesAsync(loginnedCustomer.Result);


                    if (CustomerRoles.Result.Count > 0)
                    {
                        if (CustomerRoles.Result.Where(x => x.Name.ToLower().Contains("usd") || x.Name.ToLower().Contains("intnat")).FirstOrDefault<CustomerRole>() != null)
                        {
                            obj.IsCADrole = false;
                            //CurrencyFlag = "My Account&nbsp;&nbsp;<span title='Prices are in USD Dollars'><img src='/images/flags/us.png' />&nbsp;USD</span>";
                        }
                    }
                }
            }
            return View(obj);
        }
        public class GetCustomer
        {
            public bool IsCADrole { get; set; }
        }


    }
}
