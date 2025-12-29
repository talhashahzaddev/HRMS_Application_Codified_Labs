using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.AI.Models
{
    public class LeaveApplicationDetails
    {
        public Guid leavetypeid { get; set; }
        public string? status { get; set; }
        public string? leaveType { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string? reason { get; set; }
        public string? statustext { get; set; }
    }
}
