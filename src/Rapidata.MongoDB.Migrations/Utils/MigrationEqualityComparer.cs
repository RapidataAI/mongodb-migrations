using Rapidata.MongoDB.Migrations.Contracts;

namespace Rapidata.MongoDB.Migrations.Utils;

public class MigrationEqualityComparer : IEqualityComparer<IBaseMigration>
{
    public bool Equals(IBaseMigration? x, IBaseMigration? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.Date.Equals(y.Date) && x.Version == y.Version && x.DeveloperId == y.DeveloperId;
    }

    public int GetHashCode(IBaseMigration obj)
    {
        return HashCode.Combine(obj.Date, obj.Version, obj.DeveloperId);
    }
}