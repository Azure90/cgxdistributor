using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.CustomRewardPoint
{
    public partial class RewardsPointCustom : BaseEntity
    {
        public string WeekDays { get; set; }
        public string SelectedType { get; set; }
        public int RewardPoints { get; set; }
        public string CategoryIds { get; set; }
        public string BrandIds { get; set; }
        public string ProductIds { get; set; }
        public bool Active { get; set; }
        public string CustomerRoleid { get; set; }
    }
}
