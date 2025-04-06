using MySql.Data.MySqlClient;
using MediTimeApi.Models;
using MediTimeApi.Models;

namespace MediTimeApi.Services
{
    public class UsuarioService
    {
        private readonly string _connectionString;

        public UsuarioService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public bool RegistrarUsuario(Usuario usuario)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var command = new MySqlCommand("INSERT INTO Usuarios (Nombre, Apellidos, Contrasena, Fecha_Nacimiento, Email, Telefono, Domicilio, Notificaciones) VALUES (@Nombre, @Apellidos, @Contrasena, @FechaNacimiento, @Email, @Telefono, @Domicilio, @Notificaciones)", connection);

            command.Parameters.AddWithValue("@Nombre", usuario.Nombre);
            command.Parameters.AddWithValue("@Apellidos", usuario.Apellidos);
            command.Parameters.AddWithValue("@Contrasena", usuario.Contrasena);
            command.Parameters.AddWithValue("@FechaNacimiento", usuario.Fecha_Nacimiento);
            command.Parameters.AddWithValue("@Email", usuario.Email);
            command.Parameters.AddWithValue("@Telefono", usuario.Telefono);
            command.Parameters.AddWithValue("@Domicilio", usuario.Domicilio);
            command.Parameters.AddWithValue("@Notificaciones", usuario.Notificaciones);

            return command.ExecuteNonQuery() > 0;
        }

        public Usuario? Login(string email, string contrasena)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var command = new MySqlCommand("SELECT * FROM Usuarios WHERE Email = @Email AND Contrasena = @Contrasena", connection);
            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@Contrasena", contrasena);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Usuario
                {
                    ID_Usuario = Convert.ToInt32(reader["ID_Usuario"]),
                    Nombre = reader["Nombre"].ToString(),
                    Apellidos = reader["Apellidos"].ToString(),
                    Email = reader["Email"].ToString(),
                    Contrasena = "", // nunca devuelvas la contraseña real
                    Fecha_Nacimiento = Convert.ToDateTime(reader["Fecha_Nacimiento"]),
                    Telefono = Convert.ToInt32(reader["Telefono"]),
                    Domicilio = reader["Domicilio"].ToString(),
                    Notificaciones = ((byte[])reader["Notificaciones"])[0] == 1,
                    IsAdmin = Convert.ToBoolean(reader["IsAdmin"])
                };
            }
            return null;
        }
    }
}
