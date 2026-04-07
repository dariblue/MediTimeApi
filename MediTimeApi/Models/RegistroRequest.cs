namespace MediTimeApi.Models
{
    /// <summary>
    /// Request body para el endpoint POST /Usuarios/registro.
    /// Extiende los datos del usuario con el campo opcional IdResponsableAsignado,
    /// que es OBLIGATORIO cuando Rol='Usuario' y EsResponsable=false.
    /// </summary>
    public class RegistroRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string Rol { get; set; } = "Usuario";
        public bool EsResponsable { get; set; }
        public string? PushToken { get; set; }

        /// <summary>
        /// ID del responsable/cuidador asignado.
        /// Obligatorio si Rol='Usuario' y EsResponsable=false.
        /// </summary>
        public int? IdResponsableAsignado { get; set; }
    }
}
