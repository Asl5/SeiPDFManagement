namespace SeiPDFManagement.Models
{
    /// <summary>
    /// Risultato dell'elaborazione delle email non lette.
    /// </summary>
    public class DownloadUnreadResponse
    {
        /// <summary>
        /// Indica se l'operazione è andata a buon fine.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Numero totale di file elaborati.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Elenco dei percorsi dei file salvati.
        /// </summary>
        public IEnumerable<string> Files { get; set; } = [];
    }

}
