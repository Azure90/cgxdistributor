using System.Collections.Generic;

namespace Nop.Web.Models.AssociatedCustomer
{
    public class AssociatedCustomerModel
    {
        public string FirstName { get; set; }
        public AssociatedCustomerModel()
        {
            associateorders = new List<associateorder>();
        }
        public List<associateorder> associateorders { get; set; }

    }
    public class associateorder
    {
        public string BPCode { get; set; }
        public string ProductName { get; set; }
        public int OrderId { get; set; }
        public string Price { get; set; }
    }
}
