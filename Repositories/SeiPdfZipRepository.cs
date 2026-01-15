using Oracle.ManagedDataAccess.Client;

namespace SeiPDFManagement.Repositories
{
    /// <summary>
    /// Implementazione Oracle del repository ZIP SEIPDF.
    /// Tutte le operazioni sono autocommit (come nel canale Mirth).
    /// </summary>
    public class SeiPdfZipRepository(
        IConfiguration config,
        ILogger<SeiPdfZipRepository> logger) : ISeiPdfZipRepository
    {
        private readonly string _cs =
            config.GetConnectionString("OracleConnectionString")
            ?? throw new InvalidOperationException("Manca la connection string OracleConnectionString");

        private readonly ILogger<SeiPdfZipRepository> _logger = logger;

        private async Task<OracleConnection> OpenAsync(CancellationToken ct)
        {
            var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);
            return conn;
        }

        /// <inheritdoc />
        public async Task<string> GetNextZipSequenceAsync(CancellationToken ct)
        {
            const string sql = "SELECT H2H_SEIPDF_ZIP.NEXTVAL FROM dual";

            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            var value = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));

            // zero-padding a 7 cifre
            var formatted = value.ToString("D7");

            _logger.LogInformation("Sequence ZIP generata: {Seq}", formatted);
            return formatted;
        }

        /// <inheritdoc />
        public async Task MarkPdfAsZippedAsync(string zipName, string pdfName, CancellationToken ct)
        {
            const string sql = """
                UPDATE H2H_SEIPDF_LOG
                SET LOTTO = :zipName,
                    DATA_ZIP = SYSDATE,
                    ZIPPATO = 1
                WHERE NOME_FILE = :pdfName
                  AND LOTTO IS NULL
            """;

            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new OracleParameter("zipName", OracleDbType.Varchar2) { Value = zipName });
            cmd.Parameters.Add(new OracleParameter("pdfName", OracleDbType.Varchar2) { Value = pdfName });

            var rows = await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogInformation(
                "PDF {Pdf} associato al lotto ZIP {Zip} (rows={Rows})",
                pdfName, zipName, rows);
        }

        /// <inheritdoc />
        public async Task InsertH2HLogSeiPdfAsync(
            string elaborazione,
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

            cmd.Parameters.Add(new OracleParameter("elaborazione", OracleDbType.Varchar2) { Value = elaborazione });
            cmd.Parameters.Add(new OracleParameter("messaggio", OracleDbType.Varchar2) { Value = messaggio });
            cmd.Parameters.Add(new OracleParameter("tipoFile", OracleDbType.Varchar2) { Value = tipoFile });

            await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogDebug(
                "Inserito log SEIPDF: {Elab} {Tipo}",
                elaborazione, tipoFile);
        }
    }
}
