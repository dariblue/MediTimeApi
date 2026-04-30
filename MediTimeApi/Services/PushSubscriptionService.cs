using MySql.Data.MySqlClient;
using MediTimeApi.Models;

namespace MediTimeApi.Services
{
    public class PushSubscriptionService
    {
        private readonly Database _database;

        public PushSubscriptionService(Database database)
        {
            _database = database;
        }

        public bool GuardarSuscripcion(PushSubscriptionRequest request)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            // Usar UPSERT (ON DUPLICATE KEY UPDATE) para actualizar si el Endpoint ya existe
            var command = new MySqlCommand(
                @"INSERT INTO PUSH_SUBSCRIPTIONS (IDUsuario, Endpoint, P256dh, Auth)
                  VALUES (@IDUsuario, @Endpoint, @P256dh, @Auth)
                  ON DUPLICATE KEY UPDATE 
                  IDUsuario = @IDUsuario, P256dh = @P256dh, Auth = @Auth, FechaCreacion = CURRENT_TIMESTAMP",
                connection);

            command.Parameters.AddWithValue("@IDUsuario", request.IdUsuario);
            command.Parameters.AddWithValue("@Endpoint", request.Endpoint);
            command.Parameters.AddWithValue("@P256dh", request.P256dh);
            command.Parameters.AddWithValue("@Auth", request.Auth);

            return command.ExecuteNonQuery() > 0;
        }

        public bool EliminarSuscripcion(string endpoint)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "DELETE FROM PUSH_SUBSCRIPTIONS WHERE Endpoint = @Endpoint",
                connection);

            command.Parameters.AddWithValue("@Endpoint", endpoint);
            return command.ExecuteNonQuery() > 0;
        }

        public List<PushSubscriptionModel> ObtenerSuscripcionesPorUsuario(int idUsuario)
        {
            var suscripciones = new List<PushSubscriptionModel>();

            using var connection = _database.GetConnection();
            connection.Open();

            var command = new MySqlCommand(
                "SELECT IDSubscription, IDUsuario, Endpoint, P256dh, Auth, FechaCreacion FROM PUSH_SUBSCRIPTIONS WHERE IDUsuario = @IDUsuario",
                connection);

            command.Parameters.AddWithValue("@IDUsuario", idUsuario);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                suscripciones.Add(new PushSubscriptionModel
                {
                    IDSubscription = reader.GetInt32("IDSubscription"),
                    IDUsuario = reader.GetInt32("IDUsuario"),
                    Endpoint = reader.GetString("Endpoint"),
                    P256dh = reader.GetString("P256dh"),
                    Auth = reader.GetString("Auth"),
                    FechaCreacion = reader.GetDateTime("FechaCreacion")
                });
            }

            return suscripciones;
        }
    }
}
