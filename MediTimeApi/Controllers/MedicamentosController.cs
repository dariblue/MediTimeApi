using Microsoft.AspNetCore.Mvc;
using MediTimeApi.Models;
using MediTimeApi.Services;
using System.Collections.Generic;

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

        // GET: api/Medicamentos/usuario/{id}
        [HttpGet("usuario/{id}")]
        public IActionResult GetPorUsuario(int id)
        {
            var meds = _service.GetMedicamentosByUsuario(id);
            return Ok(meds);
        }

        // POST: api/Medicamentos
        [HttpPost]
        public IActionResult Crear([FromBody] Medicamento nuevoMedicamento)
        {
            if (nuevoMedicamento == null)
            {
                return BadRequest("Datos inválidos");
            }

            // Validaciones básicas
            if (string.IsNullOrEmpty(nuevoMedicamento.Nombre))
            {
                return BadRequest("El nombre es obligatorio");
            }

            bool creado = _service.CreateMedicamento(nuevoMedicamento);
            if (creado)
            {
                // Podríamos devolver el objeto creado con su ID, 
                // pero por ahora devolvemos lo que tenemos y un 201 Created
                return StatusCode(201, nuevoMedicamento);
            }
            else
            {
                return StatusCode(500, "Error al crear el medicamento en base de datos");
            }
        }

        // PUT: api/Medicamentos/{id}
        [HttpPut("{id}")]
        public IActionResult Modificar(int id, [FromBody] Medicamento med)
        {
            if (med == null) return BadRequest("Datos inválidos");
            if (id != med.IdMedicamentos && med.IdMedicamentos != 0) return BadRequest("ID no coincide");

            // Si el cliente no mandó ID en el body, lo usamos de la ruta
            if (med.IdMedicamentos == 0) med.IdMedicamentos = id;

            bool actualizado = _service.UpdateMedicamento(id, med);
            if (actualizado)
            {
                // Devolvemos el mismo objeto actualizado (o lo que queramos)
                return Ok(med);
            }
            else
            {
                return NotFound("Medicamento no encontrado o no se pudo actualizar");
            }
        }
    }
}
