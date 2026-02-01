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
            // Generar un token falso (o real si implementaras JWT) para que el frontend no falle
            string fakeToken = Guid.NewGuid().ToString();

            return Ok(new
            {
                message = "Inicio de sesión correcto",
                token = fakeToken, // auth.js lo necesita
                idUsuario = usuario.IdUsuario, // auth.js lo usa como userId
                nombre = usuario.Nombre,
                apellidos = usuario.Apellidos,
                email = usuario.Email,
                isAdmin = usuario.IsAdmin ? 1 : 0, // auth.js espera 1 o 0 (o bool si comprueba === 1)
                // Otros datos si fueran necesarios
                fecha_Nacimiento = usuario.Fecha_Nacimiento,
                telefono = usuario.Telefono,
                domicilio = usuario.Domicilio,
                notificaciones = usuario.Notificaciones
            });
        }

        return Unauthorized("Credenciales incorrectas");
    }
}
