using MySql.Data.MySqlClient;

namespace MediTimeApi
{
    /// <summary>
    /// Servicio centralizado para la creación de conexiones a MariaDB.
    /// Se registra como Scoped en el contenedor DI.
    /// </summary>
    public class Database
    {
        private readonly string _connectionString;

        public Database(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection' en appsettings.json.");
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}
