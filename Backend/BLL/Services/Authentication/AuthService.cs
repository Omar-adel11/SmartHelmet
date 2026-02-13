using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BLL.Abstractions.Authentication;
using BLL.Services.Helper;
using BLL.Services.Helper.Email;
using DAL.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NETCore.MailKit.Core;
using Shared.DTOs;

namespace BLL.Services.Authentication
{
    public class AuthService(UserManager<AppUser> _userManager,IConfiguration _configuration,Helper.Email.IEmailService _emailService) : IAuthenticationService
    {

        public async Task<string> LoginAsync(LoginDTO loginDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);
            if (user == null)
            {
                throw new Exception("Invalid email or password.");
            }
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDTO.Password);
            if (!isPasswordValid)
            {
                throw new Exception("Invalid email or password.");
            }

            var token = await GenerateTokenAsync(user);
            return token;

        }

        public async Task<UserDTO> RegisterAsync(RegisterDTO registerDTO)
        {
            var user = new AppUser()
            {
                UserName = registerDTO.Email,
                Email = registerDTO.Email,
                FullName = registerDTO.Fullname,
                PhoneNumber = registerDTO.Phone
            };

            var result = await _userManager.CreateAsync(user, registerDTO.Password);
            if (!result.Succeeded)
            {
                throw new Exception("User registration failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            var token = await GenerateTokenAsync(user);
           

            return new UserDTO
            {
                FullName = user.FullName,
                Email = user.Email,
                Token = token,
                Phone = user.PhoneNumber
            };
        }

        public async Task<string> ForgetPasswordAsync(string Email)
        {
            var user =  await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                throw new Exception("User not found.");
            }
            var otp = RandomNumberGenerator.GetInt32(0, 10000).ToString("D4");
            var expiryTime = DateTime.UtcNow.AddMinutes(15); // Valid for 15 mins
            var tokenValue = $"{otp}|{expiryTime.ToString("o")}"; // Combine them

            await _userManager.SetAuthenticationTokenAsync(user, "Default", "ResetOTP", tokenValue);
            

            var mail = new Email()
            {
                To = Email,
                Subject = "Password Reset",
                Body = $"Use this token to reset your password: {otp}"
            };

            await _emailService.SendEmailAsync(mail);
            return "Check Your Email";
        }

        public async Task<string> VerifyOtpAsync(VerifyOtpDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new Exception("Invalid request.");

            if (await _userManager.IsLockedOutAsync(user))
                throw new Exception("Account is temporarily locked. Try again later.");

            var tokenValue = await _userManager.GetAuthenticationTokenAsync(user, "Default", "ResetOTP");
            if (string.IsNullOrEmpty(tokenValue))
            {
                await _userManager.AccessFailedAsync(user);
                throw new Exception("Invalid or expired OTP.");
            }

            var parts = tokenValue.Split('|');
            var storedOtp = parts[0];
            var expiryString = parts[1];

            if (storedOtp != dto.OTP)
            {
                await _userManager.AccessFailedAsync(user);
                throw new Exception("Invalid OTP.");
            }

            if (!DateTime.TryParseExact(expiryString, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime expiryDate))
                throw new Exception("Error parsing token expiration.");

            if (DateTime.UtcNow > expiryDate)
                throw new Exception("OTP has expired. Please request a new one.");

            // ✅ OTP is valid → generate a short-lived reset token (or flag)
            var resetSessionToken = Guid.NewGuid().ToString();

            // Store it temporarily (example: as another auth token)
            await _userManager.SetAuthenticationTokenAsync(user, "Default", "ResetSession", resetSessionToken);

            // Optional: clear failed attempts
            await _userManager.ResetAccessFailedCountAsync(user);

            return resetSessionToken; // Frontend will use this to open reset password page
        }

        public async Task<string> ResetPasswordAfterOtpAsync(ResetPasswordDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new Exception("Invalid request.");

            var sessionToken = await _userManager.GetAuthenticationTokenAsync(user, "Default", "ResetSession");
            if (string.IsNullOrEmpty(sessionToken) || sessionToken != dto.ResetSessionToken)
                throw new Exception("Unauthorized or expired reset session.");

            // Generate real Identity reset token
            var realToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, realToken, dto.NewPassword);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            // Cleanup: burn both tokens
            await _userManager.RemoveAuthenticationTokenAsync(user, "Default", "ResetOTP");
            await _userManager.RemoveAuthenticationTokenAsync(user, "Default", "ResetSession");

            return "Password has been reset successfully.";
        }


        public async Task<UserResponseDTO> GetCurrentUserAsync(string Email)
        {
            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                throw new Exception("User not found.");
            }
            return new UserResponseDTO
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                PictureURL = user.PictureURL,
                Gender = user.Gender,
                Age = user.Age,
                Weight = user.Weight,
                BloodType = user.BloodType
            };

        }


        private async Task<string> GenerateTokenAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.FullName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };

            var Roles = await _userManager.GetRolesAsync(user);

            foreach (var role in Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jwtOptions = _configuration.GetSection("JWTOptions").Get<JwtOptions>();

            var token = new JwtSecurityToken
            (
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: new SigningCredentials
                (
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    SecurityAlgorithms.HmacSha256
                )
                
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
