using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs;

namespace BLL.Abstractions.Authentication
{
    public interface IAuthenticationService
    {
        Task<string> ForgetPasswordAsync(string Email);
        Task<string> LoginAsync(LoginDTO loginDTO);
        Task<UserDTO> RegisterAsync(RegisterDTO registerDTO);
        Task<string> ResetPasswordAsync(ResetPasswordDTO resetPasswordDTO);

        Task<UserResponseDTO> GetCurrentUserAsync(string Email);
    }
}
