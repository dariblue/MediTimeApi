namespace MediTimeApi.Models
{
    public class PreferenciasUsuario
    {
        public int IDPreferencia { get; set; }
        public int IDUsuario { get; set; }
        public string Tema { get; set; } = "light";
        public string TamanoTexto { get; set; } = "medium";
        public string VistaCalendario { get; set; } = "month";
        public int PrimerDiaSemana { get; set; } = 0;
        public string Idioma { get; set; } = "es";
        public string FormatoHora { get; set; } = "12";
    }
}
