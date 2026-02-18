using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs
{
    public class HealthInfoDTO
    {
        public string FullName { get; set; }
        public int? Gender { get; set; } 
        public int? Age { get; set; }
        public decimal? Weight { get; set; }
        public string? BloodType { get; set; }
        public string? ChronicConditions { get; set; }
    }
}
