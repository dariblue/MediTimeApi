using Microsoft.AspNetCore.Mvc;
using MediTimeApi.Models;
using MediTimeApi.Services;

namespace MediTimeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistorialTomasController : ControllerBase
    {
        private readonly HistorialTomaService _service;

        public HistorialTomasController(HistorialTomaService service)
        {
            _service = service;
        }

        /// <summary>
        /// POST: api/HistorialTomas
        /// Registra una toma. Valida que Estado sea 'Tomado' o 'Pasado'.
        /// </summary>
        [HttpPost]
        public IActionResult RegistrarToma([FromBody] HistorialToma toma)
        {
            if (toma == null)
                return BadRequest("Datos inválidos.");

            if (toma.IDMedicamento <= 0 || toma.IDUsuarioAccion <= 0)
                return BadRequest("IDMedicamento e IDUsuarioAccion son obligatorios.");

            try
            {
                bool registrado = _service.RegistrarToma(toma);
                if (registrado)
                    return StatusCode(201, new { message = "Toma registrada correctamente." });

                return StatusCode(500, "Error al registrar la toma.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// GET: api/HistorialTomas/medicamento/{id}
        /// Obtiene el historial de tomas de un medicamento.
        /// </summary>
        [HttpGet("medicamento/{id}")]
        public IActionResult GetPorMedicamento(int id)
        {
            var historial = _service.GetHistorialPorMedicamento(id);
            return Ok(historial);
        }

        /// <summary>
        /// GET: api/HistorialTomas/paciente/{id}
        /// Obtiene el historial de tomas de todos los medicamentos de un paciente.
        /// </summary>
        [HttpGet("paciente/{id}")]
        public IActionResult GetPorPaciente(int id)
        {
            var historial = _service.GetHistorialPorPaciente(id);
            return Ok(historial);
        }
    }
}
