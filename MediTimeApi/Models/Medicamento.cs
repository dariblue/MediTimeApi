using System;

namespace MediTimeApi.Models
{
    public class Medicamento
    {
        public int IdMedicamentos { get; set; }
        public int IdUsuario { get; set; }
        // Se mapea a la columna 'Nombre'
        public string Nombre { get; set; }
        // Se mapea a la columna 'TipoMedicament' (OJO: sin 'o' al final según imagen)
        public string TipoMedicamento { get; set; }
        public string Dosis { get; set; }
        // Se recibe como string (ej "14:00") pero en BD es TIME
        public string HoraToma { get; set; }
        public string Notas { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        
        // Campos de auditoría
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
    }
}
