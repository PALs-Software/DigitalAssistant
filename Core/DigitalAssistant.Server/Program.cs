using BlazorBase;
using BlazorBase.AudioRecorder;
using BlazorBase.Backup;
using BlazorBase.CRUD;
using BlazorBase.CRUD.ModelServiceProviderInjection;
using BlazorBase.Files;
using BlazorBase.Server;
using BlazorBase.Server.Services;
using BlazorBase.User;
using BlazorBase.User.Models;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Services;
using DigitalAssistant.Base.Audio;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Server.Data;
using DigitalAssistant.Server.Modules.Ai.Asr.Services;
using DigitalAssistant.Server.Modules.Ai.TextToSpeech;
using DigitalAssistant.Server.Modules.Ai.TextToSpeech.Enums;
using DigitalAssistant.Server.Modules.AudioPlayer;
using DigitalAssistant.Server.Modules.BackgroundJobs;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Clients.Components;
using DigitalAssistant.Server.Modules.Clients.Services;
using DigitalAssistant.Server.Modules.Commands.Services;
using DigitalAssistant.Server.Modules.Connectors.Services;
using DigitalAssistant.Server.Modules.Devices.Services;
using DigitalAssistant.Server.Modules.Files;
using DigitalAssistant.Server.Modules.General;
using DigitalAssistant.Server.Modules.Localization;
using DigitalAssistant.Server.Modules.MainComponents;
using DigitalAssistant.Server.Modules.MessageHandling.Components;
using DigitalAssistant.Server.Modules.Users;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json.Serialization;
using TextToSpeech;

var webApplicationBuilder = WebApplication.CreateBuilder(args);

#if DEBUG
webApplicationBuilder.Configuration.AddJsonFile("appsettings.User.json");
#endif

var databaseProvider = webApplicationBuilder.Configuration["DatabaseProvider"];
var connectionString = webApplicationBuilder.Configuration.GetConnectionString("DefaultConnection");

#if CREATESQLITEMIGRATIONS
//Add-Migration -Name Init -Context SQLiteDbContext -OutputDir Data\Migrations\SQLiteMigrations
databaseProvider = "SQLite";
connectionString = "Data Source=DigitalAssistant_DEV.db";
#elif CREATEMSSQLMIGRATIONS
//Add-Migration -Name Init -Context MSSQLDbContext -OutputDir Data\Migrations\MSSQLMigrations
databaseProvider = "MSSQL";
connectionString = "Server=localhost;Database=DigitalAssistantServer_DEV;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
#endif

switch (databaseProvider)
{
    case "SQLite":
        ConfigureDatabase<SQLiteDbContext>(webApplicationBuilder, databaseProvider, connectionString);
        ConfigureIdentity<SQLiteDbContext>(webApplicationBuilder);
        ConfigureBlazorBase<SQLiteDbContext>(webApplicationBuilder);
        break;
    case "MSSQL":
        ConfigureDatabase<MSSQLDbContext>(webApplicationBuilder, databaseProvider, connectionString);
        ConfigureIdentity<MSSQLDbContext>(webApplicationBuilder);
        ConfigureBlazorBase<MSSQLDbContext>(webApplicationBuilder);
        break;
    default:
        throw new NotImplementedException($"The database provider '{databaseProvider}' is currently not supported. Choose one of the following providers: SQLite, MSSQL");
}

ConfigureAspDotNetBasics(webApplicationBuilder);
ConfigureLogging(webApplicationBuilder);
ConfigureServices(webApplicationBuilder);

var webApplication = webApplicationBuilder.Build();
ConfigureApp(webApplication);
ConfigureBlazorBaseAppSettings(webApplication);

switch (databaseProvider)
{
    case "SQLite":
        await OnStartupAsync<SQLiteDbContext>(webApplication);
        break;
    case "MSSQL":
        await OnStartupAsync<MSSQLDbContext>(webApplication);
        break;
    default:
        throw new NotImplementedException($"The database provider '{databaseProvider}' is currently not supported. Choose one of the following providers: SQLite, MSSQL");
}

await webApplication.RunAsync();

void ConfigureDatabase<TDatabaseContext>(WebApplicationBuilder builder, string databaseProvider, string? connectionString) where TDatabaseContext : ApplicationDbContext
{
    var serviceProviderInterceptor = new ServiceProviderInterceptor();

    builder.Services.AddDbContext<TDatabaseContext>(options =>
    {
        options.UseLazyLoadingProxies();
        switch (databaseProvider)
        {
            case "SQLite":
                options.UseSqlite(connectionString);
                break;
            case "MSSQL":
                options.UseSqlServer(connectionString);
                break;
            default:
                throw new NotImplementedException($"The database provider '{databaseProvider}' is currently not supported. Choose one of the following providers: SQLite, MSSQL");
        }

        options.AddInterceptors(serviceProviderInterceptor);
#if DEBUG
        options.LogTo((input) => Debug.WriteLine(input), LogLevel.Information);
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
#endif
    }, contextLifetime: ServiceLifetime.Transient, optionsLifetime: ServiceLifetime.Transient);

#if DEBUG
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
#endif
}

void ConfigureIdentity<TDatabaseContext>(WebApplicationBuilder builder) where TDatabaseContext : ApplicationDbContext
{
    builder.Services
        .AddDefaultIdentity<IdentityUser>(options =>
        {
            options.User.AllowedUserNameCharacters = " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 10;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<TDatabaseContext>();
}

void ConfigureAspDotNetBasics(WebApplicationBuilder builder)
{
    builder.Services.AddRazorPages();

    builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

    builder.Services.AddCascadingAuthenticationState();
}

void ConfigureBlazorBase<TDatabaseContext>(WebApplicationBuilder builder) where TDatabaseContext : ApplicationDbContext
{
    builder.Services.AddBlazorise(options => { options.ChangeTextOnKeyPress = true; })
            .AddBootstrapProviders()
            .AddFontAwesomeIcons()

            .AddBlazorBase(options =>
            {
                options.WebsiteName = "Digital Assistant";
                options.ShortWebsiteName = "DA";
            })
            .AddBlazorBaseBackup(UserRole.Admin.ToString())
            .AddBlazorBaseCRUD<TDatabaseContext>(options =>
            {
                options.UseAsyncDbContextMethodsPerDefaultInBaseDbContext = false;
            })
            .AddBlazorBaseServer()

            .AddBlazorBaseUserManagement<UserService, User, IdentityUser, UserRole, BlazorBaseUserOptions>(options =>
            {
                options.WebsiteName = "Digital Assistant";
                options.SkipLoadingMailServices = true;
            })

            .AddBlazorBaseFiles<ServerFile>(options =>
            {
                options.FileStorePath = builder.Configuration["FileStorePath"]!;
                options.TempFileStorePath = builder.Configuration["TempFileStorePath"]!;
            },
                allowedUserAccessRoles: [UserRole.Admin.ToString(), UserRole.User.ToString()]
            )

            .AddBlazorBaseAudioRecorderWithoutCRUDSupport()
    ;
}

void ConfigureApp(WebApplication app)
{
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
     .AddInteractiveServerRenderMode();

    app.MapControllers();
}

void ConfigureBlazorBaseAppSettings(WebApplication app)
{
    app.UseRequestLocalization(
        new RequestLocalizationOptions()
             .SetDefaultCulture("en-US")
             .AddSupportedCultures(["en-US", "de-DE"])
             .AddSupportedUICultures(["en-US", "de-DE"])
    );

    app.RegisterBlazorBaseFileOptionsInstance();
    app.RemoveDefaultIdentityControllerEndpoints();
}

void ConfigureLogging(WebApplicationBuilder builder)
{
    builder.Logging.AddConsole();

    if (OperatingSystem.IsWindows())
    {
        builder.Logging.AddEventLog(eventLogSettings =>
        {
            if (OperatingSystem.IsWindows())
                eventLogSettings.SourceName = "DigitalAssistantServer";
        });
    }
}

void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = true; // will be handled by ApiResponseActionHandlingAttribute instead
        });

    builder.Services.AddTextToSpeechService(
        new TextToSpeechConfiguration()
        {
            ModelBaseDirectoryPath = Path.Join(builder.Configuration["ModelsDirectoryPath"], "TtsModels")
        }
    );

    builder.Services
        .AddDataProtection()
        .SetDefaultKeyLifetime(TimeSpan.FromDays(365 * 100))
        .SetApplicationName("DigitalAssistant.Server");

    builder.Services
        .AddSingleton<DigitalAssistant.Base.General.BaseErrorService>()
        .AddSingleton<AudioService>()
        .AddSingleton<AsrService>()
        .AddSingleton<AsrModelSelectionService>()
        .AddSingleton<TtsModelSelectionService>()
        .AddSingleton<CommandTemplateParser>()
        .AddSingleton<ClientInformationService>()
        .AddSingleton<ClientTaskExecutionService>()
        .AddSingleton<ClientConnectionHandler>()
        .AddSingleton<ConnectorService>()
        .AddSingleton<CommandHandler>()
        .AddSingleton<IDeviceFactory, DeviceFactory>()
        .AddSingleton<IDeviceChangeArgsFactory, DeviceChangeArgsFactory>()
        .AddSingleton<IDataProtectionService, DataProtectionService>()

        .AddScoped<ScopedEventService>()
        .AddScoped<CircuitHandler, ExtendedCircuitHandlerService>()
        .AddScoped<WebAudioPlayer>()
        .AddScoped<FileDownloadService>()

        .AddTransient(typeof(JsonStringLocalizer<>))
        .AddTransient<TcpMessageHandler>()
        .AddTransient<CommandParameterParser>()
        .AddTransient<CommandProcessor>()
        .AddTransient<ClientCommandService>()
        .AddTransient<ClientStatusPopup>()
        ;

    builder.Services
        .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ClientConnectionHandler>())
        .AddHostedService<ClientDiscoveryService>()
        .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ClientTaskExecutionService>())
        .AddHostedService<DiscoverAndUpdateDevices>()
        ;
}

async Task OnStartupAsync<TDatabaseContext>(WebApplication app) where TDatabaseContext : ApplicationDbContext
{
    DatabaseMigrationService.MigrateDatabase<TDatabaseContext>(app);

    using var scope = app.Services.CreateAsyncScope();

    await DatabaseSeeder.SeedDataAsync(scope.ServiceProvider);
    await Cache.SetupCache.RefreshSetupCacheAsync(scope.ServiceProvider);
    await Cache.UserCache.InitUserCacheAsync(scope.ServiceProvider);
    await Cache.ClientCache.InitClientCacheAsync(scope.ServiceProvider);

    var textToSpeechConfiguration = scope.ServiceProvider.GetRequiredService<TextToSpeechConfiguration>();
    if (Cache.SetupCache.Setup != null)
    {
        textToSpeechConfiguration.Model = Cache.SetupCache.Setup.GetCombinedTtsModelName();
        textToSpeechConfiguration.UseGpu = Cache.SetupCache.Setup.TtsMode == TtsMode.Gpu;
        textToSpeechConfiguration.PreventLoadingAiModels = app.Configuration.GetValue<bool>("PreventLoadingAiModels");
    }

    var connectorService = scope.ServiceProvider.GetRequiredService<ConnectorService>();
    await connectorService.LoadConnectorsAsync();

    var commandHandler = scope.ServiceProvider.GetRequiredService<CommandHandler>();
    commandHandler.LoadCommands();
    if (Cache.SetupCache.Setup != null)
        await commandHandler.RefreshLocalizedCommandTemplatesCacheAsync();
}