using Microsoft.AspNetCore.Mvc;
using MediTimeApi.Models;
using MediTimeApi.Services;

namespace MediTimeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicamentosController : ControllerBase
    {
        private readonly MedicamentoService _service;

        public MedicamentosController(MedicamentoService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET: api/Medicamentos/paciente/{id}
        /// Obtiene todos los medicamentos de un paciente.
        /// </summary>
        [HttpGet("paciente/{id}")]
        public IActionResult GetPorPaciente(int id)
        {
            var meds = _service.GetMedicamentosByPaciente(id);
            return Ok(meds);
        }

        /// <summary>
        /// GET: api/Medicamentos/{id}
        /// Obtiene un medicamento por su ID.
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetPorId(int id)
        {
            var med = _service.GetMedicamentoById(id);
            if (med == null)
                return NotFound("Medicamento no encontrado.");

            return Ok(med);
        }

        /// <summary>
        /// POST: api/Medicamentos
        /// Crea un nuevo medicamento.
        /// </summary>
        [HttpPost]
        public IActionResult Crear([FromBody] Medicamento nuevoMedicamento)
        {
            if (nuevoMedicamento == null)
                return BadRequest("Datos inválidos.");

            if (string.IsNullOrWhiteSpace(nuevoMedicamento.Nombre))
                return BadRequest("El nombre del medicamento es obligatorio.");

            if (nuevoMedicamento.IDUsuarioPaciente <= 0)
                return BadRequest("El IDUsuarioPaciente es obligatorio.");

            bool creado = _service.CreateMedicamento(nuevoMedicamento);
            if (creado)
                return StatusCode(201, nuevoMedicamento);

            return StatusCode(500, "Error al crear el medicamento.");
        }

        /// <summary>
        /// PUT: api/Medicamentos/{id}
        /// Actualiza un medicamento existente.
        /// </summary>
        [HttpPut("{id}")]
        public IActionResult Modificar(int id, [FromBody] Medicamento med)
        {
            if (med == null)
                return BadRequest("Datos inválidos.");

            if (id != med.IDMedicamento && med.IDMedicamento != 0)
                return BadRequest("ID no coincide.");

            if (med.IDMedicamento == 0)
                med.IDMedicamento = id;

            bool actualizado = _service.UpdateMedicamento(id, med);
            if (actualizado)
                return Ok(med);

            return NotFound("Medicamento no encontrado o no se pudo actualizar.");
        }

        /// <summary>
        /// DELETE: api/Medicamentos/{id}
        /// Elimina un medicamento. MariaDB cascadea HISTORIAL_TOMAS.
        /// </summary>
        [HttpDelete("{id}")]
        public IActionResult Eliminar(int id)
        {
            bool eliminado = _service.DeleteMedicamento(id);
            if (eliminado)
                return Ok(new { message = "Medicamento eliminado correctamente." });

            return NotFound("Medicamento no encontrado.");
        }
    }
}
