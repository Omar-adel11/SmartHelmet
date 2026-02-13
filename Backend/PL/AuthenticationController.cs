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
    public class AuthenticationController(IServiceManager _serviceManager) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO request)
        {
            var result = await _serviceManager.AuthenticationService.RegisterAsync(request);
            return Ok(result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {
            var result = await _serviceManager.AuthenticationService.LoginAsync(request);
            return Ok(result);
        }
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordDTO ForgetPasswordDTO)
        {
            var result = await _serviceManager.AuthenticationService.ForgetPasswordAsync(ForgetPasswordDTO.Email);
            return Ok(result);

        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO request)
        {
            var token = await _serviceManager.AuthenticationService.VerifyOtpAsync(request);
            return Ok(new { resetSessionToken = token });
        }



        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPasswordAfterOtp([FromBody] ResetPasswordDTO request)
        {
            var result = await _serviceManager.AuthenticationService.ResetPasswordAfterOtpAsync(request);
            return Ok(result);
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var email = User.FindFirst(ClaimTypes.Email).Value;
            var result = await _serviceManager.AuthenticationService.GetCurrentUserAsync(email);
            return Ok(result);
        }
    }
}
