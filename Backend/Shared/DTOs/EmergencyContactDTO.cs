using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Users;
using Microsoft.AspNetCore.Http;

namespace Shared.DTOs
{
    public class EmergencyContactDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string? PictureUrl { get; set; } 
        public IFormFile? File { get; set; }    
    }
}
