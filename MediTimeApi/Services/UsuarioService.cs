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

        // ═══════════════════════════════════════════════════════════
        //  REGISTRO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra un nuevo usuario. Si Rol='Usuario' y EsResponsable=false,
        /// exige IdResponsableAsignado y crea el vínculo en PACIENTE_CUIDADOR
        /// de forma atómica (transacción).
        /// </summary>
        public int RegistrarUsuario(RegistroRequest request)
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
                    @"INSERT INTO USUARIOS (Nombre, Apellidos, Email, Contrasena, Rol, EsResponsable, PushToken, Telefono, FechaNacimiento)
                      VALUES (@Nombre, @Apellidos, @Email, @Contrasena, @Rol, @EsResponsable, @PushToken, @Telefono, @FechaNacimiento)",
                    connection, transaction);

                insertUsuario.Parameters.AddWithValue("@Nombre", request.Nombre);
                insertUsuario.Parameters.AddWithValue("@Apellidos", request.Apellidos);
                insertUsuario.Parameters.AddWithValue("@Email", request.Email);
                insertUsuario.Parameters.AddWithValue("@Contrasena", hashedPassword);
                insertUsuario.Parameters.AddWithValue("@Rol", request.Rol);
                insertUsuario.Parameters.AddWithValue("@EsResponsable", request.EsResponsable);
                insertUsuario.Parameters.AddWithValue("@PushToken", (object?)request.PushToken ?? DBNull.Value);
                insertUsuario.Parameters.AddWithValue("@Telefono", (object?)request.Telefono ?? DBNull.Value);
                insertUsuario.Parameters.AddWithValue("@FechaNacimiento", (object?)request.FechaNacimiento ?? DBNull.Value);

                insertUsuario.ExecuteNonQuery();
                int nuevoId = (int)insertUsuario.LastInsertedId;

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
                return nuevoId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  LOGIN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Login: busca por email y verifica contraseña con bcrypt.
        /// </summary>
        public Usuario? Login(string email, string contrasena)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "SELECT IDUsuario, Nombre, Apellidos, Email, Contrasena, Rol, EsResponsable, PushToken, Telefono, FechaNacimiento, Domicilio, AvatarBase64 FROM USUARIOS WHERE Email = @Email",
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

        // ═══════════════════════════════════════════════════════════
        //  GET USUARIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene un usuario por ID, incluyendo la lista de sus supervisores
        /// si es un paciente (JOIN con PACIENTE_CUIDADOR).
        /// </summary>
        public Usuario? GetUsuarioById(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "SELECT IDUsuario, Nombre, Apellidos, Email, Rol, EsResponsable, PushToken, Telefono, FechaNacimiento, Domicilio, AvatarBase64 FROM USUARIOS WHERE IDUsuario = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapUsuario(reader);
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════
        //  BUSCAR POR EMAIL
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Busca un usuario por email. Devuelve datos mínimos (sin contraseña ni avatar).
        /// </summary>
        public Usuario? BuscarPorEmail(string email)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "SELECT IDUsuario, Nombre, Apellidos, Email, Rol, EsResponsable FROM USUARIOS WHERE Email = @Email",
                connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Usuario
                {
                    IDUsuario = Convert.ToInt32(reader["IDUsuario"]),
                    Nombre = reader["Nombre"]?.ToString() ?? "",
                    Apellidos = reader["Apellidos"]?.ToString() ?? "",
                    Email = reader["Email"]?.ToString() ?? "",
                    Contrasena = "", // Nunca exponer
                    Rol = reader["Rol"]?.ToString() ?? "Usuario",
                    EsResponsable = ConvertToBool(reader["EsResponsable"])
                };
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════
        //  ACTUALIZAR DATOS PERSONALES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Actualiza los datos personales (Nombre, Apellidos, Email, Telefono, FechaNacimiento, Domicilio).
        /// </summary>
        public bool ActualizarDatosPersonales(int id, Usuario datos)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"UPDATE USUARIOS 
                  SET Nombre = @Nombre, 
                      Apellidos = @Apellidos, 
                      Email = @Email, 
                      Telefono = @Telefono, 
                      FechaNacimiento = @FechaNacimiento, 
                      Domicilio = @Domicilio 
                  WHERE IDUsuario = @Id",
                connection);

            command.Parameters.AddWithValue("@Nombre", datos.Nombre);
            command.Parameters.AddWithValue("@Apellidos", datos.Apellidos);
            command.Parameters.AddWithValue("@Email", datos.Email);
            command.Parameters.AddWithValue("@Telefono", (object?)datos.Telefono ?? DBNull.Value);
            command.Parameters.AddWithValue("@FechaNacimiento", (object?)datos.FechaNacimiento ?? DBNull.Value);
            command.Parameters.AddWithValue("@Domicilio", (object?)datos.Domicilio ?? DBNull.Value);
            command.Parameters.AddWithValue("@Id", id);

            return command.ExecuteNonQuery() > 0;
        }

        // ═══════════════════════════════════════════════════════════
        //  CAMBIAR PASSWORD
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Cambia la contraseña. Valida la actual con BCrypt antes de actualizar.
        /// Devuelve: true=OK, false=contraseña actual incorrecta.
        /// Lanza excepción si el usuario no existe.
        /// </summary>
        public bool CambiarPassword(int id, string passwordActual, string passwordNuevo)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            // 1. Obtener el hash actual
            var selectCmd = new MySqlCommand(
                "SELECT Contrasena FROM USUARIOS WHERE IDUsuario = @Id",
                connection);
            selectCmd.Parameters.AddWithValue("@Id", id);

            var storedHash = selectCmd.ExecuteScalar()?.ToString();
            if (storedHash == null)
                throw new KeyNotFoundException("Usuario no encontrado.");

            // 2. Verificar contraseña actual
            bool isValid;
            try
            {
                isValid = BCrypt.Net.BCrypt.Verify(passwordActual, storedHash);
            }
            catch
            {
                // Fallback texto plano
                isValid = passwordActual == storedHash;
            }

            if (!isValid) return false;

            // 3. Hashear la nueva y actualizar
            string nuevoHash = BCrypt.Net.BCrypt.HashPassword(passwordNuevo);

            var updateCmd = new MySqlCommand(
                "UPDATE USUARIOS SET Contrasena = @Contrasena WHERE IDUsuario = @Id",
                connection);
            updateCmd.Parameters.AddWithValue("@Contrasena", nuevoHash);
            updateCmd.Parameters.AddWithValue("@Id", id);

            return updateCmd.ExecuteNonQuery() > 0;
        }

        // ═══════════════════════════════════════════════════════════
        //  AVATAR
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Guarda el avatar como string Base64 en la columna AvatarBase64.
        /// </summary>
        public bool ActualizarAvatar(int id, string avatarBase64)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "UPDATE USUARIOS SET AvatarBase64 = @Avatar WHERE IDUsuario = @Id",
                connection);
            command.Parameters.AddWithValue("@Avatar", avatarBase64);
            command.Parameters.AddWithValue("@Id", id);

            return command.ExecuteNonQuery() > 0;
        }

        // ═══════════════════════════════════════════════════════════
        //  ELIMINAR CUENTA (borrado físico)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Elimina físicamente al usuario. ON DELETE CASCADE en las FK
        /// limpia automáticamente todas las tablas relacionadas.
        /// </summary>
        public bool EliminarCuenta(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "DELETE FROM USUARIOS WHERE IDUsuario = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", id);

            return command.ExecuteNonQuery() > 0;
        }

        // ═══════════════════════════════════════════════════════════
        //  NOTIFICACIONES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Guarda o actualiza la configuración de notificaciones (UPSERT).
        /// </summary>
        public bool GuardarNotificaciones(int idUsuario, ConfiguracionNotificaciones config)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"INSERT INTO ConfiguracionNotificaciones 
                    (IDUsuario, EmailMedicamentos, NavegadorMedicamentos, TiempoAnticipacion, NuevasCaracteristicas, Consejos)
                  VALUES 
                    (@IDUsuario, @EmailMeds, @NavMeds, @Tiempo, @Nuevas, @Consejos)
                  ON DUPLICATE KEY UPDATE
                    EmailMedicamentos = @EmailMeds,
                    NavegadorMedicamentos = @NavMeds,
                    TiempoAnticipacion = @Tiempo,
                    NuevasCaracteristicas = @Nuevas,
                    Consejos = @Consejos",
                connection);

            command.Parameters.AddWithValue("@IDUsuario", idUsuario);
            command.Parameters.AddWithValue("@EmailMeds", config.EmailMedicamentos);
            command.Parameters.AddWithValue("@NavMeds", config.NavegadorMedicamentos);
            command.Parameters.AddWithValue("@Tiempo", config.TiempoAnticipacion);
            command.Parameters.AddWithValue("@Nuevas", config.NuevasCaracteristicas);
            command.Parameters.AddWithValue("@Consejos", config.Consejos);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Obtiene la configuración de notificaciones de un usuario.
        /// </summary>
        public ConfiguracionNotificaciones? ObtenerNotificaciones(int idUsuario)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "SELECT IDConfiguracion, IDUsuario, EmailMedicamentos, NavegadorMedicamentos, TiempoAnticipacion, NuevasCaracteristicas, Consejos FROM ConfiguracionNotificaciones WHERE IDUsuario = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", idUsuario);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new ConfiguracionNotificaciones
                {
                    IDConfiguracion = Convert.ToInt32(reader["IDConfiguracion"]),
                    IDUsuario = Convert.ToInt32(reader["IDUsuario"]),
                    EmailMedicamentos = ConvertToBool(reader["EmailMedicamentos"]),
                    NavegadorMedicamentos = ConvertToBool(reader["NavegadorMedicamentos"]),
                    TiempoAnticipacion = Convert.ToInt32(reader["TiempoAnticipacion"]),
                    NuevasCaracteristicas = ConvertToBool(reader["NuevasCaracteristicas"]),
                    Consejos = ConvertToBool(reader["Consejos"])
                };
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════
        //  PREFERENCIAS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Guarda o actualiza las preferencias de interfaz (UPSERT).
        /// </summary>
        public bool GuardarPreferencias(int idUsuario, PreferenciasUsuario prefs)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"INSERT INTO PreferenciasUsuario 
                    (IDUsuario, Tema, TamanoTexto, VistaCalendario, PrimerDiaSemana, Idioma, FormatoHora)
                  VALUES 
                    (@IDUsuario, @Tema, @TamanoTexto, @VistaCal, @PrimerDia, @Idioma, @FormatoHora)
                  ON DUPLICATE KEY UPDATE
                    Tema = @Tema,
                    TamanoTexto = @TamanoTexto,
                    VistaCalendario = @VistaCal,
                    PrimerDiaSemana = @PrimerDia,
                    Idioma = @Idioma,
                    FormatoHora = @FormatoHora",
                connection);

            command.Parameters.AddWithValue("@IDUsuario", idUsuario);
            command.Parameters.AddWithValue("@Tema", prefs.Tema);
            command.Parameters.AddWithValue("@TamanoTexto", prefs.TamanoTexto);
            command.Parameters.AddWithValue("@VistaCal", prefs.VistaCalendario);
            command.Parameters.AddWithValue("@PrimerDia", prefs.PrimerDiaSemana);
            command.Parameters.AddWithValue("@Idioma", prefs.Idioma);
            command.Parameters.AddWithValue("@FormatoHora", prefs.FormatoHora);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Obtiene las preferencias de interfaz de un usuario.
        /// </summary>
        public PreferenciasUsuario? ObtenerPreferencias(int idUsuario)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "SELECT IDPreferencia, IDUsuario, Tema, TamanoTexto, VistaCalendario, PrimerDiaSemana, Idioma, FormatoHora FROM PreferenciasUsuario WHERE IDUsuario = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", idUsuario);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new PreferenciasUsuario
                {
                    IDPreferencia = Convert.ToInt32(reader["IDPreferencia"]),
                    IDUsuario = Convert.ToInt32(reader["IDUsuario"]),
                    Tema = reader["Tema"]?.ToString() ?? "light",
                    TamanoTexto = reader["TamanoTexto"]?.ToString() ?? "medium",
                    VistaCalendario = reader["VistaCalendario"]?.ToString() ?? "month",
                    PrimerDiaSemana = Convert.ToInt32(reader["PrimerDiaSemana"]),
                    Idioma = reader["Idioma"]?.ToString() ?? "es",
                    FormatoHora = reader["FormatoHora"]?.ToString() ?? "12"
                };
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════
        //  SESIONES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra una nueva sesión de login.
        /// </summary>
        public void RegistrarSesion(int idUsuario, string token, string? ip, string? dispositivo)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"INSERT INTO SesionesUsuario (IDUsuario, TokenSesion, DireccionIP, Dispositivo)
                  VALUES (@IDUsuario, @Token, @IP, @Dispositivo)",
                connection);

            command.Parameters.AddWithValue("@IDUsuario", idUsuario);
            command.Parameters.AddWithValue("@Token", token);
            command.Parameters.AddWithValue("@IP", (object?)ip ?? DBNull.Value);
            command.Parameters.AddWithValue("@Dispositivo", (object?)dispositivo ?? DBNull.Value);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Obtiene todas las sesiones activas de un usuario.
        /// </summary>
        public List<SesionUsuario> ObtenerSesiones(int idUsuario)
        {
            var lista = new List<SesionUsuario>();

            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "SELECT IDSesion, IDUsuario, TokenSesion, FechaInicio, DireccionIP, Dispositivo FROM SesionesUsuario WHERE IDUsuario = @Id ORDER BY FechaInicio DESC",
                connection);
            command.Parameters.AddWithValue("@Id", idUsuario);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new SesionUsuario
                {
                    IDSesion = Convert.ToInt32(reader["IDSesion"]),
                    IDUsuario = Convert.ToInt32(reader["IDUsuario"]),
                    TokenSesion = reader["TokenSesion"]?.ToString() ?? "",
                    FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                    DireccionIP = reader["DireccionIP"] != DBNull.Value ? reader["DireccionIP"]?.ToString() : null,
                    Dispositivo = reader["Dispositivo"] != DBNull.Value ? reader["Dispositivo"]?.ToString() : null
                });
            }

            return lista;
        }

        /// <summary>
        /// Elimina todas las sesiones de un usuario.
        /// </summary>
        public bool EliminarSesiones(int idUsuario)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "DELETE FROM SesionesUsuario WHERE IDUsuario = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", idUsuario);

            return command.ExecuteNonQuery() > 0;
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

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
                PushToken = HasColumn(reader, "PushToken") && reader["PushToken"] != DBNull.Value ? reader["PushToken"]?.ToString() : null,
                Telefono = HasColumn(reader, "Telefono") && reader["Telefono"] != DBNull.Value ? reader["Telefono"]?.ToString() : null,
                FechaNacimiento = HasColumn(reader, "FechaNacimiento") && reader["FechaNacimiento"] != DBNull.Value ? Convert.ToDateTime(reader["FechaNacimiento"]) : null,
                Domicilio = HasColumn(reader, "Domicilio") && reader["Domicilio"] != DBNull.Value ? reader["Domicilio"]?.ToString() : null,
                AvatarBase64 = HasColumn(reader, "AvatarBase64") && reader["AvatarBase64"] != DBNull.Value ? reader["AvatarBase64"]?.ToString() : null
            };
            return u;
        }

        /// <summary>
        /// Comprueba si el reader tiene una columna con el nombre indicado.
        /// </summary>
        private bool HasColumn(MySqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
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
