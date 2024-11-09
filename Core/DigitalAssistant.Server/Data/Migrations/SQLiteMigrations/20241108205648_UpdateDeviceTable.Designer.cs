﻿// <auto-generated />
using System;
using DigitalAssistant.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DigitalAssistant.Server.Data.Migrations.SQLiteMigrations
{
    [DbContext(typeof(SQLiteDbContext))]
    [Migration("20241108205648_UpdateDeviceTable")]
    partial class UpdateDeviceTable
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseCollation("Latin1_General_CS_AS")
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true);

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Clients.Models.Client", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("ClientNeedSettingsUpdate")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<int>("DashboardOrder")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("GroupId")
                        .HasColumnType("TEXT");

                    b.Property<bool>("HasBeenInitialized")
                        .HasColumnType("INTEGER");

                    b.Property<string>("InputDeviceId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<float>("OutputAudioVolume")
                        .HasColumnType("REAL");

                    b.Property<string>("OutputDeviceId")
                        .HasColumnType("TEXT");

                    b.Property<bool>("PlayRequestSound")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowInDashboard")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("SqlRowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ValidUntil")
                        .HasColumnType("TEXT");

                    b.Property<int>("VoiceAudioOutputSampleRate")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("GroupId");

                    b.ToTable("Clients");
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Connectors.Models.ConnectorSettings", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("SettingsAsJson")
                        .HasColumnType("TEXT");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ConnectorSettings");
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Devices.Models.Device", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AdditionalJsonData")
                        .HasColumnType("TEXT");

                    b.Property<string>("AlternativeNames")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Connector")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<bool>("CustomName")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DashboardOrder")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("GroupId")
                        .HasColumnType("TEXT");

                    b.Property<string>("InternalId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Manufacturer")
                        .HasColumnType("TEXT");

                    b.Property<string>("ModelType")
                        .IsRequired()
                        .HasMaxLength(13)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ProductName")
                        .HasColumnType("TEXT");

                    b.Property<bool>("ShowInDashboard")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("GroupId");

                    b.HasIndex("ModelType");

                    b.HasIndex("Type");

                    b.ToTable("Devices");

                    b.HasDiscriminator<string>("ModelType").HasValue("Device");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Files.ServerFile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("BaseFileType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("FileSize")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Hash")
                        .HasColumnType("TEXT");

                    b.Property<string>("MimeFileType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("TEXT");

                    b.Property<int>("SortIndex")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("ServerFiles");
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Groups.Models.Group", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AlternativeNames")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<int>("DashboardOrder")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("IconId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<bool>("ShowInDashboard")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("IconId");

                    b.ToTable("Groups");
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Setups.Models.Setup", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("AsrLanguage")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AsrMode")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AsrModel")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AsrPrecision")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AsrProvider")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<bool>("InitalSetupCompleted")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InterpreterLanguage")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InterpreterMode")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LlmContextSize")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LlmMode")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LlmModel")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LlmPrecision")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("TEXT");

                    b.Property<int>("TtsLanguage")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TtsMode")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TtsModel")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("TtsModelQuality")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Setup");
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Users.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("IdentityRole")
                        .HasColumnType("INTEGER");

                    b.Property<string>("IdentityUserId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("TEXT");

                    b.Property<int>("PreferredCulture")
                        .HasColumnType("INTEGER");

                    b.Property<bool?>("PrefersDarkMode")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("ProfileImageId")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("IdentityUserId");

                    b.HasIndex("ProfileImageId");

                    b.ToTable("DbUsers");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClaimType")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("TEXT");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("TEXT");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClaimType")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderKey")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleId")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Devices.Models.LightDevice", b =>
                {
                    b.HasBaseType("DigitalAssistant.Server.Modules.Devices.Models.Device");

                    b.Property<double>("Brightness")
                        .HasColumnType("REAL")
                        .HasColumnName("LightDevice_Brightness");

                    b.Property<string>("Color")
                        .HasColumnType("TEXT")
                        .HasColumnName("LightDevice_Color");

                    b.Property<bool>("ColorIsAdjustable")
                        .HasColumnType("INTEGER")
                        .HasColumnName("LightDevice_ColorIsAdjustable");

                    b.Property<int?>("ColorTemperature")
                        .HasColumnType("INTEGER")
                        .HasColumnName("LightDevice_ColorTemperature");

                    b.Property<bool>("ColorTemperatureIsAdjustable")
                        .HasColumnType("INTEGER")
                        .HasColumnName("LightDevice_ColorTemperatureIsAdjustable");

                    b.Property<bool>("IsDimmable")
                        .HasColumnType("INTEGER")
                        .HasColumnName("LightDevice_IsDimmable");

                    b.Property<int>("MaximumColorTemperature")
                        .HasColumnType("INTEGER")
                        .HasColumnName("LightDevice_MaximumColorTemperature");

                    b.Property<int>("MinimumColorTemperature")
                        .HasColumnType("INTEGER")
                        .HasColumnName("LightDevice_MinimumColorTemperature");

                    b.Property<bool>("On")
                        .HasColumnType("INTEGER")
                        .HasColumnName("LightDevice_On");

                    b.HasDiscriminator().HasValue("LightDevice");
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Devices.Models.SwitchDevice", b =>
                {
                    b.HasBaseType("DigitalAssistant.Server.Modules.Devices.Models.Device");

                    b.Property<bool>("On")
                        .HasColumnType("INTEGER")
                        .HasColumnName("SwitchDevice_On");

                    b.HasDiscriminator().HasValue("SwitchDevice");
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Clients.Models.Client", b =>
                {
                    b.HasOne("DigitalAssistant.Server.Modules.Groups.Models.Group", "Group")
                        .WithMany("Clients")
                        .HasForeignKey("GroupId");

                    b.Navigation("Group");
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Devices.Models.Device", b =>
                {
                    b.HasOne("DigitalAssistant.Server.Modules.Groups.Models.Group", "Group")
                        .WithMany("Devices")
                        .HasForeignKey("GroupId");

                    b.Navigation("Group");
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Groups.Models.Group", b =>
                {
                    b.HasOne("DigitalAssistant.Server.Modules.Files.ServerFile", "Icon")
                        .WithMany()
                        .HasForeignKey("IconId");

                    b.Navigation("Icon");
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Users.User", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", "IdentityUser")
                        .WithMany()
                        .HasForeignKey("IdentityUserId");

                    b.HasOne("DigitalAssistant.Server.Modules.Files.ServerFile", "ProfileImage")
                        .WithMany()
                        .HasForeignKey("ProfileImageId");

                    b.Navigation("IdentityUser");

                    b.Navigation("ProfileImage");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("DigitalAssistant.Server.Modules.Groups.Models.Group", b =>
                {
                    b.Navigation("Clients");

                    b.Navigation("Devices");
                });
#pragma warning restore 612, 618
        }
    }
}
