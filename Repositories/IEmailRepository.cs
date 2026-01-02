using SeiPDFManagement.Models;

namespace SeiPDFManagement.Repositories
{
    public interface IEmailRepository
    {
        Task<IEnumerable<string>> GetRecordsByLottoAsync(string lotto);
        Task<int> UpdateRecordStatusAsync(int statusId, int elaborazione);
        Task<int> InsertLogAsync(LogEntry logEntry);
    }
}
