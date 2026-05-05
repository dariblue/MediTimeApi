using System.Text.Json;
using MediTimeApi.Models;
using MediTimeApi.Services;
using MySql.Data.MySqlClient;

namespace MediTimeApi.Services
{
    public class AlertWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AlertWorker> _logger;

        public AlertWorker(IServiceProvider serviceProvider, ILogger<AlertWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[AlertWorker] Iniciado.");

            // Ciclo infinito mientras la aplicación esté corriendo
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                
                // Ejecutamos la validación en el minuto exacto
                if (now.Second == 0 || now.Second == 1 || now.Second == 2)
                {
                    try
                    {
                        await ProcessAlertsAsync(now);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[AlertWorker] Error procesando alertas.");
                    }
                    
                    // Esperamos hasta el próximo minuto para no duplicar alertas en el mismo minuto
                    await Task.Delay(TimeSpan.FromSeconds(60 - DateTime.Now.Second), stoppingToken);
                }
                else
                {
                    // Dormir 1 segundo antes de volver a chequear el reloj
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private async Task ProcessAlertsAsync(DateTime currentMinute)
        {
            // Remover segundos para la comparacion
            currentMinute = new DateTime(currentMinute.Year, currentMinute.Month, currentMinute.Day, currentMinute.Hour, currentMinute.Minute, 0);

            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();
            var webPushService = scope.ServiceProvider.GetRequiredService<WebPushService>();
            var pushService = scope.ServiceProvider.GetRequiredService<PushSubscriptionService>();

            using var connection = database.GetConnection();
            await connection.OpenAsync();

            var command = new MySqlCommand(
                @"SELECT m.IDMedicamento, m.IDUsuario_Paciente, m.Nombre, m.Dosis, m.FechaInicio, m.FrecuenciaHoras,
                         c.TiempoAnticipacion
                  FROM MEDICAMENTOS m
                  LEFT JOIN ConfiguracionNotificaciones c ON m.IDUsuario_Paciente = c.IDUsuario
                  WHERE m.Activo = 1", connection);

            var medsToNotify = new List<(Medicamento Med, int Anticipacion)>();

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var med = new Medicamento
                    {
                        IDMedicamento = Convert.ToInt32(reader["IDMedicamento"]),
                        IDUsuarioPaciente = Convert.ToInt32(reader["IDUsuario_Paciente"]),
                        Nombre = reader["Nombre"].ToString() ?? "",
                        Dosis = reader["Dosis"].ToString() ?? "",
                        FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                        FrecuenciaHoras = Convert.ToInt32(reader["FrecuenciaHoras"])
                    };
                    
                    int anticipacion = 5; // Por defecto 5 mins
                    if (!reader.IsDBNull(reader.GetOrdinal("TiempoAnticipacion")))
                    {
                        anticipacion = Convert.ToInt32(reader["TiempoAnticipacion"]);
                    }

                    medsToNotify.Add((med, anticipacion));
                }
            }

            foreach (var item in medsToNotify)
            {
                var med = item.Med;
                var anticipacion = item.Anticipacion;

                if (currentMinute >= med.FechaInicio || med.FechaInicio > currentMinute)
                {
                    // Si el inicio es en el futuro, no enviamos a menos que (Inicio - Anticipacion) sea igual a currentMinute
                    var targetTime = currentMinute.AddMinutes(anticipacion);
                    
                    if (targetTime < med.FechaInicio) continue;

                    var targetDiff = targetTime - med.FechaInicio;

                    // Si las horas de diferencia encajan EXACTAMENTE con la frecuencia en horas (resto en minutos == 0)
                    double diffTotalMinutes = targetDiff.TotalMinutes;
                    double frequenceMinutes = med.FrecuenciaHoras * 60;

                    if (diffTotalMinutes >= 0 && (diffTotalMinutes % frequenceMinutes) == 0)
                    {
                        _logger.LogInformation($"[AlertWorker] Notificación de {med.Nombre} para usuario {med.IDUsuarioPaciente} (Toma a las {targetTime:HH:mm})");

                        var suscripciones = pushService.ObtenerSuscripcionesPorUsuario(med.IDUsuarioPaciente);
                        
                        var payload = new
                        {
                            title = $"⏰ Hora de tomar: {med.Nombre}",
                            body = $"Te toca tu dosis de {med.Dosis} a las {targetTime:HH:mm}.",
                            tag = $"med-{med.IDMedicamento}-{targetTime:yyyyMMddHHmm}",
                            idMedicamento = med.IDMedicamento
                        };
                        
                        string jsonPayload = JsonSerializer.Serialize(payload);

                        foreach (var sub in suscripciones)
                        {
                            bool success = await webPushService.SendPushNotificationAsync(sub, jsonPayload);
                            if(!success) {
                                // Si da error 410, se puede eliminar la suscripcion aquí llamando al service
                                // _logger.LogWarning($"[AlertWorker] Fallo al enviar a {sub.Endpoint}");
                            }
                        }
                    }
                }
            }
        }
    }
}
