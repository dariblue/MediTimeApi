using Microsoft.AspNetCore.Mvc;
using MediTimeApi.Models;
using MediTimeApi.Services;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Identity.Data;
using LoginRequest = MediTimeApi.Models.LoginRequest;

[ApiController]
[Route("Usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _usuarioService;

    public UsuariosController(UsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    // Registro de usuario (no cambia)
    [HttpPost("registro")]
    public IActionResult Registrar([FromBody] Usuario nuevoUsuario)
    {
        var result = _usuarioService.RegistrarUsuario(nuevoUsuario);
        if (result)
        {
            return Ok(new { message = "Usuario registrado" });
        }
        return BadRequest("Error al registrar");
    }

    // Login de usuario con LoginRequest
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        // Verifica que los campos requeridos están presentes
        if (string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Contrasena))
        {
            return BadRequest("Email y Contraseña son requeridos.");
        }

        var usuario = _usuarioService.Login(loginRequest.Email, loginRequest.Contrasena);
        if (usuario != null)
        {
            return Ok(new
            {
                message = "Inicio de sesión correcto",
                usuario = new
                {
                    usuario.Nombre,
                    usuario.Apellidos,
                    usuario.Email,
                    usuario.Fecha_Nacimiento,
                    usuario.Telefono,
                    usuario.Domicilio,
                    usuario.Notificaciones,
                    usuario.IsAdmin
                }
            });
        }

        return Unauthorized("Credenciales incorrectas");
    }
}
