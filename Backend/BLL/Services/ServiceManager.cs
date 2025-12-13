using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Abstractions;
using BLL.Abstractions.Authentication;
using BLL.Abstractions.EmergencyContacts;
using BLL.Services.Authentication;
using BLL.Services.EmergencyContacts;
using BLL.Services.Helper.Email;
using DAL.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace BLL.Services
{
    public class ServiceManager
        (
            UserManager<AppUser> _userManager,
            IConfiguration _configuration,
            IEmailService _emailService,
            IUnitOfWork _unitOfWork
        ) : IServiceManager
    {
        public Abstractions.Authentication.IAuthenticationService AuthenticationService { get; } = new AuthService(_userManager, _configuration, _emailService);

        public IEmergencyContactService EmergencyContactService{ get; } = new EmergencyContactService(_unitOfWork);
    }
}
