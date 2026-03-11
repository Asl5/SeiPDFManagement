using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using SeiPDFManagement.Models;
using System.Data;

namespace SeiPDFManagement.Controllers
{
    /// <summary>
    /// Endpoint di health check dell'applicazione SEIPDF.
    /// Utilizzato per monitoraggio, schedulazioni e verifiche operative.
    /// </summary>
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IConfiguration config,
            ILogger<HealthController> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Verifica lo stato di salute dell'applicazione.
        /// </summary>
        /// <remarks>
        /// Il controllo include:
        /// - Connettività al database Oracle
        /// - Accessibilità e scrittura delle directory configurate
        /// - Stato complessivo del servizio
        ///
        /// Questo endpoint NON richiede autenticazione ed è pensato
        /// per essere usato da scheduler, monitor e operatori.
        /// </remarks>
        /// <param name="ct">
        /// Token di cancellazione per interrompere l'operazione in caso di shutdown.
        /// </param>
        /// <returns>
        /// Oggetto JSON con lo stato dettagliato del servizio.
        /// </returns>
        /// <response code="200">Servizio operativo e tutti i controlli superati</response>
        /// <response code="503">Uno o più controlli non superati</response>
        [HttpGet]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var result = new HealthResponse
            {
                TimestampUtc = DateTime.UtcNow,
                Application = "SeiPDFManagement"
            };

            // -----------------------------
            // ORACLE CHECK
            // -----------------------------
            try
            {
                var cs = _config.GetConnectionString("OracleConnectionString");

                await using var conn = new OracleConnection(cs);
                await conn.OpenAsync(ct);

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM dual";
                cmd.CommandType = CommandType.Text;

                await cmd.ExecuteScalarAsync(ct);

                result.Oracle = "OK";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HealthCheck Oracle FAILED");
                result.Oracle = "ERROR";
                result.Errors.Add("Oracle connection failed");
            }

            // -----------------------------
            // FILESYSTEM CHECK
            // -----------------------------
            CheckDirectory(result, "ExportOutput",
                _config["SeiPdfExport:OutputDirectory"]);

            CheckDirectory(result, "ZipInput",
                _config["SeiPdfZipSettings:InputDirectory"]);

            CheckDirectory(result, "Logs",
                _config["LogDirectory"]);

            // -----------------------------
            // SERVICE STATUS (badge UI)
            // -----------------------------

            result.PdfService =
                result.Oracle == "OK" &&
                result.Filesystem.GetValueOrDefault("ExportOutput") == "OK"
                    ? "OK"
                    : "ERROR";

            result.ZipService =
                result.Filesystem.GetValueOrDefault("ZipInput") == "OK"
                    ? "OK"
                    : "ERROR";

            // Per ora MAIL = OK se app è viva
            // In futuro potrai legarlo a IMAP / Graph
            result.MailService = "OK";


            // -----------------------------
            // FINAL STATUS
            // -----------------------------
            result.Status = result.Errors.Count == 0
                ? "Healthy"
                : "Unhealthy";

            return result.Status == "Healthy"
                ? Ok(result)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }

        /// <summary>
        /// Verifica l'esistenza e la scrivibilità di una directory.
        /// </summary>
        private void CheckDirectory(
            HealthResponse result,
            string name,
            string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    result.Filesystem[name] = "NOT_CONFIGURED";
                    result.Errors.Add($"{name} directory not configured");
                    return;
                }

                Directory.CreateDirectory(path);

                var testFile = Path.Combine(path, ".healthcheck");
                System.IO.File.WriteAllText(testFile, "ok");
                System.IO.File.Delete(testFile);

                result.Filesystem[name] = "OK";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HealthCheck filesystem FAILED for {Dir}", name);
                result.Filesystem[name] = "ERROR";
                result.Errors.Add($"Filesystem error on {name}");
            }
        }
    }
}
