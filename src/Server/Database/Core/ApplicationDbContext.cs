using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core;

public abstract class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<WeatherForecast> WeatherForecasts { get; set; }

    public ApplicationDbContext(DbContextOptions options)
            : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        WeatherForecast[] items = GenerateRecords();

        builder.Entity<WeatherForecast>().HasData(items);
    }

    private static WeatherForecast[] GenerateRecords()
    {
        string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        var rng = new Random();
        var items = Enumerable.Range(1, 100).Select(index => new WeatherForecast
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            Date = DateTime.Now.AddDays(index).UTCKind(),
            Temperature = rng.Next(-20, 55),
            Summary = Summaries[rng.Next(Summaries.Length)],
            CreateDate = DateTime.Now.UTCKind(),
            UpdateDate = DateTime.Now.UTCKind(),
        }).ToArray();
        return items;
    }
}
