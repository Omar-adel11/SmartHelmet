using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs
{
    public class ResetPasswordDTO
    {
        public string Email { get; set; }
        public string OTP { get; set; }
        public string NewPassword { get; set; }

    }
}
