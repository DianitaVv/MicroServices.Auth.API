using MicroServices.Auth.API.Models.Dto;
using MicroServices.Auth.API.Service;
using MicroServices.Auth.API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MicroServices.Auth.API.Controllers
{
    [Route("api/[controller]")]//específico para microservicios 
    [ApiController]
    public class AuthAPIController : ControllerBase
    {
        private readonly IAuthService _authService;//inyección de dependencias 

        protected ResponseDto _response;
        public AuthAPIController(IAuthService authService)
        {
            _authService = authService;
            _response = new();//creacion del obj, c#12
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationRequestDto model)
        {
            var errorMessage = await _authService.Register(model);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                _response.IsSuccess = false;
                _response.Message = errorMessage;
                return BadRequest(_response);
            }
            return Ok(_response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            var loginResponse = await _authService.Login(model);
            if (loginResponse.User == null)
            {
                _response.IsSuccess = false;
                _response.Message = "El nombre de usuario o la contraseña es incorrecta";
                return BadRequest(_response);
            }
            //logear
            _response.Result = loginResponse;
            return Ok(_response);
        }

        [HttpPost("AssignRole")]
        public async Task<IActionResult> AssignRole([FromBody] RegistrationRequestDto model)
        {
            // CORRECCIÓN: Ahora reescribe el rol en lugar de agregarlo
            var assignRoleSuccessful = await _authService.UpdateUserRole(model.Email, model.Role.ToUpper());
            if (!assignRoleSuccessful)
            {
                _response.IsSuccess = false;
                _response.Message = "Error en la asignación del rol";
                return BadRequest(_response);
            }

            _response.Message = $"Rol '{model.Role.ToUpper()}' asignado correctamente al usuario {model.Email}";
            return Ok(_response);
        }

        // NUEVO ENDPOINT: Para obtener el rol actual del usuario
        [HttpGet("GetUserRole/{email}")]
        public async Task<IActionResult> GetUserRole(string email)
        {
            var userRole = await _authService.GetUserRole(email);
            if (string.IsNullOrEmpty(userRole))
            {
                _response.IsSuccess = false;
                _response.Message = "Usuario no encontrado o sin rol asignado";
                return NotFound(_response);
            }

            _response.Result = new { Email = email, Role = userRole };
            return Ok(_response);
        }
    }
}