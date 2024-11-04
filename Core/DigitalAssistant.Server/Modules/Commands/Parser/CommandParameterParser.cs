using BlazorBase.Abstractions.CRUD.Extensions;
using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.CRUD.Services;
using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Commands.Models;
using DigitalAssistant.Server.Modules.Devices.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DigitalAssistant.Server.Modules.Commands.Parser;

public class CommandParameterParser
{
    #region Injects
    protected readonly BaseParser BaseParser;
    protected readonly IBaseDbContext DbContext;
    protected readonly IStringLocalizer<CommandParameterParser> Localizer;
    protected readonly IStringLocalizer<CommandTemplateParser> TemplateParserLocalizer;
    #endregion

    #region Members
    protected List<string> ColorNames = [];
    protected List<string> ColorTemperatureNames = [];
    #endregion

    public CommandParameterParser(BaseParser baseParser, IBaseDbContext dbContext, IStringLocalizer<CommandParameterParser> localizer, IStringLocalizer<CommandTemplateParser> templateParserLocalizer)
    {
        BaseParser = baseParser;
        DbContext = dbContext;
        Localizer = localizer;
        TemplateParserLocalizer = templateParserLocalizer;

        InitializeNameLists();
    }

    protected void InitializeNameLists()
    {
        var currentUICulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        ColorNames = TemplateParserLocalizer["ColorRegexTemplate"].ToString().Split("|").ToList();
        for (int i = 0; i < ColorNames.Count; i++)
            ColorNames[i] = ColorNames[i].Replace(" ", string.Empty).Trim();

        ColorTemperatureNames = TemplateParserLocalizer["ColorTemperatureRegexTemplate"].ToString().Split("|").ToList();
        for (int i = 0; i < ColorTemperatureNames.Count; i++)
            ColorTemperatureNames[i] = ColorTemperatureNames[i].Replace(" ", string.Empty).Trim();

        CultureInfo.CurrentUICulture = currentUICulture;
    }

    public async Task<(bool Success, ICommandParameters? CommandParameters)> ParseParametersFromMatchAsync(ICommandTemplate template, Match match, string language, IClient client, InterpreterMode interpreterMode)
    {
        var currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

            var parameterDictionary = new Dictionary<string, (ICommandParameter Parameter, object? Value)>();
            foreach (var parameter in template.Parameters)
            {
                if (!match.Groups.ContainsKey(parameter.Key))
                    if (parameter.Value.IsOptional || parameter.Value.AlternativeParameters.Count > 0)
                    {
                        parameterDictionary.Add(parameter.Key, (parameter.Value, null));
                        continue;
                    }
                    else
                        return (false, null);

                var groupValue = match.Groups[parameter.Key].Value;
                (bool success, object? convertedValue) = await ConvertParameterValueAsync(groupValue.Trim(), parameter.Value);
                if (!success)
                    if (parameter.Value.IsOptional || parameter.Value.AlternativeParameters.Count > 0)
                    {
                        parameterDictionary.Add(parameter.Key, (parameter.Value, null));
                        continue;
                    }
                    else
                        return (false, null);

                parameterDictionary.Add(parameter.Key, (parameter.Value, convertedValue));
            }

            var alternativeParameters = template.Parameters.Values.Where(entry => entry.AlternativeParameters.Count > 0);
            foreach (var alternativeParameter in alternativeParameters)
            {
                if (parameterDictionary[alternativeParameter.Name].Value != null)
                    continue;

                bool oneAlternativeHasValue = false;
                foreach (var alternative in alternativeParameter.AlternativeParameters)
                    if (parameterDictionary[alternative.Name].Value != null)
                        oneAlternativeHasValue = true;

                if (!oneAlternativeHasValue)
                    return (false, null);
            }

            return (true, new CommandParameters(client, language, interpreterMode, parameterDictionary.AsReadOnly()));
        }
        finally
        {
            CultureInfo.CurrentUICulture = currentUICulture;
        }
    }

    public async Task<(bool Success, ICommandParameters? CommandParameters)> ParseParametersAsync(Dictionary<ICommandParameter, string> parameters, string language, IClient client, InterpreterMode interpreterMode)
    {
        var currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            var parameterDictionary = new Dictionary<string, (ICommandParameter Parameter, object? Value)>();
            foreach (var parameter in parameters)
            {
                (bool success, object? convertedValue) = await ConvertParameterValueAsync(parameter.Value, parameter.Key);
                if (success)
                    parameterDictionary.Add(parameter.Key.Name, (parameter.Key, convertedValue));
            }

            return (true, new CommandParameters(client, language, interpreterMode, parameterDictionary.AsReadOnly()));
        }
        finally
        {
            CultureInfo.CurrentUICulture = currentUICulture;
        }
    }

    protected async Task<(bool Success, object? ConvertedValue)> ConvertParameterValueAsync(string value, ICommandParameter parameter)
    {
        bool success = false;
        object? convertedValue = null;
        switch (parameter.Type)
        {
            case CommandParameterType.Boolean:
                var extendedBooleanTrueValues = Localizer["ExtendedBooleanTrueValues"].ToString().Split(",");
                var extendedBooleanFalseValues = Localizer["ExtendedBooleanFalseValues"].ToString().Split(",");
                var normalizedValue = value.Trim().ToLower();
                if (extendedBooleanTrueValues.Contains(normalizedValue))
                {
                    convertedValue = true;
                    success = true;
                }
                else if (extendedBooleanFalseValues.Contains(normalizedValue))
                {
                    convertedValue = false;
                    success = true;
                }
                else
                    success = BaseParser.TryParseValueFromString<bool>(value, out convertedValue, out string? _);
                break;
            case CommandParameterType.Text:
                success = true;
                convertedValue = value;
                break;
            case CommandParameterType.Integer:
                success = BaseParser.TryParseValueFromString<int>(value, out convertedValue, out string? _);
                break;
            case CommandParameterType.Decimal:
                success = BaseParser.TryParseValueFromString<decimal>(value, out convertedValue, out string? _);
                break;
            case CommandParameterType.Date:
                success = BaseParser.TryParseValueFromString<DateTime>(value, out convertedValue, out string? _);
                break;
            case CommandParameterType.Time:
                success = BaseParser.TryParseValueFromString<DateTime>(value, out convertedValue, out string? _);
                if (success)
                    convertedValue = ((DateTime)convertedValue!).TimeOfDay;
                break;
            case CommandParameterType.Color:
                var normalizedColorValue = value.Trim().ToLower();
                var localizedColorNames = TemplateParserLocalizer["ColorRegexTemplate"].ToString().Split("|").ToList();
                var localizedColorName = localizedColorNames.Where(entry => entry.ToLower() == normalizedColorValue).FirstOrDefault();
                if (localizedColorName == null)
                    break;

                var index = localizedColorNames.IndexOf(localizedColorName);
                if (index == -1)
                    break;

                var colorName = ColorNames.ElementAt(index);
                var color = Color.FromName(colorName);
                success = color.IsKnownColor;
                if (success)
                    convertedValue = color;
                break;
            case CommandParameterType.ColorTemperatureColor:
                var normalizedColorTemperatureValue = value.Trim().ToLower();
                var localizedColorTemperatureNames = TemplateParserLocalizer["ColorTemperatureRegexTemplate"].ToString().Split("|").ToList();
                var localizedColorTemperatureName = localizedColorTemperatureNames.Where(entry => entry.ToLower() == normalizedColorTemperatureValue).FirstOrDefault();
                if (localizedColorTemperatureName == null)
                    break;

                var temperatureIndex = localizedColorTemperatureNames.IndexOf(localizedColorTemperatureName);
                if (temperatureIndex == -1)
                    break;

                var colorTemperatureName = ColorTemperatureNames.ElementAt(temperatureIndex);
                success = Enum.TryParse(colorTemperatureName, out ColorTemperatureColor colorTemperature);
                if (success)
                    convertedValue = colorTemperature;
                break;
            case CommandParameterType.Option:
                var commandOption = parameter.AsOptionParameter().Option;
                var commandValue = commandOption.Values.FirstOrDefault(entry => entry.LocalizedValues.Contains(value, StringComparer.OrdinalIgnoreCase));
                if (commandValue != null)
                {
                    success = true;
                    convertedValue = commandValue.Name;
                }
                break;
            case CommandParameterType.Group:
                var group = await DbContext.SetAsync((IQueryable<Groups.Models.Group> query) =>
                {
                    return query.Where(entry => entry.Name.ToLower() == value.ToLower() ||
                                       entry.AlternativeNames.Any(alternativeName => alternativeName.ToLower() == value.ToLower()))
                                .AsNoTracking()
                                .Include(entry => entry.Devices)
                                .Include(entry => entry.Clients)
                                .FirstOrDefault();
                });

                if (group != null)
                {
                    success = true;
                    convertedValue = group;
                }
                break;
            case CommandParameterType.Client:
                var client = await DbContext.FirstOrDefaultAsync<Client>(entry => entry.Name.ToLower() == value.ToLower(),
                                                                        asNoTracking: true);
                if (client != null)
                {
                    success = true;
                    convertedValue = new ClientBase(client);
                }
                break;
            case CommandParameterType.Device:
                var device = await DbContext.FirstOrDefaultAsync<Device>(entry => entry.Name.ToLower() == value.ToLower() ||
                                                                         entry.AlternativeNames.Any(alternativeName => alternativeName.ToLower() == value.ToLower()),
                                                                         asNoTracking: true);
                if (device != null)
                {
                    success = true;
                    convertedValue = device;
                }
                break;
            case CommandParameterType.LightDevice:
                var lightDevice = await DbContext.FirstOrDefaultAsync<LightDevice>(entry => entry.Name.ToLower() == value.ToLower() ||
                                                                                   entry.AlternativeNames.Any(alternativeName => alternativeName.ToLower() == value.ToLower()),
                                                                                   asNoTracking: true);
                if (lightDevice != null)
                {
                    success = true;
                    convertedValue = lightDevice;
                }
                break;
            case CommandParameterType.SwitchDevice:
                var switchDevice = await DbContext.FirstOrDefaultAsync<SwitchDevice>(entry => entry.Name.ToLower() == value.ToLower() ||
                                                                                     entry.AlternativeNames.Any(alternativeName => alternativeName.ToLower() == value.ToLower()),
                                                                                     asNoTracking: true);
                if (switchDevice != null)
                {
                    success = true;
                    convertedValue = switchDevice;
                }
                break;
            default:
                break;
        }

        return (success, convertedValue);
    }
}
