namespace MicroServices.Auth.API.Models.Dto
{//respuesta genérica
    public class ResponseDto
    {
        public object Result { get; set; } //devuelve un objeto, en este caso, un json
        public bool IsSuccess { get; set; } = true; //si la petición es 200, es success
        public string Message { get; set; } = string.Empty; //mensaje de error 
    }
}
