using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using MimeKit;
using SeiPDFManagement.Models;
using SeiPDFManagement.Repositories;
using SeiPDFManagement.Services.Context;
using SeiPDFManagement.Services.MailProcessing;

namespace SeiPDFManagement.Services;

/// <summary>
/// Servizio per leggere email IMAP da Office365 con OAuth2 e TLS 1.2+.
/// Scarica solo le email con oggetto specifico e salva eventuali allegati.
/// Registra log sia su file/console (ILogger) che su database Oracle.
/// </summary>
public class MailService(
    IRequestContext reqContext,
    IOptions<MailSettings> settings,
    ILogger<MailService> logger,
    SeiPdfAttachmentProcessor seiPdfAttachmentProcessor,
    IEmailRepository repository) : IMailService
{
    private readonly MailSettings _settings = settings.Value;
    private readonly ILogger<MailService> _logger = logger;
    private readonly IEmailRepository _repository = repository;
    private readonly SeiPdfAttachmentProcessor _attachmentProcessor = seiPdfAttachmentProcessor;
    private readonly IRequestContext _context = reqContext;

    public async Task<IEnumerable<string>> DownloadUnreadAsync()
    {
        var downloaded = new List<string>();
        string funzione = nameof(DownloadUnreadAsync);
        string servizio = _context.ControllerName ?? "UNKNOWN";

        try
        {
            _logger.LogInformation("Inizio elaborazione delle email non lette...");
            await LogSafeAsync(LogEntry.Info(servizio, funzione, "Inizio elaborazione delle email non lette"));

            var accessToken = await GetAccessTokenAsync();

            using var client = new ImapClient();
            await client.ConnectAsync(_settings.ImapServer, _settings.ImapPort, SecureSocketOptions.SslOnConnect);

            var oauth2 = new SaslMechanismOAuth2(_settings.UserEmail, accessToken);
            await client.AuthenticateAsync(oauth2);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var unseen = await inbox.SearchAsync(SearchQuery.NotSeen);
            _logger.LogInformation("Trovate {Count} email non lette", unseen.Count);
            await LogSafeAsync(LogEntry.Info(servizio, funzione, $"Trovate {unseen.Count} email non lette"));

            if (_settings.MaxEmailsToRead > 0 && unseen.Count > _settings.MaxEmailsToRead)
            {
                unseen = unseen.Take(_settings.MaxEmailsToRead).ToList();
                _logger.LogInformation("Lettura limitata alle prime {Limit} email", _settings.MaxEmailsToRead);
                //await LogSafeAsync(LogEntry.Info(servizio, funzione, $"Lettura limitata alle prime {_settings.MaxEmailsToRead} email"));
            }

            foreach (var uid in unseen)
            {
                var message = await inbox.GetMessageAsync(uid);
                var subject = message.Subject ?? "";
                _logger.LogInformation("Analizzo UID {Uid} - Oggetto: {Subject}", uid.Id, subject);

                if (SubjectDiInteresse(subject))
                {
                    _logger.LogInformation("Email UID {Uid} di interesse", uid.Id);
                    await LogSafeAsync(LogEntry.Info(servizio, funzione, $"Email UID {uid.Id} di interesse: {subject}"));

                    foreach (var attachment in message.Attachments)
                    {
                        if (attachment is MimePart part)
                        {
                            Directory.CreateDirectory(_settings.AttachmentDir);
                            var filePath = Path.Combine(_settings.AttachmentDir, part.FileName);

                            //await using var stream = File.Create(filePath);
                            await using (var stream = File.Create(filePath))
                            {
                                await part.Content.DecodeToAsync(stream);
                            }
                            downloaded.Add(filePath);

                            _logger.LogInformation("Salvato allegato {File}", filePath);

                            string xmlContent = await File.ReadAllTextAsync(filePath);

                            await _attachmentProcessor.ProcessAsync(xmlContent, part.FileName);

                            //await LogSafeAsync(LogEntry.Info(servizio, funzione, $"Salvato allegato {filePath}"));
                        }
                    }

                    await inbox.SetFlagsAsync(uid, MessageFlags.Seen, true);
                    _logger.LogInformation("Email UID {Uid} contrassegnata come letta", uid.Id);
                    await LogSafeAsync(LogEntry.Info(servizio, funzione, $"Email UID {uid.Id} contrassegnata come letta"));
                }
                else
                {
                    _logger.LogDebug("Email UID {Uid} ignorata", uid.Id);
                    await LogSafeAsync(LogEntry.Warn(servizio, funzione, $"Email UID {uid.Id} ignorata (oggetto non rilevante)"));
                }
            }

            await client.DisconnectAsync(true);
            _logger.LogInformation("Disconnessione completata dal server IMAP");
            await LogSafeAsync(LogEntry.Info(servizio, funzione, "Disconnessione completata dal server IMAP"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'elaborazione delle email");
            await LogSafeAsync(LogEntry.FromException(ex, _context.ControllerName ?? "UNKNOWN", nameof(DownloadUnreadAsync)));
        }

        return downloaded;
    }

    /// <summary>
    /// Scrive un log nel DB senza mai generare eccezioni.
    /// </summary>
    private async Task LogSafeAsync(LogEntry entry)
    {
        try
        {
            await _repository.InsertLogAsync(entry);
        }
        catch (Exception ex)
        {
            // Non blocca mai il flusso principale
            _logger.LogWarning(ex, "Errore non bloccante durante l'inserimento del log nel DB");
        }
    }

    /// <summary>
    /// Verifica se l'oggetto contiene una delle parole chiave configurate.
    /// </summary>
    private bool SubjectDiInteresse(string subject)
        => !string.IsNullOrWhiteSpace(subject)
           && _settings.SubjectsOfInterest.Any(k => subject.Contains(k, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Ottiene un access token OAuth2 per Office 365 (client credentials flow).
    /// </summary>
    private async Task<string> GetAccessTokenAsync()
    {
        var app = ConfidentialClientApplicationBuilder.Create(_settings.ClientId)
            .WithClientSecret(_settings.ClientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{_settings.TenantId}/v2.0")
            .Build();

        var scopes = new[] { "https://outlook.office365.com/.default" };
        var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
        return result.AccessToken;
    }
}
