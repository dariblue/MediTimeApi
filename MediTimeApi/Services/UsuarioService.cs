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
                       
            // Hashear contraseña antes de guardar
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(usuario.Contrasena);

            var command = new MySqlCommand("INSERT INTO Usuarios (Nombre, Apellidos, Contrasena, Fecha_Nacimiento, Email, Telefono, Domicilio, Notificaciones) VALUES (@Nombre, @Apellidos, @Contrasena, @FechaNacimiento, @Email, @Telefono, @Domicilio, @Notificaciones)", connection);

            command.Parameters.AddWithValue("@Nombre", usuario.Nombre);
            command.Parameters.AddWithValue("@Apellidos", usuario.Apellidos);
            command.Parameters.AddWithValue("@Contrasena", hashedPassword); // Usar hash
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
            
            // 1. Buscar SOLO por email
            var command = new MySqlCommand("SELECT * FROM Usuarios WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                // 2. Obtener el hash guardado
                string storedHash = reader["Contrasena"].ToString();
                bool isPasswordValid = false;

                try 
                {
                    // Intentar verificar como Hash BCrypt
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(contrasena, storedHash);
                }
                catch (Exception)
                {
                    // Fallback texto plano
                    isPasswordValid = (contrasena == storedHash); 
                }

                if (isPasswordValid)
                {
                    try 
                    {
                        var u = new Usuario();
                        // Mapeo seguro campo a campo
                        u.IdUsuario = reader["IdUsuario"] != DBNull.Value ? Convert.ToInt32(reader["IdUsuario"]) : 0;
                        u.Nombre = reader["Nombre"]?.ToString() ?? "";
                        u.Apellidos = reader["Apellidos"]?.ToString() ?? "";
                        u.Email = reader["Email"]?.ToString() ?? "";
                        u.Contrasena = ""; // Ocultar hash
                        
                        // Fecha segura
                        if (reader["Fecha_Nacimiento"] != DBNull.Value)
                        {
                            try { u.Fecha_Nacimiento = Convert.ToDateTime(reader["Fecha_Nacimiento"]); }
                            catch { u.Fecha_Nacimiento = DateTime.MinValue; }
                        }
                        
                        u.Telefono = reader["Telefono"] != DBNull.Value ? Convert.ToInt32(reader["Telefono"]) : 0;
                        u.Domicilio = reader["Domicilio"]?.ToString() ?? "";
                        
                        // Booleanos seguros (TINYINT/BIT que puede venir como byte[])
                        u.Notificaciones = false;
                        if (reader["Notificaciones"] != DBNull.Value)
                        {
                            var val = reader["Notificaciones"];
                            if (val is bool b) u.Notificaciones = b;
                            else if (val is byte[] bytes && bytes.Length > 0) u.Notificaciones = bytes[0] == 1;
                            else if (val is sbyte sb) u.Notificaciones = sb == 1;
                            else if (val is byte by) u.Notificaciones = by == 1;
                            else try { u.Notificaciones = Convert.ToBoolean(val); } catch {}
                        }

                        u.IsAdmin = false;
                        if (reader["IsAdmin"] != DBNull.Value)
                        {
                            var val = reader["IsAdmin"];
                            if (val is bool b) u.IsAdmin = b;
                            else if (val is byte[] bytes && bytes.Length > 0) u.IsAdmin = bytes[0] == 1;
                            else if (val is sbyte sb) u.IsAdmin = sb == 1;
                            else if (val is byte by) u.IsAdmin = by == 1;
                            else try { u.IsAdmin = Convert.ToBoolean(val); } catch {}
                        }
                        
                        return u;
                    }
                    catch (Exception ex)
                    {
                        // Loguear el error si pudieras, pero al menos no explotar
                        Console.WriteLine("Error mapeando usuario: " + ex.Message);
                        return null; // Devolver null hará que salga "Credenciales incorrectas" en vez de 500
                    }
                }
            }
            return null;
        }
    }
}
