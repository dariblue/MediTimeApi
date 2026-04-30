namespace MediTimeApi.Models
{
    /// <summary>
    /// DTO para recibir suscripciones push desde el frontend.
    /// </summary>
    public class PushSubscriptionRequest
    {
        public int IdUsuario { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }
}
