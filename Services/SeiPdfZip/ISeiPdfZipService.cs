namespace SeiPDFManagement.Services.SeiPdfZip
{
    /// <summary>
    /// Servizio responsabile della creazione degli archivi ZIP e dei file .t
    /// a partire dai PDF presenti nella directory di input.
    /// Replica e sostituisce il canale Mirth SEIPDF_CREAZIP.
    /// </summary>
    public interface ISeiPdfZipService
    {
        /// <summary>
        /// Esegue il processo di creazione ZIP.
        /// In una singola esecuzione può generare uno o più ZIP
        /// in base ai limiti configurati.
        /// </summary>
        Task<SeiPdfZipResult> CreateZipsAsync(CancellationToken ct);
    }

    /// <summary>
    /// Risultato dell'operazione di creazione ZIP SEIPDF.
    /// <para>
    /// Rappresenta il riepilogo di una singola esecuzione del servizio di zippatura,
    /// che replica il comportamento del canale Mirth SEIPDF_CREAZIP.
    /// </para>
    /// </summary>
    public sealed class SeiPdfZipResult
    {
        /// <summary>
        /// Numero totale di file PDF individuati nella directory di input
        /// al momento dell'esecuzione del servizio.
        /// </summary>
        public int TotalPdfFound { get; set; }

        /// <summary>
        /// Numero di file ZIP creati durante questa esecuzione.
        /// <para>
        /// Può essere maggiore di 1 se la configurazione consente
        /// la creazione di più ZIP nella stessa run.
        /// </para>
        /// </summary>
        public int ZipCreated { get; set; }

        /// <summary>
        /// Numero totale di file PDF effettivamente inseriti nei file ZIP.
        /// <para>
        /// I PDF zippati vengono rimossi dalla directory di input
        /// al termine dell'operazione.
        /// </para>
        /// </summary>
        public int PdfZipped { get; set; }

        /// <summary>
        /// Elenco completo dei percorsi dei file ZIP creati.
        /// <para>
        /// I percorsi fanno riferimento alla directory di output configurata
        /// nel servizio di zippatura.
        /// </para>
        /// </summary>
        public List<string> ZipFiles { get; set; } = [];
    }

}
