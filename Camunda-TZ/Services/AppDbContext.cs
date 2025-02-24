using System.Reflection;
using Camunda_TZ.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Camunda_TZ.Services;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets { get; set; }

    public DbSet<TicketFile> Files { get; set; }
}

internal class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseNpgsql(GetConnectionString(),
            b => b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name));

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string? GetConnectionString()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, false);

        var configuration = builder.Build();

        return configuration.GetConnectionString("DefaultConnection");
    }
}