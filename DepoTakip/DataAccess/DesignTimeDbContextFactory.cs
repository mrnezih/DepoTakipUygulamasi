using Microsoft.EntityFrameworkCore.Design;

namespace DepoTakip.DataAccess
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
    {
        public DatabaseContext CreateDbContext(string[] args)
        {
            return new DatabaseContext();
        }
    }
}