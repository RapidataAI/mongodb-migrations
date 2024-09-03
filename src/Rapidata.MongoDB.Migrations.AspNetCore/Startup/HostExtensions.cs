using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rapidata.MongoDB.Migrations.Config;
using Rapidata.MongoDB.Migrations.Core;

namespace Rapidata.MongoDB.Migrations.AspNetCore.Startup;

public static class HostExtensions
{
    public static async Task<IHost> MigrateMongoDb(this IHost host)
    {
        var migrationConfig = host.Services.GetRequiredService<MigrationConfig>();

        CancellationToken cancellationToken;
        if (migrationConfig.MigrationTimeout.HasValue)
        {
            var cancellationTokenSource = new CancellationTokenSource(migrationConfig.MigrationTimeout.Value);
            cancellationToken = cancellationTokenSource.Token;
        }
        else
        {
            cancellationToken = CancellationToken.None;
        }

        await host.Services.GetRequiredService<MigrationEngine>().Migrate(cancellationToken).ConfigureAwait(false);
        return host;
    }
}