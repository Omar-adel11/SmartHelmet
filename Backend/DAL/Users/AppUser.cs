using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace DAL.Users
{
    public class AppUser : IdentityUser<int> 
    {
        public string FullName { get; set; }
        public string? PictureURL { get; set; }
        public Gender? Gender { get; set; }
        public int? Age { get; set; }
        public decimal? Weight { get; set; }
        public string?  BloodType { get; set; }
        public ICollection<EmergencyContact>? EmergencyContacts { get; set; }
    }
}
