using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeiPDFManagement.Models
{
    [Table("UTENTI_MONITORAGGIO")]
    public class UtenteMonitoraggio
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("USERNAME")]
        public string Username { get; set; } = "";

        [Column("DATAINIZIO")]
        public DateTime? DataInizio { get; set; }

        [Column("DATAFINE")]
        public DateTime? DataFine { get; set; }

        [Column("RUOLO")]
        public string? Ruolo { get; set; }
    }
}
