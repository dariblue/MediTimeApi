namespace MediTimeApi.Models
{
    public class Usuario
    {
        public int ID_Usuario { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string Contrasena { get; set; }
        public DateTime Fecha_Nacimiento { get; set; }
        public string Email { get; set; }
        public int Telefono { get; set; }
        public string Domicilio { get; set; }
        public bool Notificaciones { get; set; }
        public bool IsAdmin { get; set; }

    }
}
