using Microsoft.Extensions.Configuration;

namespace RepoPulse.Cli.Config;

public static class AppConfig
{
    public static IConfigurationRoot Load()
    {
        // Supports:
        // - src/RepoPulse.Cli/appsettings.json
        // - environment variables (optional)
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
    }

    public static string GetConnectionString(IConfiguration config)
    {
        var cs = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in appsettings.json");
        return cs;
    }
}