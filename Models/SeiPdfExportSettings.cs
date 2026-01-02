namespace SeiPDFManagement.Models
{
    public class SeiPdfExportSettings
    {
        public string OutputDirectory { get; set; } = @"\\pvesbasl5\e$\DaInviare";
        public int MaxRecords { get; set; } = 0; // 0 = nessun limite
    }
}
