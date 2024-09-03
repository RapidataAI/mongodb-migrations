namespace Rapidata.MongoDB.Migrations.Exceptions;

public sealed class HasFailedMigrationsException : Exception
{
    public HasFailedMigrationsException() : base("There are failed migrations.")
    {
    }
}