namespace SeiPDFManagement.Models
{
    /// <summary>
    /// Risposta dell'endpoint di health check.
    /// Contiene lo stato dettagliato dei componenti principali del servizio.
    /// </summary>
    public sealed class HealthResponse
    {
        /// <summary>
        /// Stato complessivo del servizio.
        /// Valori tipici: Healthy / Unhealthy.
        /// </summary>
        public string Status { get; set; } = "Unknown";

        /// <summary>
        /// Nome dell'applicazione che espone l'endpoint.
        /// </summary>
        public string Application { get; set; } = string.Empty;

        /// <summary>
        /// Data e ora UTC in cui è stato eseguito il controllo.
        /// </summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// Stato della connessione al database Oracle.
        /// </summary>
        public string Oracle { get; set; } = "UNKNOWN";

        /// <summary>
        /// Stato delle directory filesystem utilizzate dal servizio.
        /// La chiave rappresenta la funzione della directory.
        /// </summary>
        public Dictionary<string, string> Filesystem { get; set; } = new();

        /// <summary>
        /// Elenco degli errori riscontrati durante il controllo.
        /// Vuoto se il servizio è Healthy.
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }
}
