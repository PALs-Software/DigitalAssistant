using DigitalAssistant.Server.Modules.Devices.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DigitalAssistant.Server.Tests.Mockups;

public class DbContextMockup : DbContext
{
    public DbContextMockup(DbContextOptions<DbContextMockup> options) : base(options) { }

    #region Devices
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<LightDevice> LightDevices { get; set; } = null!;
    public DbSet<SwitchDevice> SwitchDevices { get; set; } = null!;
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseCollation("Latin1_General_CS_AS"); // Make sure database is case sensitiv!

        #region Devices
        modelBuilder.Entity<Device>()
            .HasDiscriminator<string>("ModelType")
            .HasValue<Device>(nameof(Device))
            .HasValue<LightDevice>(nameof(LightDevice))
            .HasValue<SwitchDevice>(nameof(SwitchDevice));
        modelBuilder.Entity<Device>().HasIndex("ModelType");

        var deviceBaseProperties = typeof(Device).GetProperties().Select(entry => entry.Name).ToList();
        deviceBaseProperties.Add("ModelType");
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            if (typeof(Device).IsAssignableFrom(entityType.ClrType))
                foreach (var property in entityType.GetProperties())
                    if (!property.IsPrimaryKey() &&
                        !entityType.IsIgnored(property.Name) &&
                        !deviceBaseProperties.Contains(property.Name))
                        property.SetColumnName($"{entityType.ClrType.Name}_{property.Name}");
        #endregion
    }
}
