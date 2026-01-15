using Microsoft.AspNetCore.Mvc;
using SeiPDFManagement.Services.SeiPdfZip;

namespace SeiPDFManagement.Controllers
{
    [ApiController]
    [Route("api/seipdf")]
    public class SeiPdfZipController(
            ISeiPdfZipService service,
            ILogger<SeiPdfZipController> logger) : ControllerBase
    {
        private readonly ISeiPdfZipService _service = service;
        private readonly ILogger<SeiPdfZipController> _logger = logger;

        /// <summary>
        /// Crea file ZIP e .t a partire dai PDF presenti in input directory,
        /// replicando la logica del canale Mirth SEIPDF_CREAZIP.
        /// </summary>
        /// <remarks>
        /// Regole applicate:
        /// - Raggruppamento PDF per prefisso FR_/FF_ e categoria Cx
        /// - Max 15 MB per ZIP
        /// - Max 250 PDF per ZIP
        /// - Update DB per ogni PDF zippato
        /// - Creazione file .t
        /// - Cancellazione dei PDF zippati
        /// </remarks>
        [HttpPost("create-zip")]
        [ProducesResponseType(typeof(SeiPdfZipResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateZip(CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Richiesta creazione ZIP ricevuta");
                var res = await _service.CreateZipsAsync(ct);
                return Ok(res);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore creazione ZIP");
                return Problem(detail: ex.Message, statusCode: 500);
            }
        }
    }
}
