using Microsoft.AspNetCore.Mvc;
using MediTimeApi.Models;
using MediTimeApi.Services;

namespace MediTimeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PacienteCuidadorController : ControllerBase
    {
        private readonly PacienteCuidadorService _service;

        public PacienteCuidadorController(PacienteCuidadorService service)
        {
            _service = service;
        }

        /// <summary>
        /// POST: api/PacienteCuidador
        /// Crea un vínculo paciente↔cuidador.
        /// </summary>
        [HttpPost]
        public IActionResult CrearVinculo([FromBody] PacienteCuidador vinculo)
        {
            if (vinculo == null || vinculo.IDPaciente <= 0 || vinculo.IDCuidador <= 0)
                return BadRequest("IDPaciente e IDCuidador son obligatorios y deben ser válidos.");

            try
            {
                bool creado = _service.CrearVinculo(vinculo.IDPaciente, vinculo.IDCuidador);
                if (creado)
                    return Ok(new { message = "Vínculo creado correctamente." });

                return StatusCode(500, "Error al crear el vínculo.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al crear vínculo: " + ex.Message);
                return StatusCode(500, "Error interno al crear el vínculo.");
            }
        }

        /// <summary>
        /// DELETE: api/PacienteCuidador
        /// Elimina un vínculo paciente↔cuidador. DELETE simple, sin cascada manual.
        /// </summary>
        [HttpDelete]
        public IActionResult EliminarVinculo([FromBody] PacienteCuidador vinculo)
        {
            if (vinculo == null || vinculo.IDPaciente <= 0 || vinculo.IDCuidador <= 0)
                return BadRequest("IDPaciente e IDCuidador son obligatorios.");

            bool eliminado = _service.EliminarVinculo(vinculo.IDPaciente, vinculo.IDCuidador);
            if (eliminado)
                return Ok(new { message = "Vínculo eliminado correctamente." });

            return NotFound("Vínculo no encontrado.");
        }

        /// <summary>
        /// GET: api/PacienteCuidador/paciente/{id}
        /// Obtiene los cuidadores/responsables de un paciente.
        /// </summary>
        [HttpGet("paciente/{id}")]
        public IActionResult GetCuidadoresDePaciente(int id)
        {
            var cuidadores = _service.GetCuidadoresDePaciente(id);
            return Ok(cuidadores);
        }

        /// <summary>
        /// GET: api/PacienteCuidador/cuidador/{id}
        /// Obtiene los pacientes de un cuidador/responsable.
        /// </summary>
        [HttpGet("cuidador/{id}")]
        public IActionResult GetPacientesDeCuidador(int id)
        {
            var pacientes = _service.GetPacientesDeCuidador(id);
            return Ok(pacientes);
        }
    }
}
