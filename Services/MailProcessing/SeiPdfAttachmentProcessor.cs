using SeiPDFManagement.Controllers;
using SeiPDFManagement.Models;
using SeiPDFManagement.Repositories;
using SeiPDFManagement.Services.Context;
using System.Xml.Linq;

namespace SeiPDFManagement.Services.MailProcessing
{
    public class SeiPdfAttachmentProcessor(
        IEmailRepository repository,
        ILogger<SeiPdfAttachmentProcessor> logger,
        IRequestContext requestContext)
    {
        private readonly IEmailRepository _repository = repository;
        private readonly ILogger<SeiPdfAttachmentProcessor> _logger = logger;
        private readonly IRequestContext _context = requestContext;

        public async Task ProcessAsync(string xmlContent, string fileName)
        {
            if (fileName.EndsWith(".A00", StringComparison.OrdinalIgnoreCase))
                await ProcessA00Async(xmlContent);
            else if (fileName.EndsWith(".J00", StringComparison.OrdinalIgnoreCase))
                await ProcessJ00Async(xmlContent);
            else if (fileName.EndsWith(".S00", StringComparison.OrdinalIgnoreCase))
                await ProcessS00Async(xmlContent);
            else
                _logger.LogWarning("Estensione non gestita: {File}", fileName);
        }

        private async Task ProcessA00Async(string xmlContent)
        {
            _logger.LogInformation("Processo file A00");

            try
            {
                var xmlDoc = XDocument.Parse(xmlContent);

                string clientFileName = xmlDoc.Root?
                    .Attribute("ClientFileName")?
                    .Value?
                    .Trim() ?? "N/A";

                clientFileName = EstrarreParteNecessaria(clientFileName);

                var statusElement = (xmlDoc.Root?
                    .Element("Lot")?
                    .Element("Service")?
                    .Element("Print")?
                    .Element("Status")) ?? throw new Exception("Nodo Status mancante");
                int statusId = int.Parse(statusElement.Attribute("Id")!.Value);

                _logger.LogInformation("Lotto {Lotto} - Status {Status}", clientFileName, statusId);

                var records = await _repository.GetRecordsByLottoAsync(clientFileName);

                await _repository.InsertLogAsync(new LogEntry
                {
                    TipoLog = LogType.Info,
                    Servizio = _context.ControllerName ?? "UNKNOWN",
                    Messaggio = $"Elaboro lotto {clientFileName}, {records.Count()} files contenuti",
                    Funzione = nameof(ProcessA00Async),
                });

                foreach (var record in records)
                {
                    int elaborazione = int.Parse(ExtractBeforeFirstUnderscore(record));
                    await _repository.UpdateRecordStatusAsync(statusId, elaborazione);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante ProcessA00");
                throw;
            }
        }


        private async Task ProcessS00Async(string xmlContent)
        {
            _logger.LogInformation("Processo file S00");


            try
            {
                var xmlDoc = XDocument.Parse(xmlContent);

                string clientFileName = xmlDoc.Root?
                    .Attribute("ClientFileName")?
                    .Value?
                    .Trim() ?? "N/A";

                clientFileName = EstrarreParteNecessaria(clientFileName);

                var statusElement = xmlDoc.Descendants("Status").FirstOrDefault() ?? throw new Exception("Nodo Status mancante");
                int statusId = int.Parse(statusElement.Attribute("Id")!.Value);

                _logger.LogInformation("Lotto {Lotto} - Status {Status}", clientFileName, statusId);

                var records = await _repository.GetRecordsByLottoAsync(clientFileName);

                await _repository.InsertLogAsync(new LogEntry
                {
                    TipoLog = LogType.Info,
                    Servizio = _context.ControllerName ?? "UNKNOWN",
                    Messaggio = $"Elaboro lotto {clientFileName}, {records.Count()} files contenuti",
                    Funzione = nameof(ProcessS00Async),
                });

                foreach (var record in records)
                {
                    int elaborazione = int.Parse(ExtractBeforeFirstUnderscore(record));
                    await _repository.UpdateRecordStatusAsync(statusId, elaborazione);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante ProcessS00");
                throw;
            }
        }


        private async Task ProcessJ00Async(string xmlContent)
        {
            _logger.LogInformation("Processo file J00");

            try
            {
                var xmlDoc = XDocument.Parse(xmlContent);

                string clientFileName = xmlDoc
                    .Descendants("ClientFileName")
                    .FirstOrDefault()?
                    .Value?
                    .Trim() ?? "N/A";

                clientFileName = EstrarreParteNecessaria(clientFileName);

                int statusId = 10;

                _logger.LogInformation("Lotto {Lotto} - Status {Status}", clientFileName, statusId);

                var records = await _repository.GetRecordsByLottoAsync(clientFileName);

                await _repository.InsertLogAsync(new LogEntry
                {
                    TipoLog = LogType.Info,
                    Servizio = _context.ControllerName ?? "UNKNOWN",
                    Messaggio = $"Elaboro lotto {clientFileName}, {records.Count()} files contenuti",
                    Funzione = nameof(ProcessJ00Async),
                });


                foreach (var record in records)
                {
                    int elaborazione = int.Parse(ExtractBeforeFirstUnderscore(record));
                    await _repository.UpdateRecordStatusAsync(statusId, elaborazione);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante ProcessJ00");
                throw;
            }
        }


        private static string EstrarreParteNecessaria(string clientFileName)
        {
            if (string.IsNullOrEmpty(clientFileName))
                return "N/A";

            // 1. Rimuove la parte dopo l'ultimo underscore (per eliminare l'ultima parte numerica)
            int lastUnderscoreIndex = clientFileName.LastIndexOf('_');
            if (lastUnderscoreIndex != -1)
            {
                clientFileName = clientFileName.Substring(0, lastUnderscoreIndex);
            }

            // 2. Rimuove la prima parte prima del primo underscore
            int firstUnderscoreIndex = clientFileName.IndexOf('_');
            if (firstUnderscoreIndex != -1)
            {
                clientFileName = clientFileName.Substring(firstUnderscoreIndex + 1);
            }

            return clientFileName;
        }


        /// <summary>
        /// Estrae la parte prima del primo underscore "_" da una stringa.
        /// </summary>
        private static string ExtractBeforeFirstUnderscore(string input)
        {
            if (string.IsNullOrEmpty(input) || !input.Contains("_"))
                return input;

            // 1. Rimuove la parte prima del primo underscore
            int firstUnderscoreIndex = input.IndexOf('_');
            string trimmedInput = input.Substring(firstUnderscoreIndex + 1);

            firstUnderscoreIndex = trimmedInput.IndexOf('_');
            trimmedInput = trimmedInput.Substring(firstUnderscoreIndex + 1);

            // 2. Ora prende la parte prima del nuovo primo underscore
            int newFirstUnderscoreIndex = trimmedInput.IndexOf('_');
            if (newFirstUnderscoreIndex != -1)
            {
                return trimmedInput.Substring(0, newFirstUnderscoreIndex);
            }

            return trimmedInput; // Se non ci sono più underscore, restituisce il valore rimanente
        }


    }
}
