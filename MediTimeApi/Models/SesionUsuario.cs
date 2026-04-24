namespace MediTimeApi.Models
{
    public class SesionUsuario
    {
        public int IDSesion { get; set; }
        public int IDUsuario { get; set; }
        public string TokenSesion { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public string? DireccionIP { get; set; }
        public string? Dispositivo { get; set; }
    }
}
