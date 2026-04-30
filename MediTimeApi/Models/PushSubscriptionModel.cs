namespace MediTimeApi.Models
{
    /// <summary>
    /// Modelo que representa una suscripción push almacenada en BD.
    /// Tabla: PUSH_SUBSCRIPTIONS
    /// </summary>
    public class PushSubscriptionModel
    {
        public int IDSubscription { get; set; }
        public int IDUsuario { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }
}
