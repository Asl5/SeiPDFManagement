using Microsoft.AspNetCore.Mvc;
using SeiPDFManagement.Services.SeiPdfExport;

namespace SeiPDFManagement.Controllers
{   
     /// <summary>
     /// API per l'esportazione dei PDF SEIPDF.
     ///
     /// Questo controller espone il flusso applicativo che replica
     /// il comportamento del canale Mirth SEIPDF_CREAPDF.
     /// </summary>
    [ApiController]
    [Route("api/seipdf")]
    public class SeiPdfExportController(ISeiPdfExportService service, ILogger<SeiPdfExportController> logger) : ControllerBase
    {
        private readonly ISeiPdfExportService _service = service;
        private readonly ILogger<SeiPdfExportController> _logger = logger;


        /// <summary>
        /// Avvia l'esportazione dei PDF da Oracle verso filesystem.
        /// </summary>
        /// <remarks>
        /// Replica il comportamento del canale Mirth <c>SEIPDF_CREAPDF</c>.
        ///
        /// Flusso operativo:
        /// 1) Lettura record dalla vista <c>VIEW_H2H_SEI_PDF</c>
        /// 2) Se <c>FILE_COMUNICAZIONE</c> (BLOB) è NULL:
        ///    - Aggiorna <c>H2H_WS_REQUEST.STATO</c> a <c>92</c>
        /// 3) Se il BLOB è presente:
        ///    - Salva il PDF su filesystem
        ///    - Aggiorna <c>STATO</c> a <c>50</c>
        ///    - Imposta i flag <c>SEIPDF</c> e <c>INVIATO</c>
        ///    - Inserisce i record nelle tabelle di log
        /// </remarks>
        /// <param name="ct">
        /// Token di cancellazione utilizzato per interrompere l'elaborazione.
        /// </param>
        /// <returns>
        /// Oggetto di riepilogo contenente:
        /// - numero totale di record letti
        /// - numero di PDF esportati
        /// - numero di BLOB nulli
        /// - numero di errori
        /// - elenco dei file creati
        /// </returns>
        [HttpPost("export-pdf")]
        [ProducesResponseType(typeof(SeiPdfExportResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportPdf(CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Richiesta export PDF ricevuta");
                var res = await _service.ExportAsync(ct);
                return Ok(res);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore export PDF");
                return Problem(detail: ex.Message, statusCode: 500);
            }
        }
    }
}
