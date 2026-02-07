using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Users;
using Shared.DTOs;

namespace BLL.Abstractions.EmergencyContacts
{
    public interface IEmergencyContactService
    {
        Task<IEnumerable<EmergencyContactDTO>> GetEmergencyContactsByUser(int UserId);
        Task AddEmergencyContact(EmergencyContactDTO contact, int UserId);
        Task UpdateEmergencyContact(int ContactId,EmergencyContactDTO contact, int UserId);
        Task DeleteEmergencyContact(int ContactId, int UserId);
        Task SOS(int ContactId, int UserId);
    }
}
