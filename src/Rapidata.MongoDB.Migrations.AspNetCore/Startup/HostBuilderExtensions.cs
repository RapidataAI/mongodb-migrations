using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rapidata.MongoDB.Migrations.Config;
using Rapidata.MongoDB.Migrations.Core;
using Rapidata.MongoDB.Migrations.Services;

namespace Rapidata.MongoDB.Migrations.AspNetCore.Startup;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseMongoDbMigrations(
        this IHostBuilder hostBuilder,
        Action<MigrationConfigBuilder>? configure = null,
        ILogger? logger = null)
    {
        var configBuilder = new MigrationConfigBuilder();
        configure?.Invoke(configBuilder);

        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddSingleton<IMigrationService, MigrationService>();
            services.AddSingleton<MigrationEngine>();
            services.AddSingleton<IMigrationResolver, MigrationResolver>();
            services.AddSingleton(configBuilder.Build(logger));
        });
    }
}