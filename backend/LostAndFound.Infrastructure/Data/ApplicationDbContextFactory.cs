using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace LostAndFound.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Try to load configuration from the API project if available
        var basePath = Directory.GetCurrentDirectory();
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();

        // Also probe API folder if running from Infrastructure directory
        var apiPath = Path.Combine(Directory.GetParent(basePath)?.FullName ?? basePath, "LostAndFound.Api");
        if (Directory.Exists(apiPath))
        {
            builder.AddJsonFile(Path.Combine(apiPath, "appsettings.json"), optional: true)
                   .AddJsonFile(Path.Combine(apiPath, "appsettings.Development.json"), optional: true);
        }

        var config = builder.Build();
        var cs = config.GetConnectionString("DefaultConnection")
                 ?? "Host=localhost;Port=5432;Database=lostandfound;Username=lostandfound;Password=lostandfound";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(cs);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
