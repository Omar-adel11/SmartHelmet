using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Abstractions.Authentication;
using BLL.Abstractions.EmergencyContacts;
using BLL.Services.Helper.Email;

namespace BLL.Abstractions
{
    public interface IServiceManager
    {
        IAuthenticationService AuthenticationService { get; }
        IEmergencyContactService EmergencyContactService { get; }
        
    }
}
