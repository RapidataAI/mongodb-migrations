# Rapidata.MongoDB.Migrations

A modern, thread-safe MongoDB migration engine for C#

[![NuGet](https://img.shields.io/nuget/v/Rapidata.MongoDB.Migrations.svg)](https://www.nuget.org/packages/Rapidata.MongoDB.Migrations/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Features

- 🔒 Thread-safe: Can be run by multiple services simultaneously
- 🔄 Guaranteed single execution for each migration
- 📚 Support for multiple database migrations
- ⚙️ Flexible configuration options
- 🚀 Easy integration with ASP.NET Core

## Usage


### Basic Usage

To get started, define the migrations in an assembly.

```csharp
// Define a migration
public class TestMigration : IMigration
{
    public DateOnly Date => new(2024, 10, 10);
    public short Version => 1;
    public string Name => "Test Migration";

    public async Task Migrate(IMongoDatabase database, CancellationToken cancellationToken)
    {
        await database.CreateCollectionAsync(nameof(TestMigration), options: null, cancellationToken);
    }
}
```

Then, register the migrations in the host and define at least one database to migrate.

```csharp
// Register dependencies on the host
host.AddMongoDbMigrations(builder => builder.WithDatabase("default").WithMigrationsInAssemblyOfType<TestMigration>);
```

```csharp
// Run the migrations
await host.MigrateMongoDb();
```
