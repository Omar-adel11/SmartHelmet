using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BLL.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace PL
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RidesController(IRideService _rideService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> AddRide([FromBody] RideDTO rideDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _rideService.AddRideAsync(rideDto, userId);
            return Ok();
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return Ok(await _rideService.GetUserRidesAsync(userId));
        }

        [HttpGet("weekly-vitals")]
        public async Task<IActionResult> GetWeeklyVitals()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return Ok(await _rideService.GetWeeklyVitalsAsync(userId));
        }
    }
}
