using Microsoft.Extensions.Options;
using SeiPDFManagement.Models;
using SeiPDFManagement.Repositories;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace SeiPDFManagement.Services.SeiPdfZip
{
    public sealed class SeiPdfZipService(
        IOptions<SeiPdfZipSettings> settings,
        ISeiPdfZipRepository repo,
        ILogger<SeiPdfZipService> logger
    ) : ISeiPdfZipService
    {
        private readonly SeiPdfZipSettings _settings = settings.Value;
        private readonly ISeiPdfZipRepository _repo = repo;
        private readonly ILogger<SeiPdfZipService> _logger = logger;

        private static readonly Regex PdfPattern =
            new(@"^(FR_|FF_)(C\d+)_", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public async Task<SeiPdfZipResult> CreateZipsAsync(CancellationToken ct)
        {
            _logger.LogInformation("SEIPDF_CREAZIP : ----- START -----");

            Directory.CreateDirectory(_settings.InputDirectory);

            var result = new SeiPdfZipResult();

            var allPdf = Directory
                .EnumerateFiles(_settings.InputDirectory, "*.pdf")
                .Select(f => new FileInfo(f))
                .ToList();

            result.TotalPdfFound = allPdf.Count;

            var groups = allPdf
                .Select(f => new { File = f, Match = PdfPattern.Match(f.Name) })
                .Where(x => x.Match.Success)
                .GroupBy(x => x.Match.Groups[1].Value + x.Match.Groups[2].Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.File).ToList());

            foreach (var (groupKey, pdfFiles) in groups)
            {
                // Continua a creare ZIP finché ci sono PDF da processare nel gruppo
                while (pdfFiles.Count != 0)
                {
                    ct.ThrowIfCancellationRequested();

                    // Ottiene una nuova sequence per il lotto ZIP
                    var sequence = await _repo.GetNextZipSequenceAsync(ct);

                    // Nome ZIP secondo lo standard Poste
                    var zipName = $"PM_{groupKey}_{sequence}.zip";
                    var zipPath = Path.Combine(_settings.InputDirectory, zipName);

                    var addedFiles = new List<FileInfo>();

                    // Crea fisicamente il file ZIP
                    using (ZipFile.Open(zipPath, ZipArchiveMode.Create)) { }

                    // Ordina i PDF dal più grande al più piccolo
                    foreach (var pdf in pdfFiles.OrderByDescending(f => f.Length).ToList())
                    {
                        // Limite massimo di file per ZIP (es. 250)
                        if (addedFiles.Count >= _settings.MaxFilesPerZip)
                            break;

                        // Aggiunge temporaneamente il file allo ZIP
                        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Update))
                        {
                            zip.CreateEntryFromFile(pdf.FullName, pdf.Name);
                        }

                        // Dimensione reale dello ZIP dopo l'aggiunta
                        var zipSize = new FileInfo(zipPath).Length;

                        // Se supera il limite configurato (es. 15 MB)
                        if (zipSize > _settings.MaxZipSizeMb * 1024L * 1024L)
                        {
                            // Rollback: rimuove l'ultimo file inserito
                            using var zipRollback = ZipFile.Open(zipPath, ZipArchiveMode.Update);
                            zipRollback.GetEntry(pdf.Name)?.Delete();

                            _logger.LogInformation(
                                "Raggiunto limite dimensione ZIP ({Size} bytes), stop inserimenti",
                                zipSize
                            );

                            break;
                        }

                        // Conferma l'inserimento
                        addedFiles.Add(pdf);
                    }

                    // Se nessun file è stato inserito (PDF singolo troppo grande)
                    if (addedFiles.Count == 0)
                    {
                        _logger.LogWarning(
                            "ZIP {Zip} vuoto: PDF troppo grande per i limiti configurati",
                            zipName
                        );

                        File.Delete(zipPath);
                        break;
                    }

                    // Creazione del file .t vuoto associato allo ZIP
                    // (replica la createTFile del canale Mirth)
                    File.Create(
                        Path.Combine(
                            _settings.InputDirectory,
                            zipName.Replace(".zip", ".t")
                        )
                    ).Dispose();

                    // Aggiornamento DB + cleanup filesystem
                    foreach (var pdf in addedFiles)
                    {
                        await _repo.MarkPdfAsZippedAsync(
                            zipName.Replace(".zip", ""),
                            pdf.Name,
                            ct
                        );

                        pdf.Delete();
                        pdfFiles.Remove(pdf);
                    }

                    // Log applicativo DB (ZIP creato)
                    await _repo.InsertH2HLogSeiPdfAsync(
                        zipName.Replace(".zip", ""),
                        "File generato",
                        "zip",
                        ct
                    );

                    // Aggiornamento risultato
                    result.ZipCreated++;
                    result.PdfZipped += addedFiles.Count;
                    result.ZipFiles.Add(zipPath);

                    // Se configurato: un solo ZIP per run
                    if (!_settings.CreateMultipleZipsPerRun)
                        break;
                }




            }

            _logger.LogInformation("SEIPDF_CREAZIP : ----- END -----");
            return result;
        }
    }
}
