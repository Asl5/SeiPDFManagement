using Microsoft.Extensions.Options;
using SeiPDFManagement.Models;
using SeiPDFManagement.Repositories;

namespace SeiPDFManagement.Services.SeiPdfExport
{
    /// <summary>
    /// Implementazione del servizio di esportazione PDF SEIPDF.
    ///
    /// Obiettivo: replicare fedelmente il comportamento del canale Mirth SEIPDF_CREAPDF:
    /// - leggere i record dalla VIEW_H2H_SEI_PDF
    /// - estrarre il BLOB FILE_COMUNICAZIONE (PDF)
    /// - salvare il file su filesystem con naming storico
    /// - aggiornare stati e flag su H2H_WS_REQUEST
    /// - scrivere log su H2H_LOG_SEIPDF e H2H_SEIPDF_LOG
    /// </summary>
    public class SeiPdfExportService(
        IOptions<SeiPdfExportSettings> settings,
        ILogger<SeiPdfExportService> logger,
        ISeiPdfExportRepository repo) : ISeiPdfExportService
    {
        private readonly SeiPdfExportSettings _settings = settings.Value;
        private readonly ILogger<SeiPdfExportService> _logger = logger;
        private readonly ISeiPdfExportRepository _repo = repo;

        /// <summary>
        /// Esegue l'esportazione dei PDF disponibili nella VIEW_H2H_SEI_PDF.
        ///
        /// Il risultato restituisce un riepilogo numerico e l'elenco dei file creati.
        /// Il numero di record letti dipende dalla view e dal limite MaxRecords.
        /// </summary>
        public async Task<SeiPdfExportResult> ExportAsync(CancellationToken ct)
        {
            // Log di start identico al canale Mirth: utile per correlare i log storici.
            _logger.LogInformation("SEIPDF_CREAPDF : ----------- INIZIO SEIPDF_CREAPDF ----------");

            var result = new SeiPdfExportResult();

            // Assicura l'esistenza della directory di output
            Directory.CreateDirectory(_settings.OutputDirectory);

            // Lettura streaming dei record dalla view:
            // - consente di processare record uno a uno
            // - evita di caricare tutto in memoria
            await foreach (var row in _repo.ReadRowsAsync(_settings.MaxRecords, ct))
            {
                result.Total++;

                // Ricostruzione campi secondo mapping storico Mirth:
                // Nel JS:
                //   fronteretro = col1 (FRONTE_RETRO_DESC)
                //   elaborato   = col2 (ELABORAZIONE)
                //   servizio    = col3 (TIPO)       <- naming storico nel canale
                //   luogo       = col4 (SERVIZIO)   <- naming storico nel canale
                //   cc          = col6 (CENTRO_DI_COSTO)
                //
                // Nota:
                // Qui i nomi "servizioPart" e "luogoPart" sono scelti per evitare ambiguità,
                // ma il contenuto rimane coerente con il canale Mirth.
                var fronteretro = row.FronteRetroDesc ?? "";
                var elaborazione = row.Elaborazione ?? "";
                var servizioPart = row.Tipo ?? "";     // equivalente al "servizio" nel JS
                var luogoPart = row.Servizio ?? "";    // equivalente al "luogo" nel JS
                var cc = row.CentroDiCosto ?? "";

                // Naming storico del file:
                // \\pvesbasl5\e$\DaInviare\FRONTERETRO_CC+ELABORAZIONE_TIPO_SERVIZIO.pdf
                // NB: nel canale Mirth veniva usato anche un underscore tra fronteretro e cc.
                var nomeBase = $"{fronteretro}_{cc}{elaborazione}_{servizioPart}_{luogoPart}";
                var filePath = Path.Combine(_settings.OutputDirectory, nomeBase + ".pdf");

                try
                {
                    // Caso gestito esplicitamente anche nel legacy(in teoria non si dovrebbe mai verificare, nella view viene inserito il primo record con blob non nullo):
                    // se il BLOB è nullo, si marca STATO=92 e si prosegue con il record successivo.
                    if (row.FileComunicazione is null || row.FileComunicazione.Length == 0)
                    {
                        result.NullBlob++;

                        _logger.LogError(
                            "SEIPDF_CREAPDF : ---------- Errore: il BLOB del PDF è NULL per ELABORAZIONE={Elab} ----------",
                            elaborazione);

                        if (!string.IsNullOrWhiteSpace(elaborazione))
                            await _repo.SetStatoAsync(elaborazione, 92, ct);

                        // Non esporta file e non inserisce log file creato
                        continue;
                    }

                    // Salvataggio del PDF su filesystem.
                    // Equivalente del FileOutputStream del canale Mirth.
                    // In caso di errori (permessi share, percorso non raggiungibile, ecc.) si passa al catch.
                    await File.WriteAllBytesAsync(filePath, row.FileComunicazione, ct);

                    result.Exported++;
                    result.Files.Add(filePath);

                    _logger.LogInformation(
                        "SEIPDF_CREAPDF : ---------- Creato file PDF: {File} ----------",
                        filePath);

                    // Aggiornamenti su H2H_WS_REQUEST:
                    // - Mirth: UPDATE STATO=50
                    // - Mirth: UPDATE SEIPDF=1, INVIATO=1
                    //
                    // Questi update rendono tipicamente il record non più selezionabile dalla view.
                    if (!string.IsNullOrWhiteSpace(elaborazione))
                    {
                        await _repo.SetStatoAsync(elaborazione, 50, ct);
                        await _repo.SetSeiPdfInviatoAsync(elaborazione, ct);
                    }

                    // Logging su tabelle storiche:
                    // - H2H_LOG_SEIPDF: evento "File generato"
                    // - H2H_SEIPDF_LOG: traccia file e data creazione
                    //
                    // Il valore "nomeBase" è lo stesso naming usato nel canale Mirth
                    // (attenzione: qui senza estensione).
                    await _repo.InsertH2HLogSeiPdfAsync(nomeBase, "File generato", ".pdf", ct);
                    await _repo.InsertH2HSeiPdfLogAsync(nomeBase, ".pdf", 1, ct);
                }
                catch (Exception ex)
                {
                    // Qualsiasi errore non previsto (FS/DB/IO) viene contato
                    // e gestito come nel canale Mirth: STATO=92.
                    result.Errors++;

                    _logger.LogError(
                        ex,
                        "SEIPDF_CREAPDF : ---------- Errore durante l'elaborazione del PDF per ELABORAZIONE={Elab} ----------",
                        elaborazione);

                    // Nel legacy: nel catch set STATO=92
                    // Qui lo facciamo best-effort: anche se fallisce l'update non blocchiamo la pipeline.
                    if (!string.IsNullOrWhiteSpace(elaborazione))
                    {
                        try
                        {
                            await _repo.SetStatoAsync(elaborazione, 92, ct);
                        }
                        catch (Exception inner)
                        {
                            _logger.LogError(
                                inner,
                                "Errore durante update STATO=92 per ELABORAZIONE={Elab}",
                                elaborazione);
                        }
                    }
                }
            }

            // Log di fine
            _logger.LogInformation("SEIPDF_CREAPDF : ----------- FINE SEIPDF_CREAPDF ----------");

            return result;
        }
    }
}
