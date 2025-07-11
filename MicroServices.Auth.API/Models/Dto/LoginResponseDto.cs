namespace MicroServices.Auth.API.Models.Dto
{
    public class LoginResponseDto
    {
        public UserDto User { get; set; }
        public string Token { get; set; } //se genera del lado del servidor

        //encriptar el token, no lo olvides, hash o salt
    }
}
