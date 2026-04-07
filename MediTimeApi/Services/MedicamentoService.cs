using MySql.Data.MySqlClient;
using MediTimeApi.Models;

namespace MediTimeApi.Services
{
    public class MedicamentoService
    {
        private readonly Database _database;

        public MedicamentoService(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Obtiene todos los medicamentos de un paciente.
        /// </summary>
        public List<Medicamento> GetMedicamentosByPaciente(int idPaciente)
        {
            var lista = new List<Medicamento>();

            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"SELECT IDMedicamento, IDUsuario_Paciente, Nombre, Dosis, FechaInicio,
                         FrecuenciaHoras, FechaFin, StockActual, UmbralAlerta, Activo
                  FROM MEDICAMENTOS
                  WHERE IDUsuario_Paciente = @IdPaciente",
                connection);
            command.Parameters.AddWithValue("@IdPaciente", idPaciente);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapMedicamento(reader));
            }

            return lista;
        }

        /// <summary>
        /// Obtiene un medicamento por su ID.
        /// </summary>
        public Medicamento? GetMedicamentoById(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"SELECT IDMedicamento, IDUsuario_Paciente, Nombre, Dosis, FechaInicio,
                         FrecuenciaHoras, FechaFin, StockActual, UmbralAlerta, Activo
                  FROM MEDICAMENTOS
                  WHERE IDMedicamento = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapMedicamento(reader);
            }
            return null;
        }

        /// <summary>
        /// Crea un nuevo medicamento asociado a un paciente.
        /// </summary>
        public bool CreateMedicamento(Medicamento med)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"INSERT INTO MEDICAMENTOS
                  (IDUsuario_Paciente, Nombre, Dosis, FechaInicio, FrecuenciaHoras, FechaFin, StockActual, UmbralAlerta, Activo)
                  VALUES
                  (@IdPaciente, @Nombre, @Dosis, @FechaInicio, @FrecuenciaHoras, @FechaFin, @StockActual, @UmbralAlerta, @Activo)",
                connection);

            command.Parameters.AddWithValue("@IdPaciente", med.IDUsuarioPaciente);
            command.Parameters.AddWithValue("@Nombre", med.Nombre);
            command.Parameters.AddWithValue("@Dosis", med.Dosis);
            command.Parameters.AddWithValue("@FechaInicio", med.FechaInicio);
            command.Parameters.AddWithValue("@FrecuenciaHoras", med.FrecuenciaHoras);
            command.Parameters.AddWithValue("@FechaFin", (object?)med.FechaFin ?? DBNull.Value);
            command.Parameters.AddWithValue("@StockActual", med.StockActual);
            command.Parameters.AddWithValue("@UmbralAlerta", med.UmbralAlerta);
            command.Parameters.AddWithValue("@Activo", med.Activo);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Actualiza un medicamento existente.
        /// </summary>
        public bool UpdateMedicamento(int id, Medicamento med)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                @"UPDATE MEDICAMENTOS SET
                    Nombre = @Nombre,
                    Dosis = @Dosis,
                    FechaInicio = @FechaInicio,
                    FrecuenciaHoras = @FrecuenciaHoras,
                    FechaFin = @FechaFin,
                    StockActual = @StockActual,
                    UmbralAlerta = @UmbralAlerta,
                    Activo = @Activo
                  WHERE IDMedicamento = @Id AND IDUsuario_Paciente = @IdPaciente",
                connection);

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IdPaciente", med.IDUsuarioPaciente);
            command.Parameters.AddWithValue("@Nombre", med.Nombre);
            command.Parameters.AddWithValue("@Dosis", med.Dosis);
            command.Parameters.AddWithValue("@FechaInicio", med.FechaInicio);
            command.Parameters.AddWithValue("@FrecuenciaHoras", med.FrecuenciaHoras);
            command.Parameters.AddWithValue("@FechaFin", (object?)med.FechaFin ?? DBNull.Value);
            command.Parameters.AddWithValue("@StockActual", med.StockActual);
            command.Parameters.AddWithValue("@UmbralAlerta", med.UmbralAlerta);
            command.Parameters.AddWithValue("@Activo", med.Activo);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Elimina un medicamento. La cascada en MariaDB limpia HISTORIAL_TOMAS.
        /// </summary>
        public bool DeleteMedicamento(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "DELETE FROM MEDICAMENTOS WHERE IDMedicamento = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", id);

            return command.ExecuteNonQuery() > 0;
        }

        private Medicamento MapMedicamento(MySqlDataReader reader)
        {
            return new Medicamento
            {
                IDMedicamento = Convert.ToInt32(reader["IDMedicamento"]),
                IDUsuarioPaciente = Convert.ToInt32(reader["IDUsuario_Paciente"]),
                Nombre = reader["Nombre"]?.ToString() ?? "",
                Dosis = reader["Dosis"]?.ToString() ?? "",
                FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                FrecuenciaHoras = Convert.ToInt32(reader["FrecuenciaHoras"]),
                FechaFin = reader["FechaFin"] != DBNull.Value ? Convert.ToDateTime(reader["FechaFin"]) : null,
                StockActual = Convert.ToInt32(reader["StockActual"]),
                UmbralAlerta = Convert.ToInt32(reader["UmbralAlerta"]),
                Activo = Convert.ToBoolean(reader["Activo"])
            };
        }
    }
}
