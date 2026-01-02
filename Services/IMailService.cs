namespace SeiPDFManagement.Services
{
    /// <summary>
    /// Interfaccia che definisce le operazioni del servizio di lettura e gestione mail.
    /// </summary>
    public interface IMailService
    {
        /// <summary>
        /// Legge le email non lette dal server IMAP,
        /// scarica gli allegati delle mail di interesse
        /// e le marca come lette se elaborate con successo.
        /// </summary>
        /// <returns>Lista dei percorsi dei file allegati scaricati.</returns>
        Task<IEnumerable<string>> DownloadUnreadAsync();
    }
}
