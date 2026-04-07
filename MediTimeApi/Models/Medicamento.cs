namespace MediTimeApi.Models
{
    public class Medicamento
    {
        public int IDMedicamento { get; set; }
        public int IDUsuarioPaciente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Dosis { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public int FrecuenciaHoras { get; set; }
        public DateTime? FechaFin { get; set; }
        public int StockActual { get; set; }
        public int UmbralAlerta { get; set; }
        public bool Activo { get; set; } = true;
    }
}
