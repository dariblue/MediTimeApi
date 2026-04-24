namespace MediTimeApi.Models
{
    /// <summary>
    /// DTO para el endpoint POST /Usuarios/cambiar-password.
    /// </summary>
    public class CambiarPasswordRequest
    {
        public int Id { get; set; }
        public string PasswordActual { get; set; } = string.Empty;
        public string PasswordNuevo { get; set; } = string.Empty;
    }
}
