using BackEncordados.Common.Database.Helpers;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

public class MaterialsDbContext(DbContextOptions<MaterialsDbContext> options): DbContext(options)
{
    public DbSet<Cuerdas> Cuerdas { get; set; }
    public DbSet<Material> Materiales { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cuerdas>(builder =>
        {
            builder.ToTable("Cuerdas"); 
            builder.HasKey(c => c.Id);  

            builder.Property(c => c.Marca).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Modelo).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Stock).IsRequired();
            builder.Property(c => c.Precio).IsRequired();

            builder.Property(c => c.StringFormat)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(c => c.StringsType)
                .HasConversion<string>()
                .IsRequired();
            builder.ConfigureTimestamps();
            builder.HasQueryFilter(u => !u.IsDeleted);
        });

        modelBuilder.Entity<Material>(builder =>
        {
            builder.ToTable("Materiales");
            builder.HasKey(m => m.Id); 

            builder.Property(m => m.Marca).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Modelo).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Stock).IsRequired();
            builder.Property(m => m.Precio).IsRequired();

            builder.Property(m => m.Type)
                .HasConversion<string>()
                .IsRequired();

            builder.ConfigureTimestamps();
            
            builder.HasQueryFilter(u => !u.IsDeleted);
        });
    }
}
