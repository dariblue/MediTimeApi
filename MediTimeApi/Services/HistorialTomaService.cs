using MySql.Data.MySqlClient;
using MediTimeApi.Models;

namespace MediTimeApi.Services
{
    public class HistorialTomaService
    {
        private readonly Database _database;

        // Estados válidos según el ENUM de la BD
        private static readonly HashSet<string> EstadosValidos = new() { "Tomado", "Pasado" };

        public HistorialTomaService(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Registra una toma en el historial.
        /// Valida que Estado sea 'Tomado' o 'Pasado'.
        /// </summary>
        public bool RegistrarToma(HistorialToma toma)
        {
            if (!EstadosValidos.Contains(toma.Estado))
            {
                throw new ArgumentException(
                    $"Estado inválido: '{toma.Estado}'. Los valores permitidos son: 'Tomado', 'Pasado'.");
            }

            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"INSERT INTO HISTORIAL_TOMAS (IDMedicamento, IDUsuario_Accion, FechaHoraToma, Estado)
                  VALUES (@IDMedicamento, @IDUsuarioAccion, @FechaHoraToma, @Estado)",
                connection);

            command.Parameters.AddWithValue("@IDMedicamento", toma.IDMedicamento);
            command.Parameters.AddWithValue("@IDUsuarioAccion", toma.IDUsuarioAccion);
            command.Parameters.AddWithValue("@FechaHoraToma", toma.FechaHoraToma);
            command.Parameters.AddWithValue("@Estado", toma.Estado);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Obtiene el historial de tomas de un medicamento específico.
        /// </summary>
        public List<HistorialToma> GetHistorialPorMedicamento(int idMedicamento)
        {
            var lista = new List<HistorialToma>();

            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"SELECT IDToma, IDMedicamento, IDUsuario_Accion, FechaHoraToma, Estado
                  FROM HISTORIAL_TOMAS
                  WHERE IDMedicamento = @IDMedicamento
                  ORDER BY FechaHoraToma DESC",
                connection);
            command.Parameters.AddWithValue("@IDMedicamento", idMedicamento);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapHistorialToma(reader));
            }

            return lista;
        }

        /// <summary>
        /// Obtiene el historial de tomas de todos los medicamentos de un paciente.
        /// JOIN con MEDICAMENTOS sobre IDUsuario_Paciente.
        /// </summary>
        public List<HistorialToma> GetHistorialPorPaciente(int idPaciente)
        {
            var lista = new List<HistorialToma>();

            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"SELECT ht.IDToma, ht.IDMedicamento, ht.IDUsuario_Accion, ht.FechaHoraToma, ht.Estado
                  FROM HISTORIAL_TOMAS ht
                  INNER JOIN MEDICAMENTOS m ON ht.IDMedicamento = m.IDMedicamento
                  WHERE m.IDUsuario_Paciente = @IDPaciente
                  ORDER BY ht.FechaHoraToma DESC",
                connection);
            command.Parameters.AddWithValue("@IDPaciente", idPaciente);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapHistorialToma(reader));
            }

            return lista;
        }

        private HistorialToma MapHistorialToma(MySqlDataReader reader)
        {
            return new HistorialToma
            {
                IDToma = Convert.ToInt32(reader["IDToma"]),
                IDMedicamento = Convert.ToInt32(reader["IDMedicamento"]),
                IDUsuarioAccion = Convert.ToInt32(reader["IDUsuario_Accion"]),
                FechaHoraToma = Convert.ToDateTime(reader["FechaHoraToma"]),
                Estado = reader["Estado"]?.ToString() ?? ""
            };
        }
    }
}
