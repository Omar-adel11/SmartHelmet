using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Users
{
    public class Ride : BaseEntity<int>
    {
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }
        public DateTime RideDate { get; set; }
        public string Duration { get; set; } // e.g., "05:23"
        public int AvgHeartRate { get; set; }
    }
}
