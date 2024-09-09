using DigitalAssistant.Base;
using DigitalAssistant.Base.Audio;
using DigitalAssistant.Base.General;
using DigitalAssistant.Abstractions.Services;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using DigitalAssistant.Client.Modules.Audio.Linux;
using DigitalAssistant.Client.Modules.Audio.Windows;
using DigitalAssistant.Client.Modules.ServerConnection.Services;
using DigitalAssistant.Client.Modules.SpeechRecognition.Services;
using DigitalAssistant.Client.Modules.State;
using Microsoft.AspNetCore.DataProtection;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();
if (OperatingSystem.IsWindows())
    builder.Logging.AddEventLog(config =>
    {
        if (OperatingSystem.IsWindows())
            config.SourceName = "DigitalAssistant.Client";
    });


#if DEBUG
builder.Configuration.SetBasePath(AppContext.BaseDirectory);
builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json");
builder.Configuration.AddJsonFile("appsettings.User.json");
#endif

builder.Services
    .AddDataProtection()
    .SetDefaultKeyLifetime(TimeSpan.FromDays(365 * 100))
    .SetApplicationName("DigitalAssistant.Client");

builder.Services
    .AddSingleton<BaseErrorService>()
    .AddSingleton((serviceProvider) =>
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        return config.GetRequiredSection("ClientSettings").Get<ClientSettings>() ?? new();
    })
    .AddSingleton<ClientState>()
    .AddSingleton<AudioService>()
    .AddSingleton<ServerConnectionService>()
    .AddSingleton<ServerTaskExecutionService>()
    .AddSingleton<WakeWordListener>()
    .AddSingleton<IDataProtectionService, DataProtectionService>()

    .AddTransient<TcpMessageHandler>()
    ;

if (OperatingSystem.IsWindows())
{
    builder.Services
        .AddSingleton<IAudioPlayer, WindowsAudioPlayer>()
        .AddSingleton<IAudioRecorder, WindowsAudioRecorder>()
        .AddSingleton<IAudioDeviceService, WindowsAudioDeviceService>()
        ;
}
else if (OperatingSystem.IsLinux())
{
    builder.Services
        .AddSingleton<IAudioPlayer, LinuxAudioPlayer>()
        .AddSingleton<IAudioRecorder, LinuxAudioRecorder>()
        .AddSingleton<IAudioDeviceService, LinuxAudioDeviceService>()
        ;
}

builder.Services
    .AddHostedService<ServerConnectionHandler>()
    .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<WakeWordListener>())
    .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ServerTaskExecutionService>())
    ;

var host = builder.Build();
host.Run();
