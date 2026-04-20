using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Users;
using Microsoft.AspNetCore.Http;

namespace Shared.DTOs
{
    public class UpdateUserDTO
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public IFormFile? file { get; set; }
        public Gender? Gender { get; set; }
        public int? Age { get; set; }
        public decimal? Weight { get; set; }
        public string? BloodType { get; set; }
        public string? ChronicConditions { get; set; }
    }
}
