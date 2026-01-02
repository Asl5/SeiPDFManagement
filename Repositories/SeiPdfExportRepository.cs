using Oracle.ManagedDataAccess.Client;
using SeiPDFManagement.Models;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace SeiPDFManagement.Repositories
{
    /// <summary>
    /// Repository Oracle per l'esportazione dei PDF SEIPDF.
    /// 
    /// Questa classe sostituisce l'accesso JDBC utilizzato
    /// nel canale Mirth SEIPDF_CREAPDF.
    /// 
    /// Responsabilità:
    /// - leggere i record dalla VIEW_H2H_SEI_PDF
    /// - estrarre il BLOB PDF
    /// - aggiornare gli stati applicativi
    /// - scrivere i log di processo
    /// </summary>
    public class SeiPdfExportRepository(
        IConfiguration config,
        ILogger<SeiPdfExportRepository> logger) : ISeiPdfExportRepository
    {
        private readonly string _cs = config.GetConnectionString("OracleConnectionString")
                ?? throw new InvalidOperationException(
                    "Manca la connection string OracleConnectionString");
        private readonly ILogger<SeiPdfExportRepository> _logger = logger;

        /// <summary>
        /// Apre una connessione Oracle.
        /// 
        /// NOTA:
        /// - non viene aperta una transazione esplicita
        /// - ogni operazione è autocommittata
        /// </summary>
        private async Task<OracleConnection> OpenAsync(CancellationToken ct)
        {
            var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);
            return conn;
        }

        // ------------------------------------------------------------------
        // LETTURA VIEW_H2H_SEI_PDF
        // ------------------------------------------------------------------

        /// <summary>
        /// Legge i record dalla VIEW_H2H_SEI_PDF.
        /// 
        /// La view restituisce:
        /// - al massimo un record per volta (coda DB)
        /// - solo i record pronti per l'esportazione
        /// </summary>
        public async IAsyncEnumerable<ViewH2HSeiPdfRow> ReadRowsAsync(
            int maxRecords,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            var sql = """
                SELECT
                    FRONTE_RETRO_DESC,
                    ELABORAZIONE,
                    TIPO,
                    SERVIZIO,
                    FILE_COMUNICAZIONE,
                    CENTRO_DI_COSTO
                FROM VIEW_H2H_SEI_PDF
                """;

            if (maxRecords > 0)
                sql += "\nFETCH FIRST :maxRecords ROWS ONLY";

            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (maxRecords > 0)
            {
                cmd.Parameters.Add(
                    new OracleParameter("maxRecords", OracleDbType.Int32)
                    {
                        Value = maxRecords
                    });
            }

            // SequentialAccess per gestire correttamente i BLOB
            await using var reader =
                await cmd.ExecuteReaderAsync(
                    System.Data.CommandBehavior.SequentialAccess,
                    ct);

            while (await reader.ReadAsync(ct))
            {
                var row = new ViewH2HSeiPdfRow
                {
                    FronteRetroDesc = reader.IsDBNull(0) ? null : reader.GetString(0),
                    Elaborazione = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Tipo = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Servizio = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CentroDiCosto = reader.IsDBNull(5) ? null : reader.GetString(5),
                    FileComunicazione = null
                };

                // Estrazione del BLOB PDF
                if (!reader.IsDBNull(4))
                {
                    using var blob = reader.GetOracleBlob(4);
                    row.FileComunicazione = blob.Value;
                }

                yield return row;
            }
        }

        // ------------------------------------------------------------------
        // AGGIORNAMENTO STATI
        // ------------------------------------------------------------------

        /// <summary>
        /// Aggiorna lo STATO di una richiesta.
        /// 
        /// Il commit è implicito (nessuna transazione esplicita)
        /// </summary>
        public async Task SetStatoAsync(
            string elaborazione,
            int stato,
            CancellationToken ct)
        {
            const string sql =
                "UPDATE H2H_WS_REQUEST SET STATO = :stato WHERE ELABORAZIONE = :elaborazione";

            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            cmd.Parameters.Add(new OracleParameter("stato", OracleDbType.Int32) { Value = stato });
            cmd.Parameters.Add(new OracleParameter("elaborazione", OracleDbType.Varchar2) { Value = elaborazione });

            var rows = await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogInformation(
                "Aggiornato STATO={Stato} per ELABORAZIONE={Elab} (rows={Rows})",
                stato, elaborazione, rows);
        }

        /// <summary>
        /// Imposta i flag SEIPDF e INVIATO a 1.
        /// </summary>
        public async Task SetSeiPdfInviatoAsync(
            string elaborazione,
            CancellationToken ct)
        {
            const string sql =
                "UPDATE H2H_WS_REQUEST SET SEIPDF=1, INVIATO=1 WHERE ELABORAZIONE = :elaborazione";

            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            cmd.Parameters.Add(new OracleParameter("elaborazione", OracleDbType.Varchar2) { Value = elaborazione });

            var rows = await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogInformation(
                "Aggiornato SEIPDF/INVIATO per ELABORAZIONE={Elab} (rows={Rows})",
                elaborazione, rows);
        }

        // ------------------------------------------------------------------
        // LOG DI PROCESSO
        // ------------------------------------------------------------------

        /// <summary>
        /// Inserisce un record nella tabella H2H_LOG_SEIPDF.
        /// 
        /// Eventuali errori di logging NON devono
        /// bloccare il flusso principale.
        /// </summary>
        public async Task InsertH2HLogSeiPdfAsync(
            string elaborazioneFileName,
            string messaggio,
            string tipoFile,
            CancellationToken ct)
        {
            const string sql = """
                INSERT INTO H2H_LOG_SEIPDF
                    (ELABORAZIONE, MESSAGGIO, TIPO_FILE)
                VALUES
                    (:elaborazione, :messaggio, :tipoFile)
                """;

            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            cmd.Parameters.Add(new OracleParameter("elaborazione", OracleDbType.Varchar2) { Value = elaborazioneFileName });
            cmd.Parameters.Add(new OracleParameter("messaggio", OracleDbType.Varchar2) { Value = messaggio });
            cmd.Parameters.Add(new OracleParameter("tipoFile", OracleDbType.Varchar2) { Value = tipoFile });

            await cmd.ExecuteNonQueryAsync(ct);
        }

        /// <summary>
        /// Inserisce un record nella tabella H2H_SEIPDF_LOG.
        /// </summary>
        public async Task InsertH2HSeiPdfLogAsync(
            string nomeFile,
            string tipoFile,
            int creato,
            CancellationToken ct)
        {
            const string sql = """
                INSERT INTO H2H_SEIPDF_LOG
                    (NOME_FILE, TIPO_FILE, CREATO, DATA_CREAZIONE)
                VALUES
                    (:nomeFile, :tipoFile, :creato, SYSDATE)
                """;

            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            cmd.Parameters.Add(new OracleParameter("nomeFile", OracleDbType.Varchar2) { Value = nomeFile });
            cmd.Parameters.Add(new OracleParameter("tipoFile", OracleDbType.Varchar2) { Value = tipoFile });
            cmd.Parameters.Add(new OracleParameter("creato", OracleDbType.Int32) { Value = creato });

            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
