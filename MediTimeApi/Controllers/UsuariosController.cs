using Microsoft.AspNetCore.Mvc;
using MediTimeApi.Models;
using MediTimeApi.Services;
using LoginRequest = MediTimeApi.Models.LoginRequest;

namespace MediTimeApi.Controllers
{
    [ApiController]
    [Route("Usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuarioService _usuarioService;
        private readonly PacienteCuidadorService _pacienteCuidadorService;

        public UsuariosController(UsuarioService usuarioService, PacienteCuidadorService pacienteCuidadorService)
        {
            _usuarioService = usuarioService;
            _pacienteCuidadorService = pacienteCuidadorService;
        }

        /// <summary>
        /// Registro de usuario con validación de regla de negocio:
        /// Si Rol='Usuario' y EsResponsable=false, exige IdResponsableAsignado.
        /// </summary>
        [HttpPost("registro")]
        public IActionResult Registrar([FromBody] RegistroRequest request)
        {
            if (request == null)
                return BadRequest("Datos inválidos.");

            if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Contrasena))
                return BadRequest("Nombre, Email y Contraseña son obligatorios.");

            // Validar roles permitidos
            var rolesPermitidos = new[] { "Usuario", "Responsable", "Cuidador" };
            if (!rolesPermitidos.Contains(request.Rol))
                return BadRequest($"Rol inválido: '{request.Rol}'. Valores permitidos: Usuario, Responsable, Cuidador.");

            // Validar regla de negocio: paciente no autónomo necesita responsable
            if (request.Rol == "Usuario" && !request.EsResponsable && (!request.IdResponsableAsignado.HasValue || request.IdResponsableAsignado.Value <= 0))
            {
                return BadRequest("Un paciente con EsResponsable=false debe incluir un IdResponsableAsignado válido.");
            }

            try
            {
                var result = _usuarioService.RegistrarUsuario(request);
                if (result)
                    return Ok(new { message = "Usuario registrado correctamente." });

                return StatusCode(500, "Error al registrar el usuario.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en registro: " + ex.Message);
                return StatusCode(500, "Error interno al registrar el usuario.");
            }
        }

        /// <summary>
        /// Login de usuario. Devuelve datos con el nuevo esquema de roles.
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.Contrasena))
                return BadRequest("Email y Contraseña son requeridos.");

            var usuario = _usuarioService.Login(loginRequest.Email, loginRequest.Contrasena);
            if (usuario != null)
            {
                string token = Guid.NewGuid().ToString();

                return Ok(new
                {
                    message = "Inicio de sesión correcto",
                    token,
                    idUsuario = usuario.IDUsuario,
                    nombre = usuario.Nombre,
                    apellidos = usuario.Apellidos,
                    email = usuario.Email,
                    rol = usuario.Rol,
                    esResponsable = usuario.EsResponsable,
                    pushToken = usuario.PushToken
                });
            }

            return Unauthorized("Credenciales incorrectas.");
        }

        /// <summary>
        /// Obtiene un usuario por ID, incluyendo sus supervisores vinculados
        /// si es paciente.
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetUsuario(int id)
        {
            var usuario = _usuarioService.GetUsuarioById(id);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            // Si es paciente, incluir cuidadores vinculados
            List<Usuario>? cuidadores = null;
            if (usuario.Rol == "Usuario")
            {
                cuidadores = _pacienteCuidadorService.GetCuidadoresDePaciente(id);
            }

            // Si es cuidador/responsable, incluir pacientes vinculados
            List<Usuario>? pacientes = null;
            if (usuario.Rol == "Responsable" || usuario.Rol == "Cuidador")
            {
                pacientes = _pacienteCuidadorService.GetPacientesDeCuidador(id);
            }

            return Ok(new
            {
                idUsuario = usuario.IDUsuario,
                nombre = usuario.Nombre,
                apellidos = usuario.Apellidos,
                email = usuario.Email,
                rol = usuario.Rol,
                esResponsable = usuario.EsResponsable,
                pushToken = usuario.PushToken,
                cuidadores = cuidadores?.Select(c => new
                {
                    idUsuario = c.IDUsuario,
                    nombre = c.Nombre,
                    apellidos = c.Apellidos,
                    email = c.Email,
                    rol = c.Rol
                }),
                pacientes = pacientes?.Select(p => new
                {
                    idUsuario = p.IDUsuario,
                    nombre = p.Nombre,
                    apellidos = p.Apellidos,
                    email = p.Email,
                    rol = p.Rol,
                    esResponsable = p.EsResponsable
                })
            });
        }
    }
}
