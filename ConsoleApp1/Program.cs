using System.Collections.ObjectModel;
using ConsoleTables;
using Detached.Mappers;
using Detached.Mappers.EntityFramework;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Starting...");
        using (DetachedDbContext context = new DetachedDbContext())
        {
            // create the sqlite db.
            context.Database.EnsureCreated();
            ListCreators(context);
            Console.WriteLine("Migration successful");
            var newCreator = context.Map<Creator>(new Creator
            {
                FullName = "Daniel Abraham.", PrimaryLanguage = "EN",
                Works = new() { new Work { Title = @"The Dragon's Path.", Language = "EN" } }
            });
            context.SaveChanges();
            var newId = newCreator.Id;
            ListCreators(context);
            Console.WriteLine("Adding more data successful");
            var updatedCreator = context.Map<Creator>(new Creator
            {
                Id = newId, FullName = "Daniel Abraham",
                Works = new()
                {
                    new Work { Id = 42, Title = @"The Dragon's Path", Language = "EN" },
                    new Work { Title = @"The Tyrant's Law", Language = "EN" },
                }
            });
            context.SaveChanges();
            ListCreators(context);
        }
    }

    public static void ListCreators(DetachedDbContext context)
    {
        var creators = context.Creators.Include(c => c.Works);

        foreach (var creator in creators)
        {
            Console.WriteLine($@"Creator : {creator.Id} - {creator.FullName}");
            ConsoleTable.From(creator.Works).Write();
        }
    }
}

public class DetachedDbContext : DbContext
{
    static SqliteConnection _connection;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
            _connection.Open();
        }

        optionsBuilder.UseSqlite(_connection)
            .UseDetached()
            .UseMapping(mapperConfig =>
            {
                mapperConfig.Default(mapperOptions =>
                {
                    mapperOptions.Type<Creator>().Member(f => f.Works).Composition();
                });
            });
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CreatorConfiguration());
        modelBuilder.ApplyConfiguration(new WorkConfiguration());
        modelBuilder.Entity<Creator>().HasMany<Work>(c => c.Works).WithOne(w => w.Creator)
            .HasForeignKey(w => w.CreatorId);
        modelBuilder.Entity<Creator>().HasData(new Creator
        {
            Id = 1, FullName = @"Douglas Adams", PrimaryLanguage = "EN", Born = new DateTime(1952, 4, 11),
            Died = new DateTime(2001, 5, 11)
        });
        modelBuilder.Entity<Work>().HasData(new Work { Id = 41, CreatorId = 1, Title = @"Young Zaphod Plays It Safe", Language = "EN"});
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Creator> Creators { get; set; }

    public DbSet<Work> Works { get; set; }
}

public class Creator
{
    public ulong Id { get; set; }

    public string FullName { get; set; }

    public DateTime Born { get; set; }

    public DateTime Died { get; set; }

    public string PrimaryLanguage { get; set; }

    public Collection<Work> Works { get; set; }
}

public class Work
{
    public ulong Id { get; set; }

    public string Title { get; set; }

    public string Language { get; set; }

    public ulong CreatorId { get; set; }

    public Creator Creator { get; set; }
}

public class CreatorConfiguration : IEntityTypeConfiguration<Creator>
{
    public void Configure(EntityTypeBuilder<Creator> builder)
    {
        builder.HasMany(p => p.Works).WithOne(a => a.Creator).OnDelete(DeleteBehavior.Cascade);
        builder.HasKey(p => p.Id);
    }
}

public class WorkConfiguration : IEntityTypeConfiguration<Work>
{
    public void Configure(EntityTypeBuilder<Work> builder)
    {
        builder.HasKey(p => p.Id);
    }
}