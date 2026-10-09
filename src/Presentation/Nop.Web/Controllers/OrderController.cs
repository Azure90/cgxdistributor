using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

using System.Data;
using Dapper;
using Nop.Web.Models.Order;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Nop.Web.Controllers
{
    [AutoValidateAntiforgeryToken]
    public partial class OrderController : BasePublicController
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly IOrderModelFactory _orderModelFactory;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IPdfService _pdfService;
        private readonly IShipmentService _shipmentService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly IConfiguration _configuration;
        #endregion

        #region Ctor

        public OrderController(ICustomerService customerService,
            IOrderModelFactory orderModelFactory,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IPaymentService paymentService,
            IPdfService pdfService,
            IShipmentService shipmentService,
            IWebHelper webHelper,
            IWorkContext workContext,
            RewardPointsSettings rewardPointsSettings,
            IConfiguration configuration)
        {
            _customerService = customerService;
            _orderModelFactory = orderModelFactory;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _paymentService = paymentService;
            _pdfService = pdfService;
            _shipmentService = shipmentService;
            _webHelper = webHelper;
            _workContext = workContext;
            _rewardPointsSettings = rewardPointsSettings;
            _configuration = configuration;
        }

        #endregion

        #region Methods

        //My account / Orders
        public virtual async Task<IActionResult> CustomerOrders()
        {
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            var model = await _orderModelFactory.PrepareCustomerOrderListModelAsync();
            //code by tp
            //_azureBlobStorageConnectionString = appSettings.Get<CitagenixConnectionistring>().ConnectionString;
            var connstring = _configuration["Nop:CitagenixConnectionistring"];
            //var datasettings = DataSettingsManager.LoadSettings();
           // var connstring = datasettings.ConnectionString;
            var dt = new DataTable();
            var conn = new SqlConnection(connstring);
            SqlDataAdapter da = new SqlDataAdapter();
            conn.Open();

            var currcust = _workContext.GetCurrentCustomerAsync().Result.BPCode;

            var Customercmd = new SqlCommand("select o.id, o.OrderStatusId,o.CreatedOnUtc,o.OrderTotal,o.ShippingStatusId  from /*[CitagenixProd].dbo.*/Customer c join /*[CitagenixProd].dbo.*/[order] o on c.id=o.CustomerId where c.BPCode= '" + currcust + "'", conn);
            Customercmd.CommandType = CommandType.Text;
            Customercmd.CommandTimeout = 0;


            da = new SqlDataAdapter(Customercmd);
            dt = new DataTable();
            da.Fill(dt);
            var model1 = dt.Rows;
            for (var i = 0; i < model1.Count; i++)
            {
                if (!model.Orders.Any(d => d.Id == Convert.ToInt32(model1[i].ItemArray[0])))
                    {
                    //  var Citagenixmodel = new Nop.Web.Models.Order.CustomerOrderListModel.CitagenixOrderDetailsModel();
                    var Citagenixmodel = new Nop.Web.Models.Order.CustomerOrderListModel.OrderDetailsModel();
                    var status = "";
                    if (model1[i].ItemArray[1].ToString() == "10")
                    {
                        status = "Pending";
                    }
                    if (model1[i].ItemArray[1].ToString() == "20")
                    {
                        status = "Processing";
                    }
                    if (model1[i].ItemArray[1].ToString() == "30")
                    {
                        status = "Complete";
                    }
                    if (model1[i].ItemArray[1].ToString() == "40")
                    {
                        status = "Cancelled";
                    }
                    Citagenixmodel.Id = Convert.ToInt32(model1[i].ItemArray[0]);
                    Citagenixmodel.CustomOrderNumber = Convert.ToString(model1[i].ItemArray[0]);
                    Citagenixmodel.OrderStatus = status;
                    Citagenixmodel.CreatedOn = Convert.ToDateTime(model1[i].ItemArray[2]);
                    Citagenixmodel.OrderTotal = Convert.ToString(model1[i].ItemArray[3]);
                    Citagenixmodel.CompanyName = "Citagenix";
                    model.Orders.Add(Citagenixmodel);
                }
            }


            return View(model);
        }



        //My account / Orders / Cancel recurring order
        [HttpPost, ActionName("CustomerOrders")]
        [FormValueRequired(FormValueRequirement.StartsWith, "cancelRecurringPayment")]
        public virtual async Task<IActionResult> CancelRecurringPayment(IFormCollection form)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            //get recurring payment identifier
            var recurringPaymentId = 0;
            foreach (var formValue in form.Keys)
                if (formValue.StartsWith("cancelRecurringPayment", StringComparison.InvariantCultureIgnoreCase))
                    recurringPaymentId = Convert.ToInt32(formValue["cancelRecurringPayment".Length..]);

            var recurringPayment = await _orderService.GetRecurringPaymentByIdAsync(recurringPaymentId);
            if (recurringPayment == null)
            {
                return RedirectToRoute("CustomerOrders");
            }

            if (await _orderProcessingService.CanCancelRecurringPaymentAsync(customer, recurringPayment))
            {
                var errors = await _orderProcessingService.CancelRecurringPaymentAsync(recurringPayment);

                var model = await _orderModelFactory.PrepareCustomerOrderListModelAsync();
                model.RecurringPaymentErrors = errors;

                return View(model);
            }

            return RedirectToRoute("CustomerOrders");
        }

        //My account / Orders / Retry last recurring order
        [HttpPost, ActionName("CustomerOrders")]
        [FormValueRequired(FormValueRequirement.StartsWith, "retryLastPayment")]
        public virtual async Task<IActionResult> RetryLastRecurringPayment(IFormCollection form)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Challenge();

            //get recurring payment identifier
            var recurringPaymentId = 0;
            if (!form.Keys.Any(formValue => formValue.StartsWith("retryLastPayment", StringComparison.InvariantCultureIgnoreCase) &&
                int.TryParse(formValue[(formValue.IndexOf('_') + 1)..], out recurringPaymentId)))
            {
                return RedirectToRoute("CustomerOrders");
            }

            var recurringPayment = await _orderService.GetRecurringPaymentByIdAsync(recurringPaymentId);
            if (recurringPayment == null)
                return RedirectToRoute("CustomerOrders");

            if (!await _orderProcessingService.CanRetryLastRecurringPaymentAsync(customer, recurringPayment))
                return RedirectToRoute("CustomerOrders");

            var errors = await _orderProcessingService.ProcessNextRecurringPaymentAsync(recurringPayment);
            var model = await _orderModelFactory.PrepareCustomerOrderListModelAsync();
            model.RecurringPaymentErrors = errors.ToList();

            return View(model);
        }

        //My account / Reward points
        public virtual async Task<IActionResult> CustomerRewardPoints(int? pageNumber)
        {
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            if (!_rewardPointsSettings.Enabled)
                return RedirectToRoute("CustomerInfo");

            var model = await _orderModelFactory.PrepareCustomerRewardPointsAsync(pageNumber);
            return View(model);
        }

        //My account / Order details page
        public virtual async Task<IActionResult> Details(int orderId, string CompanyName, string OrderStatus)
        {
            if (CompanyName == "Citagenix")
            {
                var Citagenixmodel = new OrderDetailsModel();
                //var connection = DataSettingsManager.LoadSettings().ConnectionString;
                var connection = _configuration["Nop:CitagenixConnectionistring"];
                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();

                    ds.Add("@id", orderId);
                    var result = conn1.Query("Get_Citagenixinvoicedata", param: ds, commandType: CommandType.StoredProcedure);
                    if (result.Count() > 0)
                    {
                        foreach (var i in result)
                        {
                            Citagenixmodel.CustomOrderNumber = Convert.ToString(i.orderid);
                            Citagenixmodel.CreatedOn = Convert.ToDateTime(i.Date);
                            Citagenixmodel.OrderStatus = OrderStatus;
                            Citagenixmodel.OrderTotal = Convert.ToString(i.pricewithtax);
                            Citagenixmodel.BillingAddress.PhoneEnabled = true;
                            Citagenixmodel.BillingAddress.FaxEnabled = true;
                            Citagenixmodel.BillingAddress.CompanyEnabled = true;
                            Citagenixmodel.BillingAddress.Company = "true";
                            Citagenixmodel.BillingAddress.StreetAddressEnabled = true;
                            Citagenixmodel.BillingAddress.CityEnabled = true;
                            Citagenixmodel.BillingAddress.CountyEnabled = true;
                            Citagenixmodel.BillingAddress.StateProvinceEnabled = true;
                            Citagenixmodel.BillingAddress.ZipPostalCodeEnabled = true;
                            Citagenixmodel.BillingAddress.CityEnabled = true;
                            Citagenixmodel.BillingAddress.Address1 = Convert.ToString(i.Address);
                            Citagenixmodel.BillingAddress.FirstName = Convert.ToString(i.FirstName);
                            Citagenixmodel.BillingAddress.LastName = Convert.ToString(i.LastName);
                            Citagenixmodel.BillingAddress.Email = Convert.ToString(i.Email);
                            Citagenixmodel.BillingAddress.PhoneNumber = Convert.ToString(i.PhoneNumber);
                            Citagenixmodel.BillingAddress.FaxNumber = Convert.ToString(i.FaxNumber);
                            Citagenixmodel.BillingAddress.Company = "Citagenix";
                            Citagenixmodel.BillingAddress.ZipPostalCode = Convert.ToString(i.ZipPostalCode);
                            Citagenixmodel.BillingAddress.StateProvinceName = Convert.ToString(i.City);
                            Citagenixmodel.IsShippable = true;
                            Citagenixmodel.ShippingAddress.PhoneEnabled = true;
                            Citagenixmodel.ShippingAddress.FaxEnabled = true;
                            Citagenixmodel.ShippingAddress.CompanyEnabled = true;
                            Citagenixmodel.ShippingAddress.Company = "true";
                            Citagenixmodel.ShippingAddress.StreetAddressEnabled = true;
                            Citagenixmodel.ShippingAddress.CityEnabled = true;
                            Citagenixmodel.ShippingAddress.CountyEnabled = true;
                            Citagenixmodel.ShippingAddress.StateProvinceEnabled = true;
                            Citagenixmodel.ShippingAddress.ZipPostalCodeEnabled = true;
                            Citagenixmodel.ShippingAddress.CityEnabled = true;
                            Citagenixmodel.ShippingAddress.Address1 = Convert.ToString(i.Address);
                            Citagenixmodel.ShippingAddress.FirstName = Convert.ToString(i.FirstName);
                            Citagenixmodel.ShippingAddress.LastName = Convert.ToString(i.LastName);
                            Citagenixmodel.ShippingAddress.Email = Convert.ToString(i.Email);
                            Citagenixmodel.ShippingAddress.PhoneNumber = Convert.ToString(i.PhoneNumber);
                            Citagenixmodel.ShippingAddress.FaxNumber = Convert.ToString(i.FaxNumber);
                            Citagenixmodel.ShippingAddress.Company = "Citagenix";
                            Citagenixmodel.ShippingAddress.ZipPostalCode = Convert.ToString(i.ZipPostalCode);
                            Citagenixmodel.ShippingAddress.StateProvinceName = Convert.ToString(i.city);
                            Citagenixmodel.ShowSku = true;
                            Citagenixmodel.PaymentMethod = i.PaymentMethodSystemName;
                            Citagenixmodel.PaymentMethodStatus = OrderStatus;
                            Citagenixmodel.ShippingMethod = i.ShippingMethod;
                            var orderItemModel = new OrderDetailsModel.OrderItemModel
                            {
                                Id = 0,
                                // OrderItemGuid = orderItem.OrderItemGuid,
                                Sku = i.Sku,
                                ProductName = i.ProductName,
                                ProductSeName = i.ProductName,
                                Quantity = i.Quantity,
                                UnitPrice = Convert.ToString(i.pricewithouttax),
                                SubTotal = Convert.ToString(i.pricewithouttax)
                                /*AttributeInfo = orderItem.AttributeDescription*/,
                            };

                            Citagenixmodel.Items.Add(orderItemModel);
                            Citagenixmodel.DisplayTax = true;
                            Citagenixmodel.DisplayTaxRates = true;
                            
                            Citagenixmodel.Tax = Convert.ToString(i.tax);
                            Citagenixmodel.OrderSubtotal = Convert.ToString(i.pricewithouttax);
                            var Shippingstatus = "";
                            if (i.ShippingStatusId == 10)
                            {
                                Shippingstatus = "Shipping Not Required";
                            }
                            if (i.ShippingStatusId == 20)
                            {
                                Shippingstatus = "Not Yet Shipped";
                            }
                            if (i.ShippingStatusId == 25)
                            {
                                Shippingstatus = "Partially Shipped";
                            }
                            if (i.ShippingStatusId == 30)
                            {
                                Shippingstatus = "Shipped";
                            }
                            if (i.ShippingStatusId == 40)
                            {
                                Shippingstatus = "Delivered";
                            }
                            Citagenixmodel.ShippingStatus = Shippingstatus;
                        }                        

                    }

                }
                return View(Citagenixmodel);

            }
            else
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                var customer = await _workContext.GetCurrentCustomerAsync();

                if (order == null || order.Deleted || customer.Id != order.CustomerId)
                    return Challenge();

                var model = await _orderModelFactory.PrepareOrderDetailsModelAsync(order);
                return View(model);
            }

        }

        //My account / Order details page / Print
        public virtual async Task<IActionResult> PrintOrderDetails(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (order == null || order.Deleted || customer.Id != order.CustomerId)
                return Challenge();

            var model = await _orderModelFactory.PrepareOrderDetailsModelAsync(order);
            model.PrintMode = true;

            return View("Details", model);
        }

        //My account / Order details page / PDF invoice
        [CheckLanguageSeoCode(ignore: true)]
        public virtual async Task<IActionResult> GetPdfInvoice(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (order == null || order.Deleted || customer.Id != order.CustomerId)
                return Challenge();

            byte[] bytes;
            await using (var stream = new MemoryStream())
            {
                await _pdfService.PrintOrderToPdfAsync(stream, order, await _workContext.GetWorkingLanguageAsync());
                bytes = stream.ToArray();
            }
            return File(bytes, MimeTypes.ApplicationPdf, $"order_{order.CustomOrderNumber}.pdf");
        }

        //My account / Order details page / re-order
        public virtual async Task<IActionResult> ReOrder(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (order == null || order.Deleted || customer.Id != order.CustomerId)
                return Challenge();
            order.ParentCustomerId =  _workContext.OriginalCustomerIfImpersonated==null?0: _workContext.OriginalCustomerIfImpersonated.Id;
            await _orderProcessingService.ReOrderAsync(order);
            return RedirectToRoute("ShoppingCart");
        }

        //My account / Order details page / Complete payment
        [HttpPost, ActionName("Details")]

        [FormValueRequired("repost-payment")]
        public virtual async Task<IActionResult> RePostPayment(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (order == null || order.Deleted || customer.Id != order.CustomerId)
                return Challenge();

            if (!await _paymentService.CanRePostProcessPaymentAsync(order))
                return RedirectToRoute("OrderDetails", new { orderId = orderId });

            var postProcessPaymentRequest = new PostProcessPaymentRequest
            {
                Order = order
            };
            await _paymentService.PostProcessPaymentAsync(postProcessPaymentRequest);

            if (_webHelper.IsRequestBeingRedirected || _webHelper.IsPostBeingDone)
            {
                //redirection or POST has been done in PostProcessPayment
                return Content("Redirected");
            }

            //if no redirection has been done (to a third-party payment page)
            //theoretically it's not possible
            return RedirectToRoute("OrderDetails", new { orderId = orderId });
        }

        //My account / Order details page / Shipment details page
        public virtual async Task<IActionResult> ShipmentDetails(int shipmentId)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(shipmentId);
            if (shipment == null)
                return Challenge();

            var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
            var customer = await _workContext.GetCurrentCustomerAsync();

            if (order == null || order.Deleted || customer.Id != order.CustomerId)
                return Challenge();

            var model = await _orderModelFactory.PrepareShipmentDetailsModelAsync(shipment);
            return View(model);
        }
        //public virtual async Task<IActionResult> rewardheaderdetail()
        //{
        //    var model = await _orderModelFactory.PrepareCustomerRewardPointsAsync(null);
        //    var rewardPointsBalance = model.RewardPointsBalance;
        //    var PointsAmount = model.RewardPointsAmount;
        //    return Json(new
        //    {
        //        rewardPointsBalance,
        //        PointsAmount,
        //    });

        //}
        #endregion
    }
}