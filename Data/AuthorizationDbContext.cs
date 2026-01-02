using Microsoft.EntityFrameworkCore;
using SeiPDFManagement.Models;
using System.Collections.Generic;

namespace SeiPDFManagement.Data
{
    public class AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options) : DbContext(options)
    {
        public DbSet<UtenteMonitoraggio> UtentiMonitoraggio => Set<UtenteMonitoraggio>();
    }
}
