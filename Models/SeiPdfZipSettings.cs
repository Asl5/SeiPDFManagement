namespace SeiPDFManagement.Models
{
    public class SeiPdfZipSettings
    {
        public string InputDirectory { get; set; } = "";
        public int MaxZipSizeMb { get; set; } = 15;
        public int MaxFilesPerZip { get; set; } = 250;
        public bool CreateMultipleZipsPerRun { get; set; } = true;
    }
}
