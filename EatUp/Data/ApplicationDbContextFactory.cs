using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EatUp.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(
            "Server=localhost;Port=3306;Database=eatup_db;User=root;Password=root;",
            new MySqlServerVersion(new Version(8, 0, 0)));
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
