namespace MediTimeApi.Models
{
    public class HistorialToma
    {
        public int IDToma { get; set; }
        public int IDMedicamento { get; set; }
        public int IDUsuarioAccion { get; set; }
        public DateTime FechaHoraToma { get; set; }
        public string Estado { get; set; } = string.Empty; // 'Tomado' o 'Pasado'
    }
}
