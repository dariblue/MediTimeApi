using MySql.Data.MySqlClient;
using MediTimeApi.Models;

namespace MediTimeApi.Services
{
    public class PacienteCuidadorService
    {
        private readonly Database _database;

        public PacienteCuidadorService(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Crea un vínculo paciente↔cuidador.
        /// </summary>
        public bool CrearVinculo(int idPaciente, int idCuidador)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "INSERT INTO PACIENTE_CUIDADOR (IDPaciente, IDCuidador) VALUES (@IDPaciente, @IDCuidador)",
                connection);
            command.Parameters.AddWithValue("@IDPaciente", idPaciente);
            command.Parameters.AddWithValue("@IDCuidador", idCuidador);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Elimina un vínculo paciente↔cuidador.
        /// DELETE simple — MariaDB maneja ON DELETE CASCADE si se borra un usuario.
        /// </summary>
        public bool EliminarVinculo(int idPaciente, int idCuidador)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "DELETE FROM PACIENTE_CUIDADOR WHERE IDPaciente = @IDPaciente AND IDCuidador = @IDCuidador",
                connection);
            command.Parameters.AddWithValue("@IDPaciente", idPaciente);
            command.Parameters.AddWithValue("@IDCuidador", idCuidador);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Obtiene los cuidadores/responsables vinculados a un paciente.
        /// </summary>
        public List<Usuario> GetCuidadoresDePaciente(int idPaciente)
        {
            var lista = new List<Usuario>();

            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"SELECT u.IDUsuario, u.Nombre, u.Apellidos, u.Email, u.Rol, u.EsResponsable, u.PushToken
                  FROM PACIENTE_CUIDADOR pc
                  INNER JOIN USUARIOS u ON pc.IDCuidador = u.IDUsuario
                  WHERE pc.IDPaciente = @IDPaciente",
                connection);
            command.Parameters.AddWithValue("@IDPaciente", idPaciente);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapUsuarioFromReader(reader));
            }

            return lista;
        }

        /// <summary>
        /// Obtiene los pacientes vinculados a un cuidador/responsable.
        /// </summary>
        public List<Usuario> GetPacientesDeCuidador(int idCuidador)
        {
            var lista = new List<Usuario>();

            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"SELECT u.IDUsuario, u.Nombre, u.Apellidos, u.Email, u.Rol, u.EsResponsable, u.PushToken
                  FROM PACIENTE_CUIDADOR pc
                  INNER JOIN USUARIOS u ON pc.IDPaciente = u.IDUsuario
                  WHERE pc.IDCuidador = @IDCuidador",
                connection);
            command.Parameters.AddWithValue("@IDCuidador", idCuidador);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapUsuarioFromReader(reader));
            }

            return lista;
        }

        private Usuario MapUsuarioFromReader(MySqlDataReader reader)
        {
            return new Usuario
            {
                IDUsuario = Convert.ToInt32(reader["IDUsuario"]),
                Nombre = reader["Nombre"]?.ToString() ?? "",
                Apellidos = reader["Apellidos"]?.ToString() ?? "",
                Email = reader["Email"]?.ToString() ?? "",
                Contrasena = "", // Nunca exponer
                Rol = reader["Rol"]?.ToString() ?? "",
                EsResponsable = Convert.ToBoolean(reader["EsResponsable"]),
                PushToken = reader["PushToken"] != DBNull.Value ? reader["PushToken"]?.ToString() : null
            };
        }
    }
}
