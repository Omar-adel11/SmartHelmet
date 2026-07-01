using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Abstractions;
using BLL.Abstractions.EmergencyContacts;
using BLL.Services.Helper.Email;
using DAL.Users;
using MailKit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Shared.DTOs;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using static System.Net.WebRequestMethods;

namespace BLL.Services.EmergencyContacts
{
<<<<<<< HEAD
    public class EmergencyContactService(IUnitOfWork _unitOfWork,
        Helper.Email.IEmailService _emailService,
        IWebHostEnvironment _environment,
        IConfiguration _configuration) : IEmergencyContactService
=======
    public class EmergencyContactService(IUnitOfWork _unitOfWork, Helper.Email.IEmailService _emailService, IWebHostEnvironment _environment) : IEmergencyContactService
>>>>>>> afefcdecc269f1d642f668a131adb4860ed2e941
    {
        public async Task<IEnumerable<EmergencyContactDTO>> GetEmergencyContactsByUser(int UserId)
        {
            var contacts = await _unitOfWork.GetRepository<EmergencyContact,int>().GetAllAsync(E=>E.AppUser);
            var result = contacts.Where(c => c.AppUserId == UserId).Select(c => new EmergencyContactDTO
            {
                Id = c.Id,
                Name = c.Name,
                PhoneNumber = c.PhoneNumber,
                PictureUrl = c.PictureURL
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

            if (contact.File is not null)
            {
                emergencyContact.PictureURL = Helper.DocumentSettings.UploadFile(contact.File, _environment.WebRootPath, "images");
            }

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
            if (Requiredcontact.PictureURL is not null)
            {
                Helper.DocumentSettings.DeleteFile(Requiredcontact.PictureURL, _environment.WebRootPath, "images");
            }

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

            if (contactDto.File is not null)
            {
                if (contact.PictureURL is not null)
                {
                    Helper.DocumentSettings.DeleteFile(contact.PictureURL, _environment.WebRootPath, "images");
                }
                contact.PictureURL = Helper.DocumentSettings.UploadFile(contactDto.File, _environment.WebRootPath, "images");
            }

            await _unitOfWork.SaveChangesAsync();
        }


        public async Task SOS(int UserId, string mssg)
        {
            // 1. Fetch all emergency contacts for this specific user
            var contacts = await GetEmergencyContactsByUser(UserId);

            if (contacts == null || !contacts.Any())
                throw new Exception("No emergency contacts found for this user.");

            // 2. Extract Twilio details securely from configuration
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            var fromPhone = _configuration["Twilio:FromPhoneNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhone))
            {
                throw new Exception("Twilio SMS configurations are missing.");
            }

            // 3. Initialize Twilio client instance
            TwilioClient.Init(accountSid, authToken);

            // 4. Loop through every contact and dispatch real SMS text messages
            foreach (var contact in contacts)
            {
                try
                {
                    // Ensure phone numbers include country codes (e.g., +201xxxxxxxxx for Egypt)
                    var targetPhoneNumber = contact.PhoneNumber.StartsWith("+")
                        ? contact.PhoneNumber
                        : $"+20{contact.PhoneNumber.TrimStart('0')}"; // Example defaulting to Egypt formatting

                    await MessageResource.CreateAsync(
                        body: $"⚠️ SMART HELMET EMERGENCY ALERT!\nHello {contact.Name},\n{mssg}",
                        from: new PhoneNumber(fromPhone),
                        to: new PhoneNumber(targetPhoneNumber)
                    );
                }
                catch (Exception smsEx)
                {
                    // Log the error or handle delivery failures gracefully so one bad contact number doesn't block the rest
                    Console.WriteLine($"Failed to send SMS text message to {contact.Name}: {smsEx.Message}");
                }
            }
        }
    }
}
