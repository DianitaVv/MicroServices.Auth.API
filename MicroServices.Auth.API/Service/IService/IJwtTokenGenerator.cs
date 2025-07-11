using MicroServices.Auth.API.Models;

namespace MicroServices.Auth.API.Service.IService
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(ApplicationUser applicationUser, IEnumerable<string> roles);
        string GenerateToken(ApplicationUser user, string userRole);
    }
}

//esta clase se va a implementar en le extension del usuario, vamos a pasarle la lista de roles.
//es una interface y es estrictamente jwt
//esta interface se va a implementar en el servicio de jwt token generator

//el token es una cadena
//un principio solid es matener todo sencillo, con una sola responsabilidad 