using Microsoft.EntityFrameworkCore;
using gadget_repair.Data;

public class DesignTimeDbContext : ApplicationDbContext
{
    public DesignTimeDbContext()
        : base(CreateOptions())
    {
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();

        options.UseMySql(
            DatabaseConfig.ConnectionString,
            ServerVersion.AutoDetect(DatabaseConfig.ConnectionString)
        );

        return options.Options;
    }
}