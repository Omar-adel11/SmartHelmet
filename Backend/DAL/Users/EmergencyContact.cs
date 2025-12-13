using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Users
{
    public class EmergencyContact : BaseEntity<int>
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public AppUser AppUser { get; set; }
        public int AppUserId { get; set; } //FK
    }
}
