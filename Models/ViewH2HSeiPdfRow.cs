namespace SeiPDFManagement.Models
{
    public class ViewH2HSeiPdfRow
    {
        public string? FronteRetroDesc { get; set; }     // FRONTE_RETRO_DESC
        public string? Elaborazione { get; set; }        // ELABORAZIONE
        public string? Tipo { get; set; }                // TIPO
        public string? Servizio { get; set; }            // SERVIZIO
        public byte[]? FileComunicazione { get; set; }   // FILE_COMUNICAZIONE (BLOB PDF)
        public string? CentroDiCosto { get; set; }       // CENTRO_DI_COSTO
    }
}
