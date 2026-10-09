using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Customers;

namespace Nop.Web.Controllers
{
    public class ProductGroupController : BasePublicController
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public ProductGroupController(
            ICustomerService customerService,
            IWorkContext workContext,
            ISettingService settingService,
            IWebHelper webHelper)
        {
            _customerService = customerService;
            _workContext = workContext;
            _settingService = settingService;
            _webHelper = webHelper;
        }

        #endregion


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> IsGuest()
        {
            int statuscode;

            var curUser = await _workContext.GetCurrentCustomerAsync();
            if (await _customerService.IsInCustomerRoleAsync(curUser, "Guests", true))
            {
                statuscode = 200; //OK hey are a guest
            }
            else
            {
                statuscode = 203;  //sucess no content, not a guest
            }

            return StatusCode(statuscode);
        }

        public async Task<bool> ShowProductSelector()
        {
            bool retval = true;

            //Assigns the current guest user to the role (id) so that they may browse that product group
            string rolelist;

            //Try to get the roles from the CustomerRegisterRoles plugin setting        
            rolelist = "Dental,Medical,Consumer";

            var roles = rolelist.Split(",");

            var curUser = await _workContext.GetCurrentCustomerAsync();
            if (await _customerService.IsInCustomerRoleAsync(curUser, "Guests", true))
            {

                foreach (var rolename in roles)
                {
                    //Remove other roles Product Group roles if they exist (Dental, Consumer, Medical)
                    if (await _customerService.IsInCustomerRoleAsync(curUser, rolename))
                    {
                        retval = false;
                        break;
                    }

                }
            }
            else
            {
                retval = false;
            }

            return retval;
        }

        public async Task<IActionResult> FilterBackup(string id)
        {
            //Assigns the current guest user to the role (id) so that they may browse that product group
            string rolelist;

            //Try to get the roles from the CustomerRegisterRoles plugin setting
            var rolesetting = _settingService.GetSetting("productgroupcontrollersettings.rolelist");

            if (rolesetting == null)
            {
                rolelist = "Dental,Medical,Consumer";
            }
            else
            {
                rolelist = rolesetting.Value;
            }

            var roles = rolelist.Split(",");

            var curUser = await _workContext.GetCurrentCustomerAsync();
            if (await _customerService.IsInCustomerRoleAsync(curUser, "Guests", true))
            {
                foreach (var rolename in roles)
                {
                    //Remove other roles Product Group roles if they exist (Dental, Consumer, Medical)

                    if (id.ToLower() != rolename.ToLower())
                    {
                        if (await _customerService.IsInCustomerRoleAsync(curUser, rolename))
                        {
                            var role = await _customerService.GetCustomerRoleBySystemNameAsync(rolename);
                            await _customerService.RemoveCustomerRoleMappingAsync(curUser, role);
                        }
                    }
                }

                //Add the Filer Role (id)
                var newrole = await _customerService.GetCustomerRoleBySystemNameAsync(id);
                if (newrole != null)
                {
                    if (!await _customerService.IsInCustomerRoleAsync(curUser, newrole.Name))
                    {
                        var custrole = new Core.Domain.Customers.CustomerCustomerRoleMapping();
                        custrole.CustomerId = curUser.Id;
                        custrole.CustomerRoleId = newrole.Id;

                        await _customerService.AddCustomerRoleMappingAsync(custrole);
                    }
                }
                else
                {
                    throw new System.ArgumentException("Invalid role name: " + id, "id");
                }

                // Getting ProductId from ProductController/ProductDetails
                if (TempData["productId"] != null)
                {
                    var productid = TempData["productId"].ToString();
                    return RedirectToAction("ProductDetails", "Product", new { productId = productid });
                }
                return RedirectToAction("Index", "Home");
            }

            //Dont do anything if the user is not a Guest
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Filter(string id)
        {
            id = id.ToLower();
            //Assigns the current guest user to the role (id) so that they may browse that product group
            string rolelist;
            var aa = HttpContext.Request.Path;
            //Try to get the roles from the CustomerRegisterRoles plugin setting
            var rolesetting = await _settingService.GetSettingAsync("productgroupcontrollersettings.rolelist");

            if (rolesetting == null)
            {
                rolelist = "cad,usd,intl";
            }
            else
            {
                rolelist = rolesetting.Value;
            }

            var roles = rolelist.Split(",");

            var curUser = await _workContext.GetCurrentCustomerAsync();
            if (await _customerService.IsInCustomerRoleAsync(curUser, "Guests", true))
            {
                //var aa = _customerService.GetAllCustomerRoles();
                foreach (var rolename in roles)
                {
                    //Remove other roles Product Group roles if they exist (Dental, Consumer, Medical)
                    if (id.ToLower() != rolename.ToLower())
                    {
                        var role = await _customerService.GetCustomerRolesBySystemNameAsync(rolename);
                        await _customerService.RemoveCustomerRoleListMappingAsync(curUser, role);
                    }
                }
                //Add the Filer Role(id)
                var newrole = _customerService.GetCustomerRolesBySystemNameAsync(id).Result;

                if (id == "usd")
                {
                    newrole = newrole.Where(d => d.Name != "USD B2B INTL").ToList();
                }
                if (newrole.Count > 0)
                {
                    foreach (var role in newrole)
                    {
                        if (!_customerService.IsInCustomerRoleAsync(curUser, role.Name).Result)
                        {
                            var custrole = new Core.Domain.Customers.CustomerCustomerRoleMapping();
                            custrole.CustomerId = curUser.Id;
                            custrole.CustomerRoleId = role.Id;
                            await _customerService.AddCustomerRoleMappingAsync(custrole);
                        }
                    }
                }
                else
                {
                    throw new System.ArgumentException("Invalid role name: " + id, "id");
                }

                // Getting ProductId from ProductController/ProductDetails
                if (TempData["productId"] != null)
                {
                    var productid = TempData["productId"].ToString();
                    return RedirectToAction("ProductDetails", "Product", new { productId = productid });
                }
                return RedirectToAction("Index", "Home");
            }

            //Dont do anything if the user is not a Guest
            return RedirectToAction("Index", "Home");

        }
        public async Task<IActionResult> Filterrefistercustomer(string id)
        {
            id = id.ToLower();
            //Assigns the current guest user to the role (id) so that they may browse that product group
            string rolelist;
            var aa = HttpContext.Request.Path;
            //Try to get the roles from the CustomerRegisterRoles plugin setting
            var rolesetting = await _settingService.GetSettingAsync("productgroupcontrollersettings.rolelist");

            if (rolesetting == null)
            {
                rolelist = "cad,usd,intl";
            }
            else
            {
                rolelist = rolesetting.Value;
            }

            var roles = rolelist.Split(",");

            var curUser = await _workContext.GetCurrentCustomerAsync();
            //if (await _customerService.IsInCustomerRoleAsync(curUser, "Guests", true))
              //var aa = _customerService.GetAllCustomerRoles();
                foreach (var rolename in roles)
                {
                    //Remove other roles Product Group roles if they exist (Dental, Consumer, Medical)
                    if (id.ToLower() != rolename.ToLower())
                    {
                        var role = await _customerService.GetCustomerRolesBySystemNameAsync(rolename);
                        await _customerService.RemoveCustomerRoleListMappingAsync(curUser, role);
                    }
                }
                //Add the Filer Role(id)
                var newrole = _customerService.GetCustomerRolesBySystemNameAsync(id).Result;

                if (id == "usd")
                {
                    newrole = newrole.Where(d => d.Name != "USD B2B INTL").ToList();
                }
                if (newrole.Count > 0)
                {
                    foreach (var role in newrole)
                    {
                        if (!_customerService.IsInCustomerRoleAsync(curUser, role.Name).Result)
                        {
                            var custrole = new Core.Domain.Customers.CustomerCustomerRoleMapping();
                            custrole.CustomerId = curUser.Id;
                            custrole.CustomerRoleId = role.Id;
                            await _customerService.AddCustomerRoleMappingAsync(custrole);
                        }
                    }
                }
                else
                {
                    throw new System.ArgumentException("Invalid role name: " + id, "id");
                }

                // Getting ProductId from ProductController/ProductDetails
                if (TempData["productId"] != null)
                {
                    var productid = TempData["productId"].ToString();
                    return RedirectToAction("ProductDetails", "Product", new { productId = productid });
                }
                return RedirectToAction("Index", "Home");            

            //Dont do anything if the user is not a Guest
            return RedirectToAction("Index", "Home");

        }
    }
}
