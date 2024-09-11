using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Connectors.Models;
using DigitalAssistant.Server.Modules.Devices.Models;
using DigitalAssistant.Server.Modules.Files;
using DigitalAssistant.Server.Modules.Setups.Models;
using DigitalAssistant.Server.Modules.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DigitalAssistant.Server.Data;

public class ApplicationDbContext(DbContextOptions options) : IdentityDbContext(options)
{
    #region Basics

    public DbSet<User> DbUsers { get; set; } = null!;

    public DbSet<ServerFile> ServerFiles { get; set; } = null!;

    public DbSet<Setup> Setup { get; set; } = null!;

    #endregion

    #region Clients
    public DbSet<Client> Clients { get; set; } = null!;
    #endregion

    #region Devices
    public DbSet<ConnectorSettings> ConnectorSettings { get; set; } = null!;
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