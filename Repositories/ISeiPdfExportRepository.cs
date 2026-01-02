using SeiPDFManagement.Models;

namespace SeiPDFManagement.Repositories
{
    /// <summary>
    /// Contratto di accesso ai dati Oracle per il flusso SEIPDF.
    ///
    /// Questa interfaccia rappresenta il confine applicativo
    /// tra il servizio C# e il database Oracle.
    ///
    /// Incapsula la logica che nel sistema legacy
    /// era implementata nel canale Mirth SEIPDF_CREAPDF.
    /// </summary>
    public interface ISeiPdfExportRepository
    {
        /// <summary>
        /// Legge i record dalla VIEW_H2H_SEI_PDF.
        ///
        /// La view:
        /// - restituisce solo i record pronti per l'esportazione
        /// - funge da coda applicativa lato database
        /// - può restituire uno o più record in base a maxRecords
        ///
        /// Il metodo restituisce uno stream asincrono
        /// per consentire l'elaborazione progressiva dei record.
        /// </summary>
        /// <param name="maxRecords">
        /// Numero massimo di record da leggere.
        /// Se maxRecords è minore o uguale a zero, non viene applicato alcun limite.
        /// </param>
        /// <param name="ct">
        /// CancellationToken per interrompere l'operazione.
        /// </param>
        IAsyncEnumerable<ViewH2HSeiPdfRow> ReadRowsAsync(
            int maxRecords,
            CancellationToken ct);

        /// <summary>
        /// Aggiorna il campo STATO della tabella H2H_WS_REQUEST
        /// per una specifica elaborazione.
        ///
        /// Il commit è implicito (nessuna transazione esplicita),
        /// in coerenza con il comportamento del canale Mirth.
        /// </summary>
        /// <param name="elaborazione">
        /// Identificativo della richiesta da aggiornare.
        /// </param>
        /// <param name="stato">
        /// Valore dello stato da impostare.
        /// </param>
        /// <param name="ct">
        /// CancellationToken.
        /// </param>
        Task SetStatoAsync(
            string elaborazione,
            int stato,
            CancellationToken ct);

        /// <summary>
        /// Imposta i flag SEIPDF e INVIATO a 1
        /// sulla tabella H2H_WS_REQUEST.
        ///
        /// Questo metodo indica che il PDF
        /// è stato correttamente esportato.
        /// </summary>
        /// <param name="elaborazione">
        /// Identificativo della richiesta.
        /// </param>
        /// <param name="ct">
        /// CancellationToken.
        /// </param>
        Task SetSeiPdfInviatoAsync(
            string elaborazione,
            CancellationToken ct);

        /// <summary>
        /// Inserisce un record nella tabella H2H_LOG_SEIPDF.
        ///
        /// Utilizzato per tracciare l'esito del processo
        /// di creazione/esportazione del file PDF.
        ///
        /// Eventuali errori di logging
        /// NON devono interrompere il flusso principale.
        /// </summary>
        /// <param name="elaborazioneFileName">
        /// Nome logico del file (come da naming SEIPDF).
        /// </param>
        /// <param name="messaggio">
        /// Messaggio descrittivo del log.
        /// </param>
        /// <param name="tipoFile">
        /// Tipo file (es. ".pdf").
        /// </param>
        /// <param name="ct">
        /// CancellationToken.
        /// </param>
        Task InsertH2HLogSeiPdfAsync(
            string elaborazioneFileName,
            string messaggio,
            string tipoFile,
            CancellationToken ct);

        /// <summary>
        /// Inserisce un record nella tabella H2H_SEIPDF_LOG.
        ///
        /// Registra l'avvenuta creazione del file PDF
        /// e la relativa data di creazione.
        /// </summary>
        /// <param name="nomeFile">
        /// Nome fisico del file PDF creato.
        /// </param>
        /// <param name="tipoFile">
        /// Tipo file (es. ".pdf").
        /// </param>
        /// <param name="creato">
        /// Flag di creazione (1 = creato).
        /// </param>
        /// <param name="ct">
        /// CancellationToken.
        /// </param>
        Task InsertH2HSeiPdfLogAsync(
            string nomeFile,
            string tipoFile,
            int creato,
            CancellationToken ct);
    }
}