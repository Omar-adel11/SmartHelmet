using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BLL.Abstractions;
using BLL.Services.Helper.Email;
using DAL.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace PL
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmergencyContactsController(IServiceManager _serviceManager,UserManager<AppUser> _userManager) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetEmergencyContacts()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid token");
            var contacts = await _serviceManager.EmergencyContactService.GetEmergencyContactsByUser(userId);
            return Ok(contacts);
        }
        [HttpPost]
        public async Task<IActionResult> AddEmergencyContact([FromBody] EmergencyContactDTO contact)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid token");

            await _serviceManager.EmergencyContactService.AddEmergencyContact(contact,userId);
            return Ok();
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEmergencyContact(int id, [FromBody] EmergencyContactDTO contact)
        {

            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid token");

            await _serviceManager.EmergencyContactService.UpdateEmergencyContact(id,contact, userId);
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEmergencyContact(int id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid token");

            await _serviceManager.EmergencyContactService.DeleteEmergencyContact(id, userId);
            return Ok();
        }

        private bool TryGetUserId(out int userId)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out userId);
        }

    }
}
