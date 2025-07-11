using MicroServices.Auth.API.Models.Dto;

namespace MicroServices.Auth.API.Service.IService
{
    public interface IAuthService
    {
        Task<string> Register(RegistrationRequestDto registrationRequestDto);
        Task<LoginResponseDto> Login(LoginRequestDto loginRequestDto);
        Task<bool> AssignRole(string email, string roleName);

        // NUEVOS MÉTODOS para manejo de rol único
        Task<bool> UpdateUserRole(string email, string newRole);
        Task<string> GetUserRole(string email);
    }
}