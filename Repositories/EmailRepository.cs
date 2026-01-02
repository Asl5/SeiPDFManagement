using Dapper;
using Oracle.ManagedDataAccess.Client;
using SeiPDFManagement.Models;
using SeiPDFManagement.Services.Context;

namespace SeiPDFManagement.Repositories
{
    public class EmailRepository(IRequestContext requestContext,IConfiguration config, ILogger<EmailRepository> logger) : IEmailRepository
    {
        private readonly string _connectionString = config.GetConnectionString("OracleConnectionString")
                ?? throw new InvalidOperationException("Manca la connection string OracleConnectionString");
        private readonly ILogger<EmailRepository> _logger = logger;
        private readonly IRequestContext _context = requestContext;

        private OracleConnection CreateConnection()
        {
            var conn = new OracleConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // ---------------------------------------------------
        // OPERAZIONI
        // ---------------------------------------------------

        public async Task<IEnumerable<string>> GetRecordsByLottoAsync(string lotto)
        {
            const string sql = """
            SELECT NOME_FILE 
            FROM H2H_SEIPDF_LOG 
            WHERE LOTTO = :lotto AND TIPO_FILE = '.pdf'
        """;

            await using var conn = CreateConnection();
            var result = await conn.QueryAsync<string>(sql, new { lotto });
            _logger.LogInformation("Trovati {Count} record per lotto {Lotto}", result.Count(), lotto);
            return result;
        }

        public async Task<int> UpdateRecordStatusAsync(int statusId, int elaborazione)
        {
            const string queryRequest = """
            MERGE INTO H2H_WS_REQUEST log
            USING (
                SELECT stati.ID_ASL5 AS new_stato
                FROM H2H_SEIPDF_STATI stati
                WHERE stati.ID_POSTEL = :statusId
            ) nuovi
            ON (log.ELABORAZIONE = :elaborazione)
            WHEN MATCHED THEN
                UPDATE SET 
                    log.STATO = nuovi.new_stato,
                    log.DATA_SEIPDF = SYSDATE
                WHERE nuovi.new_stato > log.STATO
        """;

            const string queryAnag = """
            MERGE INTO H2H_ANAGRAFICA_INVII log
            USING (
                SELECT :elaborazione AS elaborazione,
                       stati.ID_ASL5 AS new_stato
                FROM H2H_SEIPDF_STATI stati
                WHERE stati.ID_POSTEL = :statusId
            ) dati
            ON (log.ID_ELABORAZIONE = dati.elaborazione)
            WHEN MATCHED THEN
                UPDATE SET log.DA_ELABORARE = dati.new_stato
                WHERE dati.new_stato > log.DA_ELABORARE
        """;

            await using var conn = CreateConnection();
            await using var tx = conn.BeginTransaction();

            try
            {
                var p = new { statusId, elaborazione };
                var rows1 = await conn.ExecuteAsync(queryRequest, p, tx);
                var rows2 = await conn.ExecuteAsync(queryAnag, p, tx);
                await tx.CommitAsync();

                _logger.LogInformation(
                    "Aggiornati {Rows1} record in H2H_WS_REQUEST e {Rows2} in H2H_ANAGRAFICA_INVII (Elaborazione={Elab})",
                    rows1, rows2, elaborazione);

                await InsertLogAsync(new LogEntry
                {
                    TipoLog = LogType.Info,
                    Messaggio = "Aggiornati " + rows1 + " record in H2H_WS_REQUEST e " + rows2 + " in H2H_ANAGRAFICA_INVII (Elaborazione=" + elaborazione + ")",
                    Funzione = nameof(UpdateRecordStatusAsync),
                    Servizio = _context.ControllerName ?? "UNKNOWN"
                });

                return rows1 + rows2;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Errore durante l'update per elaborazione {Elab}", elaborazione);
                await InsertLogAsync(new LogEntry
                {
                    TipoLog = LogType.Error,
                    Messaggio = "Errore durante l'update per elaborazione " + elaborazione +": " + ex.Message,
                    Funzione = nameof(UpdateRecordStatusAsync),
                    Servizio = _context.ControllerName ?? "UNKNOWN",
                    Dati = ex.StackTrace
                });
                throw;
            }
        }

        public async Task<int> InsertLogAsync(LogEntry logEntry)
        {
            const string sql = """
            INSERT INTO LOG_TABLE 
                (TIPO_LOG, MESSAGGIO, FUNZIONE, PARAMS, SERVIZIO, DATI)
            VALUES 
                (:TipoLog, :Messaggio, :Funzione, :Parametri, :Servizio, :Dati)
        """;

            await using var conn = CreateConnection();

            try
            {
                var result = await conn.ExecuteAsync(sql, new
                {
                    TipoLog = logEntry.TipoLog.ToString(),
                    logEntry.Messaggio,
                    logEntry.Funzione,
                    Parametri = logEntry.Parametri ?? string.Empty,
                    logEntry.Servizio,
                    Dati = logEntry.Dati ?? string.Empty
                });

                _logger.LogDebug("Inserito log nel DB: {Msg}", logEntry.Messaggio);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore durante inserimento log: {Msg}", logEntry.Messaggio);
                return 0;
            }
        }
    }
}
