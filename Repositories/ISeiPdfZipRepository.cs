namespace SeiPDFManagement.Repositories
{
    /// <summary>
    /// Repository per le operazioni DB legate alla creazione ZIP SEIPDF.
    /// Replica fedelmente il comportamento del canale Mirth SEIPDF_CREAZIP.
    /// </summary>
    public interface ISeiPdfZipRepository
    {
        /// <summary>
        /// Ottiene il prossimo valore della sequence H2H_SEIPDF_ZIP
        /// formattato a 7 cifre con zero-padding.
        /// </summary>
        Task<string> GetNextZipSequenceAsync(CancellationToken ct);

        /// <summary>
        /// Marca un PDF come zippato nella tabella H2H_SEIPDF_LOG
        /// assegnando il lotto ZIP.
        /// </summary>
        Task MarkPdfAsZippedAsync(string zipName, string pdfName, CancellationToken ct);

        /// <summary>
        /// Inserisce un log applicativo nella tabella H2H_LOG_SEIPDF
        /// (usato per .zip e .t).
        /// </summary>
        Task InsertH2HLogSeiPdfAsync(string elaborazione, string messaggio, string tipoFile, CancellationToken ct);
    }
}
