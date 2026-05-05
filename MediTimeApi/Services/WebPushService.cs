using WebPush;
using MediTimeApi.Models;

namespace MediTimeApi.Services
{
    public class WebPushService
    {
        private readonly IConfiguration _configuration;
        private readonly WebPushClient _webPushClient;

        public WebPushService(IConfiguration configuration)
        {
            _configuration = configuration;
            _webPushClient = new WebPushClient();
        }

        public async Task<bool> SendPushNotificationAsync(PushSubscriptionModel subscription, string payload)
        {
            var subject = _configuration["VapidDetails:Subject"];
            var publicKey = _configuration["VapidDetails:PublicKey"];
            var privateKey = _configuration["VapidDetails:PrivateKey"];

            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            {
                Console.WriteLine("[WebPushService] Falta configuración de VAPID en appsettings.json.");
                return false;
            }

            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
            var pushSubscription = new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);

            try
            {
                await _webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                return true;
            }
            catch (WebPushException exception)
            {
                Console.WriteLine($"[WebPushService] HTTP Status Code: {exception.StatusCode}");
                Console.WriteLine($"[WebPushService] Error: {exception.Message}");
                // Si el status es 410 (Gone) o 404 (Not Found), la suscripción expiró o ya no es válida.
                // En un sistema en producción, deberíamos borrarla de la BBDD.
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebPushService] Error general: {ex.Message}");
                return false;
            }
        }
    }
}
