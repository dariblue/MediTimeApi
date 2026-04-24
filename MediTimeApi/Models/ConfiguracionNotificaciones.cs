namespace MediTimeApi.Models
{
    public class ConfiguracionNotificaciones
    {
        public int IDConfiguracion { get; set; }
        public int IDUsuario { get; set; }
        public bool EmailMedicamentos { get; set; } = true;
        public bool NavegadorMedicamentos { get; set; } = true;
        public int TiempoAnticipacion { get; set; } = 5;
        public bool NuevasCaracteristicas { get; set; } = true;
        public bool Consejos { get; set; } = true;
    }
}
