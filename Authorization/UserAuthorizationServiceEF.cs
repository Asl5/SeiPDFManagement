using Microsoft.EntityFrameworkCore;
using SeiPDFManagement.Data;

namespace SeiPDFManagement.Authorization
{
    public class UserAuthorizationServiceEF(
        AuthorizationDbContext db,
        ILogger<UserAuthorizationServiceEF> logger) : IUserAuthorizationService
    {
        private readonly AuthorizationDbContext _db = db;
        private readonly ILogger<UserAuthorizationServiceEF> _logger = logger;

        public async Task<bool> IsEnabledAsync(string rawUser)
        {
            if (string.IsNullOrWhiteSpace(rawUser))
                return false;

            // DOMAIN\user → user
            var username = rawUser.Contains("\\")
                ? rawUser.Split('\\')[1]
                : rawUser;

            var now = DateTime.Now;

            var abilitato = await _db.UtentiMonitoraggio
                .AsNoTracking()
                .AnyAsync(u =>
                    u.Username == username &&
                    (u.DataInizio == null || u.DataInizio <= now) &&
                    (u.DataFine == null || u.DataFine >= now));

            if (!abilitato)
                _logger.LogWarning("Utente {User} NON autorizzato", username);

            return abilitato;
        }
    }

}
