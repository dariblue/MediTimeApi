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

        public PushSubscriptionsController(PushSubscriptionService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
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
    }
}
