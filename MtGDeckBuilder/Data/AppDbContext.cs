using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore;
using MtGDeckBuilder.Models;
using System.Text.Json;

namespace MtGDeckBuilder.Data;

public class AppDbContext : DbContext 
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder b) {
        // JSON converter for string[] → single TEXT column
        var stringArrayToJson = new ValueConverter<string[], string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>());

        b.Entity<Card>(e =>
        {
            e.HasIndex(x => x.OracleId).IsUnique();
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Cmc);
            e.HasIndex(x => x.ColorIdentity);

            e.Property(x => x.Colors).HasConversion(stringArrayToJson);
            e.Property(x => x.ColorIdentity).HasConversion(stringArrayToJson);
        });
    }
}