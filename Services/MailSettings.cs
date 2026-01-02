namespace SeiPDFManagement.Services
{
    /// <summary>
    /// Impostazioni configurabili tramite appsettings.json
    /// per la connessione IMAP e la gestione delle email.
    /// </summary>
    public class MailSettings
    {
        /// <summary> Indirizzo del server IMAP (es. outlook.office365.com). </summary>
        public string ImapServer { get; set; } = string.Empty;

        /// <summary> Porta IMAP (993 per SSL). </summary>
        public int ImapPort { get; set; } = 993;

        /// <summary> ID del tenant Azure AD. </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary> ID dell’app registrata in Azure AD. </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary> Secret dell’app registrata. </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary> Indirizzo email dell’utente associato alla casella da leggere. </summary>
        public string UserEmail { get; set; } = string.Empty;

        /// <summary> Cartella dove salvare gli allegati scaricati. </summary>
        public string AttachmentDir { get; set; } = string.Empty;

        /// <summary> Lista di parole chiave o frasi da cercare nell’oggetto delle email. </summary>
        public List<string> SubjectsOfInterest { get; set; } = new();

        /// <summary> Numero massimo di email da leggere per ciclo (0 = nessun limite). </summary>
        public int MaxEmailsToRead { get; set; } = 0;
    }
}
