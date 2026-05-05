using Microsoft.AspNetCore.Mvc;
using MediTimeApi.Models;
using MediTimeApi.Services;

namespace MediTimeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PushSubscriptionsController : ControllerBase
    {
        private readonly PushSubscriptionService _service;
        private readonly IConfiguration _configuration;
        private readonly WebPushService _webPushService;

        public PushSubscriptionsController(PushSubscriptionService service, IConfiguration configuration, WebPushService webPushService)
        {
            _service = service;
            _configuration = configuration;
            _webPushService = webPushService;
        }

        [HttpPost("subscribe")]
        public IActionResult Subscribe([FromBody] PushSubscriptionRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Endpoint))
                return BadRequest("Datos de suscripción inválidos.");

            try
            {
                bool guardado = _service.GuardarSuscripcion(request);
                if (guardado)
                    return Ok(new { message = "Suscripción guardada correctamente." });

                return StatusCode(500, "Error al guardar la suscripción.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPost("unsubscribe")]
        public IActionResult Unsubscribe([FromBody] PushSubscriptionRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Endpoint))
                return BadRequest("Endpoint es requerido.");

            try
            {
                _service.EliminarSuscripcion(request.Endpoint);
                return Ok(new { message = "Suscripción eliminada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("vapid-public-key")]
        public IActionResult GetVapidPublicKey()
        {
            var publicKey = _configuration["VapidDetails:PublicKey"];
            if (string.IsNullOrEmpty(publicKey))
            {
                return StatusCode(500, "VAPID Public Key no está configurada en el servidor.");
            }
            
            // Return base64url encoded key for frontend
            var base64UrlKey = publicKey.Replace("+", "-").Replace("/", "_").Replace("=", "");
            return Ok(new { publicKey = base64UrlKey });
        }

        [HttpPost("test-delayed-push")]
        public IActionResult TestDelayedPush([FromBody] PushSubscriptionRequest request)
        {
            if (request == null || request.IdUsuario <= 0)
                return BadRequest("ID de usuario requerido.");

            var userId = request.IdUsuario;
            var subscriptions = _service.ObtenerSuscripcionesPorUsuario(userId);

            if (subscriptions == null || !subscriptions.Any())
                return BadRequest("El usuario no tiene suscripciones Push.");

            // Disparar tarea en segundo plano (Fire and forget)
            Task.Run(async () =>
            {
                await Task.Delay(60000); // 1 minuto
                var payload = new
                {
                    title = "¡Test Exitoso desde el Servidor!",
                    body = "Si estás viendo esto con la app cerrada, tu Web Push funciona perfectamente en producción.",
                    tag = $"test-{DateTime.Now.Ticks}",
                    idMedicamento = 0
                };
                
                string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

                foreach (var sub in subscriptions)
                {
                    await _webPushService.SendPushNotificationAsync(sub, jsonPayload);
                }
            });

            return Ok(new { message = "Prueba de notificación programada para dentro de 1 minuto." });
        }
    }
}
