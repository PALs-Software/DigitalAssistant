using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Localization;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Commands.Interpreter;
using DigitalAssistant.Server.Modules.Commands.Parser;
using DigitalAssistant.Server.Modules.Commands.SystemCommands;
using DigitalAssistant.Server.Modules.Connectors.Services;
using DigitalAssistant.Server.Modules.Devices.Models;
using DigitalAssistant.Server.Modules.Localization;
using DigitalAssistant.Server.Modules.Plugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace DigitalAssistant.Server.Modules.Commands.Services;

public class CommandHandler(IServiceProvider serviceProvider,
    CommandTemplateParser commandTemplateParser,
    ConnectorService connectorService,
    ILogger<CommandHandler> logger)
{
    #region Injects
    protected readonly IServiceProvider ServiceProvider = serviceProvider;
    protected readonly ConnectorService ConnectorService = connectorService;
    protected readonly CommandTemplateParser CommandTemplateParser = commandTemplateParser;  
    protected readonly ILogger<CommandHandler> Logger = logger;
    #endregion

    #region Members
    protected List<ICommand> Commands = [];
    protected ConcurrentDictionary<string, List<List<ICommandTemplate>>> LocalizedCommandTemplates = [];
    protected ReaderWriterLockSlim CommandCacheLock = new();
    #endregion

    public ReadOnlyCollection<ICommand> GetCommands()
    {
        return Commands.AsReadOnly();
    }

    public async Task<List<List<ICommandTemplate>>> GetLocalizedCommandTemplatesAsync(string language)
    {
        if (!LocalizedCommandTemplates.TryGetValue(language, out var templates))
        {
            await RefreshLocalizedCommandTemplatesCacheAsync(language: language);
            CommandCacheLock.EnterReadLock();
            try
            {
                return LocalizedCommandTemplates[language];
            }
            finally
            {
                CommandCacheLock.ExitReadLock();
            }
        }

        return templates;
    }

    public void LoadCommands()
    {
        var commandsFolderPath = Path.Combine(AppContext.BaseDirectory, "Commands");
        var commandsPathes = Directory.GetFiles(commandsFolderPath, "*Commands.dll", SearchOption.AllDirectories);
        foreach (var commandPath in commandsPathes)
        {
            var commandAssembly = LoadCommand(commandPath);
            foreach (var command in CreateCommands(commandAssembly))
                Commands.Add(command);
        }

        Commands.Add(CreateCommand(typeof(StopCommand))!);
        Commands.Add(CreateCommand(typeof(PauseCommand))!);
        Commands.Add(CreateCommand(typeof(ContinueCommand))!);
        Commands.Add(CreateCommand(typeof(IncreaseVolumeCommand))!);
        Commands.Add(CreateCommand(typeof(DecreaseVolumeCommand))!);
        Commands.Add(CreateCommand(typeof(SetVolumeCommand))!);
    }

    protected Assembly LoadCommand(string path)
    {
        var assemblyName = AssemblyName.GetAssemblyName(path);
        Logger.LogInformation("Loading command '{AssemblyName}'", assemblyName);

        var loadContext = new PluginLoadContext(path);
        return loadContext.LoadFromAssemblyName(assemblyName);
    }

    protected List<ICommand> CreateCommands(Assembly assembly)
    {
        var commands = new List<ICommand>();
        var commandTypes = assembly.GetTypes().Where(type => typeof(ICommand).IsAssignableFrom(type));
        foreach (var commandType in commandTypes)
        {
            var command = CreateCommand(commandType);
            if (command != null)
                commands.Add(command);
        }

        return commands;
    }

    protected ICommand? CreateCommand(Type commandType)
    {
        var localizer = (IStringLocalizer)ServiceProvider.GetRequiredService(typeof(IStringLocalizer<>).MakeGenericType(commandType));
        var jsonLocalizer = (IJsonStringLocalizer)ServiceProvider.GetRequiredService(typeof(JsonStringLocalizer<>).MakeGenericType(commandType));
        var command = Activator.CreateInstance(commandType, localizer, jsonLocalizer) as ICommand;

        return command;
    }

    public async Task RefreshLocalizedCommandTemplatesCacheAsync(bool clearAllLanguages = false, string? language = null)
    {
        CultureInfo cultureInfo;
        if (language == null)
            cultureInfo = CultureInfo.GetCultureInfo((int?)Cache.SetupCache.Setup?.InterpreterLanguage ?? 9);
        else
            cultureInfo = CultureInfo.GetCultureInfo(language);

        var dbContext = ServiceProvider.GetRequiredService<IBaseDbContext>();
        var clients = await dbContext.SetAsync((IQueryable<Client> query) =>
        {
            return query.AsNoTracking()
                        .Select(entry => entry.Name)
                        .ToList();
        });
        var devices = await dbContext.SetAsync((IQueryable<Device> query) =>
        {
            return query.AsNoTracking()
                        .Select(entry => new Tuple<string, List<string>, DeviceType>(entry.Name, entry.AlternativeNames, entry.Type).ToValueTuple())
                        .ToList();
        });
        CommandTemplateParser.SetTemplateNames(clients, devices);
        ServiceProvider.GetRequiredService<CommandLlmInterpreter>().SetTemplateNames(clients, devices);

        try
        {
            CommandCacheLock.EnterWriteLock();

            if (clearAllLanguages)
                LocalizedCommandTemplates.Clear();
            else
                LocalizedCommandTemplates.Remove(cultureInfo.Name, out _);

            ConcurrentBag<List<ICommandTemplate>> commandTemplates = [];
            Parallel.ForEach(Commands, (command, parallelLoopState) =>
            {
                var currentUICulture = CultureInfo.CurrentUICulture;
                CultureInfo.CurrentUICulture = cultureInfo;
                var templatesPerCommand = new List<ICommandTemplate>();
                foreach (var template in command.GetTemplates())
                    templatesPerCommand.Add(CommandTemplateParser.ParseTemplate(command, template, cultureInfo.Name));

                commandTemplates.Add(templatesPerCommand);
                CultureInfo.CurrentUICulture = currentUICulture;
            });

            LocalizedCommandTemplates.TryAdd(cultureInfo.Name, commandTemplates.ToList());
        }
        finally
        {
            CommandCacheLock.ExitWriteLock();
        }
    }
}
