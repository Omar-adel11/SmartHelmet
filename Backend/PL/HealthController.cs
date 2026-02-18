using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class HealthController(UserManager<AppUser> _userManager) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetHealthInfo()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            return Ok(new HealthInfoDTO
            {
                FullName = user.FullName,
                Gender = (int?)user.Gender,
                Age = user.Age,
                Weight = user.Weight,
                BloodType = user.BloodType,
                ChronicConditions = user.ChronicConditions
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateHealthInfo([FromBody] HealthInfoDTO dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            user.FullName = dto.FullName;
            user.Gender = (Gender?)dto.Gender;
            user.Age = dto.Age;
            user.Weight = dto.Weight;
            user.BloodType = dto.BloodType;
            user.ChronicConditions = dto.ChronicConditions;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok("Health information updated successfully.");
        }
    }
}
