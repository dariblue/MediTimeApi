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

        // ═══════════════════════════════════════════════════════════
        //  REGISTRO
        // ═══════════════════════════════════════════════════════════

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
                if (result > 0)
                    return Ok(new { message = "Usuario registrado correctamente.", idUsuario = result, id = result });

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

        // ═══════════════════════════════════════════════════════════
        //  LOGIN (modificado: registra sesión)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Login de usuario. Devuelve datos con el nuevo esquema de roles.
        /// Registra la sesión en SesionesUsuario con IP y dispositivo.
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

                // Registrar la sesión con IP y User-Agent
                string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                string? dispositivo = Request.Headers["User-Agent"].FirstOrDefault();

                try
                {
                    _usuarioService.RegistrarSesion(usuario.IDUsuario, token, ip, dispositivo);
                }
                catch (Exception ex)
                {
                    // No bloquear el login si falla el registro de sesión
                    Console.WriteLine("Error al registrar sesión: " + ex.Message);
                }

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
                    pushToken = usuario.PushToken,
                    telefono = usuario.Telefono,
                    fechaNacimiento = usuario.FechaNacimiento,
                    domicilio = usuario.Domicilio,
                    avatarBase64 = usuario.AvatarBase64
                });
            }

            return Unauthorized("Credenciales incorrectas.");
        }

        // ═══════════════════════════════════════════════════════════
        //  GET USUARIO (modificado: incluye preferencias y notificaciones)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene un usuario por ID, incluyendo sus supervisores vinculados,
        /// preferencias de interfaz y configuración de notificaciones.
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

            // Obtener preferencias y notificaciones
            var preferencias = _usuarioService.ObtenerPreferencias(id);
            var notificaciones = _usuarioService.ObtenerNotificaciones(id);

            return Ok(new
            {
                idUsuario = usuario.IDUsuario,
                nombre = usuario.Nombre,
                apellidos = usuario.Apellidos,
                email = usuario.Email,
                rol = usuario.Rol,
                esResponsable = usuario.EsResponsable,
                pushToken = usuario.PushToken,
                telefono = usuario.Telefono,
                fechaNacimiento = usuario.FechaNacimiento,
                domicilio = usuario.Domicilio,
                avatarBase64 = usuario.AvatarBase64,
                preferencias = preferencias != null ? new
                {
                    tema = preferencias.Tema,
                    tamanoTexto = preferencias.TamanoTexto,
                    vistaCalendario = preferencias.VistaCalendario,
                    primerDiaSemana = preferencias.PrimerDiaSemana,
                    idioma = preferencias.Idioma,
                    formatoHora = preferencias.FormatoHora
                } : null,
                configuracionNotificaciones = notificaciones != null ? new
                {
                    emailMedicamentos = notificaciones.EmailMedicamentos,
                    navegadorMedicamentos = notificaciones.NavegadorMedicamentos,
                    tiempoAnticipacion = notificaciones.TiempoAnticipacion,
                    nuevasCaracteristicas = notificaciones.NuevasCaracteristicas,
                    consejos = notificaciones.Consejos
                } : null,
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

        // ═══════════════════════════════════════════════════════════
        //  BUSCAR POR EMAIL
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// GET /Usuarios/buscar?email={email}
        /// Busca un usuario por correo. Devuelve datos mínimos.
        /// </summary>
        [HttpGet("buscar")]
        public IActionResult BuscarPorEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("El email es requerido.");

            var usuario = _usuarioService.BuscarPorEmail(email);
            if (usuario != null)
            {
                return Ok(new
                {
                    existe = true,
                    usuario = new
                    {
                        id = usuario.IDUsuario,
                        nombre = usuario.Nombre + " " + usuario.Apellidos,
                        email = usuario.Email,
                        rol = usuario.Rol,
                        esResponsable = usuario.EsResponsable
                    }
                });
            }

            return Ok(new { existe = false });
        }

        // ═══════════════════════════════════════════════════════════
        //  ACTUALIZAR DATOS PERSONALES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// PUT /Usuarios/{id}
        /// Actualiza los datos personales del usuario.
        /// </summary>
        [HttpPut("{id}")]
        public IActionResult ActualizarDatosPersonales(int id, [FromBody] Usuario datos)
        {
            if (datos == null)
                return BadRequest("Datos inválidos.");

            try
            {
                var actualizado = _usuarioService.ActualizarDatosPersonales(id, datos);
                if (actualizado)
                    return Ok(new { message = "Datos actualizados correctamente." });

                return NotFound("Usuario no encontrado.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al actualizar datos: " + ex.Message);
                return StatusCode(500, "Error interno al actualizar los datos.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CAMBIAR PASSWORD
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// POST /Usuarios/cambiar-password
        /// Cambia la contraseña del usuario. Valida la contraseña actual.
        /// </summary>
        [HttpPost("cambiar-password")]
        public IActionResult CambiarPassword([FromBody] CambiarPasswordRequest request)
        {
            if (request == null || request.Id <= 0)
                return BadRequest("Datos inválidos.");

            if (string.IsNullOrWhiteSpace(request.PasswordActual) || string.IsNullOrWhiteSpace(request.PasswordNuevo))
                return BadRequest("La contraseña actual y la nueva son requeridas.");

            if (request.PasswordNuevo.Length < 6)
                return BadRequest("La nueva contraseña debe tener al menos 6 caracteres.");

            try
            {
                var resultado = _usuarioService.CambiarPassword(request.Id, request.PasswordActual, request.PasswordNuevo);
                if (resultado)
                    return Ok(new { message = "Contraseña actualizada correctamente." });

                return BadRequest(new { message = "La contraseña actual es incorrecta." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Usuario no encontrado.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cambiar contraseña: " + ex.Message);
                return StatusCode(500, "Error interno al cambiar la contraseña.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  NOTIFICACIONES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// PUT /Usuarios/{id}/notificaciones
        /// Guarda o actualiza la configuración de notificaciones.
        /// </summary>
        [HttpPut("{id}/notificaciones")]
        public IActionResult GuardarNotificaciones(int id, [FromBody] ConfiguracionNotificaciones config)
        {
            if (config == null)
                return BadRequest("Datos inválidos.");

            try
            {
                var resultado = _usuarioService.GuardarNotificaciones(id, config);
                if (resultado)
                    return Ok(new { message = "Configuración de notificaciones actualizada." });

                return StatusCode(500, "Error al guardar la configuración.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al guardar notificaciones: " + ex.Message);
                return StatusCode(500, "Error interno al guardar notificaciones.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  PREFERENCIAS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// PUT /Usuarios/{id}/preferencias
        /// Guarda o actualiza las preferencias de interfaz.
        /// </summary>
        [HttpPut("{id}/preferencias")]
        public IActionResult GuardarPreferencias(int id, [FromBody] PreferenciasUsuario prefs)
        {
            if (prefs == null)
                return BadRequest("Datos inválidos.");

            try
            {
                var resultado = _usuarioService.GuardarPreferencias(id, prefs);
                if (resultado)
                    return Ok(new { message = "Preferencias actualizadas." });

                return StatusCode(500, "Error al guardar las preferencias.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al guardar preferencias: " + ex.Message);
                return StatusCode(500, "Error interno al guardar preferencias.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  AVATAR
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// PUT /Usuarios/{id}/avatar
        /// Recibe un JSON con el string Base64 de la imagen y lo guarda.
        /// </summary>
        [HttpPut("{id}/avatar")]
        public IActionResult ActualizarAvatar(int id, [FromBody] AvatarRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AvatarBase64))
                return BadRequest("Datos inválidos. Se requiere el campo 'avatarBase64'.");

            try
            {
                var resultado = _usuarioService.ActualizarAvatar(id, request.AvatarBase64);
                if (resultado)
                    return Ok(new { message = "Avatar actualizado correctamente." });

                return NotFound("Usuario no encontrado.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al actualizar avatar: " + ex.Message);
                return StatusCode(500, "Error interno al actualizar el avatar.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  ELIMINAR CUENTA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// DELETE /Usuarios/{id}
        /// Ejecuta un borrado físico. ON DELETE CASCADE limpia todo.
        /// </summary>
        [HttpDelete("{id}")]
        public IActionResult EliminarCuenta(int id)
        {
            try
            {
                var eliminado = _usuarioService.EliminarCuenta(id);
                if (eliminado)
                    return Ok(new { message = "Cuenta eliminada correctamente." });

                return NotFound("Usuario no encontrado.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al eliminar cuenta: " + ex.Message);
                return StatusCode(500, "Error interno al eliminar la cuenta.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SESIONES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// GET /Usuarios/{id}/sesiones
        /// Devuelve la lista de sesiones activas del usuario.
        /// </summary>
        [HttpGet("{id}/sesiones")]
        public IActionResult ObtenerSesiones(int id)
        {
            try
            {
                var sesiones = _usuarioService.ObtenerSesiones(id);
                return Ok(sesiones.Select(s => new
                {
                    idSesion = s.IDSesion,
                    fechaInicio = s.FechaInicio,
                    direccionIP = s.DireccionIP,
                    dispositivo = s.Dispositivo
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener sesiones: " + ex.Message);
                return StatusCode(500, "Error interno al obtener sesiones.");
            }
        }

        /// <summary>
        /// DELETE /Usuarios/{id}/sesiones
        /// Invalida/borra todas las sesiones del usuario.
        /// </summary>
        [HttpDelete("{id}/sesiones")]
        public IActionResult EliminarSesiones(int id)
        {
            try
            {
                var eliminado = _usuarioService.EliminarSesiones(id);
                if (eliminado)
                    return Ok(new { message = "Todas las sesiones han sido cerradas." });

                return Ok(new { message = "No había sesiones activas." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al eliminar sesiones: " + ex.Message);
                return StatusCode(500, "Error interno al cerrar sesiones.");
            }
        }
    }
}
