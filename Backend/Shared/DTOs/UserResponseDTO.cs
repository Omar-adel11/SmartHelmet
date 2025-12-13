using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Users;

namespace Shared.DTOs
{
    public class UserResponseDTO
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string? PictureURL { get; set; }
        public Gender? Gender { get; set; }
        public int? Age { get; set; }
        public decimal? Weight { get; set; }
        public string? BloodType { get; set; }

    }

  
}
