namespace SeiPDFManagement.Models
{
    /// <summary>
    /// Modello che rappresenta un record di log da scrivere nel database LOG_TABLE.
    /// </summary>
    public class LogEntry
    {
        /// <summary> Livello del log (INFO, WARN, ERROR, DEBUG). </summary>
        public LogType TipoLog { get; set; } = LogType.Info;

        /// <summary> Messaggio principale del log. </summary>
        public string Messaggio { get; set; } = string.Empty;

        /// <summary> Nome del metodo o contesto che ha generato il log. </summary>
        public string Funzione { get; set; } = string.Empty;

        /// <summary> Parametri contestuali (facoltativi). </summary>
        public string? Parametri { get; set; }

        /// <summary> Nome del servizio o componente chiamante. </summary>
        public string Servizio { get; set; } = string.Empty;

        /// <summary> Eventuali dati extra o payload serializzato. </summary>
        public string? Dati { get; set; }

        /// <summary> Timestamp locale del log. </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Crea un LogEntry a partire da un messaggio generico.
        /// </summary>
        public static LogEntry Info(string servizio, string funzione, string messaggio, string? dati = null, string? parametri = null)
            => new()
            {
                TipoLog = LogType.Info,
                Servizio = servizio,
                Funzione = funzione,
                Messaggio = messaggio,
                Dati = dati,
                Parametri = parametri
            };

        /// <summary>
        /// Crea un LogEntry di tipo warning.
        /// </summary>
        public static LogEntry Warn(string servizio, string funzione, string messaggio, string? dati = null)
            => new()
            {
                TipoLog = LogType.Warn,
                Servizio = servizio,
                Funzione = funzione,
                Messaggio = messaggio,
                Dati = dati
            };

        /// <summary>
        /// Crea un LogEntry di errore partendo da un'eccezione.
        /// </summary>
        public static LogEntry FromException(Exception ex, string servizio, string funzione, string? parametri = null)
            => new()
            {
                TipoLog = LogType.Error,
                Servizio = servizio,
                Funzione = funzione,
                Messaggio = ex.Message,
                Dati = ex.StackTrace,
                Parametri = parametri
            };
    }

    /// <summary>
    /// Livelli di severità per il logging applicativo.
    /// </summary>
    public enum LogType
    {
        Info,
        Warn,
        Error,
        Debug
    }
}