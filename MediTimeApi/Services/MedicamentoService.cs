using MySql.Data.MySqlClient;
using MediTimeApi.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;

namespace MediTimeApi.Services
{
    public class MedicamentoService
    {
        private readonly string _connectionString;

        public MedicamentoService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public List<Medicamento> GetMedicamentosByUsuario(int idUsuario)
        {
            var lista = new List<Medicamento>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Medicamentos WHERE IDUsuario = @IdUsuario";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var m = new Medicamento();
                            m.IdMedicamentos = reader.GetInt32("IDMedicamentos");
                            m.IdUsuario = reader.GetInt32("IDUsuario");
                            m.Nombre = reader["Nombre"].ToString();
                            // Corregido: TipoMedicamento
                            m.TipoMedicamento = reader["TipoMedicamento"].ToString(); 
                            m.Dosis = reader["Dosis"].ToString();
                            
                            // TIME se lee como TimeSpan
                            var timeSpan = reader.GetTimeSpan("HoraToma");
                            // Lo convertimos a string "HH:mm"
                            m.HoraToma = timeSpan.ToString(@"hh\:mm");
                            
                            m.Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString() : "";
                            
                            if (reader["FechaInicio"] != DBNull.Value)
                                m.FechaInicio = reader.GetDateTime("FechaInicio");
                                
                            if (reader["FechaFin"] != DBNull.Value)
                                m.FechaFin = reader.GetDateTime("FechaFin");

                            // Fechas auditoria
                            if (reader["FechaCreacion"] != DBNull.Value)
                                m.FechaCreacion = reader.GetDateTime("FechaCreacion");

                             // Corregido: FechaModificacion
                            if (reader["FechaModificacion"] != DBNull.Value)
                                m.FechaModificacion = reader.GetDateTime("FechaModificacion");
                            
                            lista.Add(m);
                        }
                    }
                }
            }
            return lista;
        }

        public bool CreateMedicamento(Medicamento med)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                // Corregido: TipoMedicamento, FechaModificacion
                var query = @"INSERT INTO Medicamentos 
                              (IDUsuario, Nombre, TipoMedicamento, Dosis, HoraToma, Notas, FechaInicio, FechaFin, FechaCreacion, FechaModificacion) 
                              VALUES 
                              (@IdUsuario, @Nombre, @Tipo, @Dosis, @Hora, @Notas, @Inicio, @Fin, @Creado, @Modificado)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUsuario", med.IdUsuario);
                    command.Parameters.AddWithValue("@Nombre", med.Nombre);
                    command.Parameters.AddWithValue("@Tipo", med.TipoMedicamento);
                    command.Parameters.AddWithValue("@Dosis", med.Dosis);
                    
                    // Convertir string "HH:mm" a TimeSpan para la BD
                    TimeSpan ts;
                    if (TimeSpan.TryParse(med.HoraToma, out ts))
                    {
                        command.Parameters.AddWithValue("@Hora", ts);
                    }
                    else
                    {
                        // Fallback predeterminado o error
                        command.Parameters.AddWithValue("@Hora", TimeSpan.Zero);
                    }

                    command.Parameters.AddWithValue("@Notas", med.Notas ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Inicio", med.FechaInicio);
                    command.Parameters.AddWithValue("@Fin", med.FechaFin ?? (object)DBNull.Value);
                    
                    // Fechas actuales
                    var now = DateTime.Now;
                    command.Parameters.AddWithValue("@Creado", now);
                    command.Parameters.AddWithValue("@Modificado", now);

                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool UpdateMedicamento(int id, Medicamento med)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                var query = @"UPDATE Medicamentos 
                              SET 
                                Nombre = @Nombre, 
                                TipoMedicamento = @Tipo, 
                                Dosis = @Dosis, 
                                HoraToma = @Hora, 
                                Notas = @Notas, 
                                FechaInicio = @Inicio, 
                                FechaFin = @Fin, 
                                FechaModificacion = @Modificado
                              WHERE IDMedicamentos = @Id AND IDUsuario = @IdUsuario";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@IdUsuario", med.IdUsuario);
                    command.Parameters.AddWithValue("@Nombre", med.Nombre);
                    command.Parameters.AddWithValue("@Tipo", med.TipoMedicamento);
                    command.Parameters.AddWithValue("@Dosis", med.Dosis);
                    
                    TimeSpan ts;
                    if (TimeSpan.TryParse(med.HoraToma, out ts))
                    {
                        command.Parameters.AddWithValue("@Hora", ts);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@Hora", TimeSpan.Zero);
                    }

                    command.Parameters.AddWithValue("@Notas", med.Notas ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Inicio", med.FechaInicio);
                    command.Parameters.AddWithValue("@Fin", med.FechaFin ?? (object)DBNull.Value);
                    
                    command.Parameters.AddWithValue("@Modificado", DateTime.Now);

                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
