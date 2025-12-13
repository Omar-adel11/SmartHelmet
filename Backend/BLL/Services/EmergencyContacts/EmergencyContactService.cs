using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Abstractions;
using BLL.Abstractions.EmergencyContacts;
using DAL.Users;
using MailKit;
using Microsoft.AspNetCore.Identity;
using Shared.DTOs;

namespace BLL.Services.EmergencyContacts
{
    public class EmergencyContactService(IUnitOfWork _unitOfWork) : IEmergencyContactService
    {
        public async Task<IEnumerable<EmergencyContactDTO>> GetEmergencyContactsByUser(int UserId)
        {
            var contacts = await _unitOfWork.GetRepository<EmergencyContact,int>().GetAllAsync(E=>E.AppUser);
            var result = contacts.Where(c => c.AppUserId == UserId).Select(c => new EmergencyContactDTO
            {
                Id = c.Id,
                Name = c.Name,
                PhoneNumber = c.PhoneNumber
            }).ToList();
            return result;
        }

        public async Task AddEmergencyContact(EmergencyContactDTO contact,int UserId)
        {
            
            var emergencyContact = new EmergencyContact
            {
                Name = contact.Name,
                PhoneNumber = contact.PhoneNumber,
                AppUserId = UserId
            };

            await _unitOfWork.GetRepository<EmergencyContact, int>().AddAsync(emergencyContact);
            await _unitOfWork.SaveChangesAsync();
        }


        public async Task DeleteEmergencyContact(int ContactId,int UserId)
        {
            var Requiredcontact = await _unitOfWork
                .GetRepository<EmergencyContact, int>()
                .GetByIdAsync(ContactId);

            if (Requiredcontact == null)
                throw new Exception("Contact not found.");

            if (Requiredcontact.AppUserId != UserId)
                throw new Exception("Unauthorized delete attempt.");

            _unitOfWork.GetRepository<EmergencyContact, int>().Delete(Requiredcontact);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateEmergencyContact(int ContactId,EmergencyContactDTO contactDto, int UserId)
        {
            var contact = await _unitOfWork
                .GetRepository<EmergencyContact, int>()
                .GetByIdAsync(ContactId);

            if (contact == null)
                throw new Exception("Contact not found.");

            if (contact.AppUserId != UserId)
                throw new Exception("Unauthorized update attempt.");

            contact.Name = contactDto.Name;
            contact.PhoneNumber = contactDto.PhoneNumber;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task SOS(int ContactId, int UserId)
        {
            var contacts = await GetEmergencyContactsByUser(UserId);
            var contact = contacts.FirstOrDefault(c => c.Id == ContactId);
            if (contact == null)
                throw new Exception("Contact not found.");

            //Send SOS To Phone Number
        }
    }
}
