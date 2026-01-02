using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace SeiPDFManagement.Services;

/// <summary>
/// Servizio per leggere email via Microsoft Graph e salvare gli allegati
/// filtrando per oggetto.
/// </summary>
public class GraphMailService : IMailService
{
    private readonly MailSettings _settings;
    private readonly ILogger<GraphMailService> _logger;
    private readonly IHttpClientFactory _httpFactory;

    public GraphMailService(
        IOptions<MailSettings> settings,
        ILogger<GraphMailService> logger,
        IHttpClientFactory httpFactory)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpFactory = httpFactory;
    }

    public async Task<IEnumerable<string>> DownloadUnreadAsync()
    {
        var token = await GetAccessTokenAsync();
        var client = _httpFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // 1️⃣ Cerca solo le email non lette, filtrando per oggetto
        var filter = string.Join(" or ", _settings.SubjectsOfInterest.Select(s => $"contains(subject,'{s}')"));
        var url = $"https://graph.microsoft.com/v1.0/users/{_settings.UserEmail}/messages?$filter=isRead eq false and ({filter})&$top={_settings.MaxEmailsToRead}";

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        var messages = json["value"] ?? new JArray();
        var downloadedFiles = new List<string>();

        foreach (var msg in messages)
        {
            string id = msg["id"]?.ToString() ?? "";
            string subject = msg["subject"]?.ToString() ?? "";
            _logger.LogInformation("Elaboro email: {Subject}", subject);

            // 2️⃣ Ottieni gli allegati
            var attachUrl = $"https://graph.microsoft.com/v1.0/users/{_settings.UserEmail}/messages/{id}/attachments";
            var attachResp = await client.GetAsync(attachUrl);
            attachResp.EnsureSuccessStatusCode();

            var attachJson = JObject.Parse(await attachResp.Content.ReadAsStringAsync());
            foreach (var attachment in attachJson["value"] ?? new JArray())
            {
                if (attachment["@odata.type"]?.ToString() == "#microsoft.graph.fileAttachment")
                {
                    var name = attachment["name"]?.ToString() ?? "unknown";
                    var contentBytes = Convert.FromBase64String(attachment["contentBytes"]?.ToString() ?? "");
                    var path = Path.Combine(_settings.AttachmentDir, name);
                    Directory.CreateDirectory(_settings.AttachmentDir);
                    await File.WriteAllBytesAsync(path, contentBytes);
                    downloadedFiles.Add(path);

                    _logger.LogInformation("Salvato allegato: {File}", path);
                }
            }

            // 3️⃣ Marca la mail come letta
            var patch = new StringContent("{\"isRead\": true}", System.Text.Encoding.UTF8, "application/json");
            await client.PatchAsync($"https://graph.microsoft.com/v1.0/users/{_settings.UserEmail}/messages/{id}", patch);
        }

        return downloadedFiles;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var app = ConfidentialClientApplicationBuilder.Create(_settings.ClientId)
            .WithClientSecret(_settings.ClientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{_settings.TenantId}")
            .Build();

        var scopes = new[] { "https://graph.microsoft.com/.default" };
        var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
        return result.AccessToken;
    }
}
