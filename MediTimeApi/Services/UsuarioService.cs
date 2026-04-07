using MySql.Data.MySqlClient;
using MediTimeApi.Models;

namespace MediTimeApi.Services
{
    public class UsuarioService
    {
        private readonly Database _database;

        public UsuarioService(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Registra un nuevo usuario. Si Rol='Usuario' y EsResponsable=false,
        /// exige IdResponsableAsignado y crea el vínculo en PACIENTE_CUIDADOR
        /// de forma atómica (transacción).
        /// </summary>
        public bool RegistrarUsuario(RegistroRequest request)
        {
            // Validar regla de negocio: paciente no autónomo necesita responsable
            if (request.Rol == "Usuario" && !request.EsResponsable)
            {
                if (!request.IdResponsableAsignado.HasValue || request.IdResponsableAsignado.Value <= 0)
                {
                    throw new ArgumentException(
                        "Un paciente con EsResponsable=false debe tener un IdResponsableAsignado válido.");
                }
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Contrasena);

            using var connection = _database.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // INSERT del usuario
                var insertUsuario = new MySqlCommand(
                    @"INSERT INTO USUARIOS (Nombre, Apellidos, Email, Contrasena, Rol, EsResponsable, PushToken)
                      VALUES (@Nombre, @Apellidos, @Email, @Contrasena, @Rol, @EsResponsable, @PushToken)",
                    connection, transaction);

                insertUsuario.Parameters.AddWithValue("@Nombre", request.Nombre);
                insertUsuario.Parameters.AddWithValue("@Apellidos", request.Apellidos);
                insertUsuario.Parameters.AddWithValue("@Email", request.Email);
                insertUsuario.Parameters.AddWithValue("@Contrasena", hashedPassword);
                insertUsuario.Parameters.AddWithValue("@Rol", request.Rol);
                insertUsuario.Parameters.AddWithValue("@EsResponsable", request.EsResponsable);
                insertUsuario.Parameters.AddWithValue("@PushToken", (object?)request.PushToken ?? DBNull.Value);

                insertUsuario.ExecuteNonQuery();
                long nuevoId = insertUsuario.LastInsertedId;

                // Si es paciente no autónomo, crear vínculo con responsable
                if (request.Rol == "Usuario" && !request.EsResponsable)
                {
                    var insertVinculo = new MySqlCommand(
                        @"INSERT INTO PACIENTE_CUIDADOR (IDPaciente, IDCuidador) VALUES (@IDPaciente, @IDCuidador)",
                        connection, transaction);

                    insertVinculo.Parameters.AddWithValue("@IDPaciente", nuevoId);
                    insertVinculo.Parameters.AddWithValue("@IDCuidador", request.IdResponsableAsignado!.Value);
                    insertVinculo.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Login: busca por email y verifica contraseña con bcrypt.
        /// </summary>
        public Usuario? Login(string email, string contrasena)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "SELECT IDUsuario, Nombre, Apellidos, Email, Contrasena, Rol, EsResponsable, PushToken FROM USUARIOS WHERE Email = @Email",
                connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                string storedHash = reader["Contrasena"]?.ToString() ?? "";
                bool isValid = false;

                try
                {
                    isValid = BCrypt.Net.BCrypt.Verify(contrasena, storedHash);
                }
                catch
                {
                    // Fallback texto plano (para migraciones pendientes)
                    isValid = contrasena == storedHash;
                }

                if (isValid)
                {
                    return MapUsuario(reader);
                }
            }

            return null;
        }

        /// <summary>
        /// Obtiene un usuario por ID, incluyendo la lista de sus supervisores
        /// si es un paciente (JOIN con PACIENTE_CUIDADOR).
        /// </summary>
        public Usuario? GetUsuarioById(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "SELECT IDUsuario, Nombre, Apellidos, Email, Rol, EsResponsable, PushToken FROM USUARIOS WHERE IDUsuario = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapUsuario(reader);
            }

            return null;
        }

        /// <summary>
        /// Mapea un MySqlDataReader a un objeto Usuario (sin exponer la contraseña).
        /// </summary>
        private Usuario MapUsuario(MySqlDataReader reader)
        {
            var u = new Usuario
            {
                IDUsuario = Convert.ToInt32(reader["IDUsuario"]),
                Nombre = reader["Nombre"]?.ToString() ?? "",
                Apellidos = reader["Apellidos"]?.ToString() ?? "",
                Email = reader["Email"]?.ToString() ?? "",
                Contrasena = "", // Nunca devolver el hash
                Rol = reader["Rol"]?.ToString() ?? "Usuario",
                EsResponsable = ConvertToBool(reader["EsResponsable"]),
                PushToken = reader["PushToken"] != DBNull.Value ? reader["PushToken"]?.ToString() : null
            };
            return u;
        }

        /// <summary>
        /// Convierte de forma segura valores BIT/TINYINT de MariaDB a bool.
        /// </summary>
        private bool ConvertToBool(object val)
        {
            if (val == null || val == DBNull.Value) return false;
            if (val is bool b) return b;
            if (val is sbyte sb) return sb == 1;
            if (val is byte by) return by == 1;
            if (val is byte[] bytes && bytes.Length > 0) return bytes[0] == 1;
            try { return Convert.ToBoolean(val); } catch { return false; }
        }
    }
}
