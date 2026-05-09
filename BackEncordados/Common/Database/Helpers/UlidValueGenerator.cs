using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace BackEncordados.Common.Database.Helpers;

public class UlidValueGenerator : ValueGenerator<Ulid>
{
    public override Ulid Next(EntityEntry entry) => Ulid.NewUlid();
    public override bool GeneratesTemporaryValues => false;
}