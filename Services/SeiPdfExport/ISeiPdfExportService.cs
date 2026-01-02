namespace SeiPDFManagement.Services.SeiPdfExport
{
    /// <summary>
    /// Servizio applicativo responsabile dell'esportazione dei PDF SEIPDF
    /// da Oracle (BLOB) verso filesystem.
    ///
    /// Questo servizio incapsula la logica che nel sistema legacy
    /// era implementata nel canale Mirth SEIPDF_CREAPDF.
    /// </summary>
    public interface ISeiPdfExportService
    {
        /// <summary>
        /// Avvia il processo di esportazione dei PDF.
        ///
        /// Il metodo:
        /// - legge i record disponibili dalla VIEW_H2H_SEI_PDF
        /// - salva i file PDF su filesystem
        /// - aggiorna gli stati applicativi su Oracle
        /// - registra i log di processo
        ///
        /// Il numero di record effettivamente processati dipende
        /// dalla logica di selezione implementata nella view.
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>
        /// Oggetto di riepilogo contenente l'esito dell'operazione.
        /// </returns>
        Task<SeiPdfExportResult> ExportAsync(CancellationToken ct);
    }

    /// <summary>
    /// Oggetto di riepilogo dell'operazione di esportazione PDF.
    ///
    /// Viene restituito dal servizio applicativo al controller
    /// per fornire un feedback strutturato sull'esecuzione.
    /// </summary>
    public sealed class SeiPdfExportResult
    {
        /// <summary>
        /// Numero totale di record letti dalla VIEW_H2H_SEI_PDF.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Numero di PDF esportati correttamente su filesystem.
        /// </summary>
        public int Exported { get; set; }

        /// <summary>
        /// Numero di record per i quali il BLOB PDF era nullo.
        ///
        /// In questo caso il record viene marcato come errore
        /// (STATO = 92), replicando il comportamento legacy.
        /// </summary>
        public int NullBlob { get; set; }

        /// <summary>
        /// Numero di errori verificatisi durante il processo
        /// (esclusi i casi di BLOB nullo).
        /// </summary>
        public int Errors { get; set; }

        /// <summary>
        /// Elenco dei percorsi dei file PDF creati su filesystem.
        /// </summary>
        public List<string> Files { get; set; } = [];
    }
}
