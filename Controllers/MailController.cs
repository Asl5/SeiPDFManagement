
using Microsoft.AspNetCore.Mvc;
using SeiPDFManagement.Models;
using SeiPDFManagement.Services;
using SeiPDFManagement.Services.Context;

namespace SeiPDFManagement.Controllers
{
    /// <summary>
    /// Controller API che espone endpoint per leggere email da Office365 via IMAP OAuth2.
    /// Richiamabile anche da Postman o da applicazioni esterne.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MailController(IRequestContext reqContext,IMailService mailService, ILogger<MailController> logger, IConfiguration config) : ControllerBase
    {
        private readonly IMailService _mailService = mailService;
        private readonly ILogger<MailController> _logger = logger;
        private readonly IConfiguration _config = config;
        private readonly IRequestContext _context = reqContext;

        /// <summary>
        /// Scarica ed elabora le email non lette dalla casella configurata.
        /// </summary>
        /// <remarks>
        /// L'endpoint:
        /// - legge le email IMAP non lette
        /// - filtra per oggetti configurati
        /// - salva gli allegati su filesystem
        /// - elabora i file (.A00, .J00, .S00)
        ///
        /// Sicurezza:
        /// - opzionale API Key tramite header `X-API-KEY`
        /// </remarks>
        /// <param name="apiKey">
        /// Chiave API opzionale per l'accesso.
        /// Deve essere passata nell'header HTTP `X-API-KEY`.
        /// </param>
        /// <returns>
        /// Esito dell'operazione con elenco dei file elaborati.
        /// </returns>
        /// <response code="200">Operazione completata con successo</response>
        /// <response code="401">Chiave API mancante o non valida</response>
        /// <response code="500">Errore interno durante l'elaborazione</response>
        [HttpGet("download-unread")]
        [ProducesResponseType(typeof(DownloadUnreadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadUnreadAttachments([FromHeader(Name = "X-API-KEY")] string? apiKey = null)
        {
            try
            {
                _context.ControllerName = ControllerContext.ActionDescriptor.ControllerName;
                // --- (Facoltativo) Controllo semplice di sicurezza con chiave API ---
                var requiredKey = _config["Security:ApiKey"];
                if (!string.IsNullOrEmpty(requiredKey) && apiKey != requiredKey)
                {
                    _logger.LogWarning("Accesso non autorizzato: chiave API mancante o errata");
                    return Unauthorized(new { Error = "Chiave API non valida o mancante" });
                }

                _logger.LogInformation("Richiesta ricevuta per DownloadUnreadAttachments da {Ip}", HttpContext.Connection.RemoteIpAddress);

                // Esegue la lettura delle mail non lette e scarica gli allegati
                var files = await _mailService.DownloadUnreadAsync();

                _logger.LogInformation("Operazione completata, {Count} file scaricati", files.Count());

                var response = new DownloadUnreadResponse
                {
                    Success = true,
                    Count = files.Count(),
                    Files = files
                };
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione delle email");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    ex.StackTrace
                });
            }
        }
    }
}

