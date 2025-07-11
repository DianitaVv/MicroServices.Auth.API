namespace MicroServices.Auth.API.Models.Dto
{
    public class UserDto
    {
        public string ID { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; } // Cambiado de string[] a string para un solo rol
    }
}