using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs
{
    public class RideDTO
    {
        public int Id { get; set; }
        public DateTime RideDate { get; set; }
        public string Duration { get; set; }
        public int AvgHeartRate { get; set; }
    }

    public class WeeklyVitalsDTO
    {
        public string Day { get; set; } // e.g., "Sat"
        public int AvgHeartRate { get; set; }
    }
}
