using System;
using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Order
{
    public partial record CustomerOrderListModel : BaseNopModel
    {
        public CustomerOrderListModel()
        {
            //code by tp
            CitagenixOrders=new List<CitagenixOrderDetailsModel>();
            Orders = new List<OrderDetailsModel>();
            RecurringOrders = new List<RecurringOrderModel>();
            RecurringPaymentErrors = new List<string>();
        }
        //code by tp
        public IList<CitagenixOrderDetailsModel> CitagenixOrders { get; set; }
        public IList<OrderDetailsModel> Orders { get; set; }
        public IList<RecurringOrderModel> RecurringOrders { get; set; }
        public IList<string> RecurringPaymentErrors { get; set; }
        public string ISImpersonatecustomer { get; set; }
        #region Nested classes

        public partial record OrderDetailsModel : BaseNopEntityModel
        {
            public string CustomOrderNumber { get; set; }
            public string OrderTotal { get; set; }
            public bool IsReturnRequestAllowed { get; set; }
            public OrderStatus OrderStatusEnum { get; set; }
            public string OrderStatus { get; set; }
            public string PaymentStatus { get; set; }
            public string ShippingStatus { get; set; }
            public DateTime CreatedOn { get; set; }
            public string CompanyName { get; set; }
            public string CustomerName { get; set; }
            public string CustomerEmail { get; set; }
            public DateTime? PreferredDeliveryDate { get; set; }
            public bool IsPreOrder { get; set; }
        }
        //code by tp
        public partial record CitagenixOrderDetailsModel : BaseNopEntityModel
        {
            public string CustomOrderNumber { get; set; }
            public string OrderTotal { get; set; }
            
            public string OrderStatus { get; set; }
    
            public DateTime CreatedOn { get; set; }
            
            public string CompanyName { get; set; }
           


        }

        public partial record RecurringOrderModel : BaseNopEntityModel
        {
            public string StartDate { get; set; }
            public string CycleInfo { get; set; }
            public string NextPayment { get; set; }
            public int TotalCycles { get; set; }
            public int CyclesRemaining { get; set; }
            public int InitialOrderId { get; set; }
            public bool CanRetryLastPayment { get; set; }
            public string InitialOrderNumber { get; set; }
            public bool CanCancel { get; set; }
        }

        public partial record CustomerPreOrderModel : BaseNopModel
        {
            public CustomerPreOrderModel()
            {
                PreOrders = new List<OrderDetailsModel>();
                RegularOrders = new List<OrderDetailsModel>();
            }

            public IList<OrderDetailsModel> PreOrders { get; set; }

            public IList<OrderDetailsModel> RegularOrders { get; set; }
        }

        #endregion
    }
}