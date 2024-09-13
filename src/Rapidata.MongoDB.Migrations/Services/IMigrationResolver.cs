using System.Reflection;
using Rapidata.MongoDB.Migrations.Contracts;

namespace Rapidata.MongoDB.Migrations.Services;

public interface IMigrationResolver
{
    IEnumerable<IMigration> GetMigrations(IEnumerable<Assembly> assemblies);
}
