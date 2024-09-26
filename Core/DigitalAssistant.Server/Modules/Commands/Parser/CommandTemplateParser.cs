using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Base.General;
using DigitalAssistant.Server.Modules.Commands.Exceptions;
using DigitalAssistant.Server.Modules.Commands.Models;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DigitalAssistant.Server.Modules.Commands.Parser;

public class CommandTemplateParser
{
    #region Injects
    protected readonly IStringLocalizer<CommandTemplateParser> Localizer;
    protected readonly IStringLocalizer<CommandParameterParser> ParameterLocalizer;
    #endregion

    #region Member
    protected Dictionary<char, char> SectionBrackets = new() { { '(', ')' }, { '[', ']' }, { '{', '}' } };
    protected string AreaNames = string.Empty;
    protected string ClientNames = string.Empty;
    protected string DeviceNames = string.Empty;
    protected string LightDeviceNames = string.Empty;
    protected string SwitchDeviceNames = string.Empty;
    protected JsonSerializerOptions JsonSerializerOptions = new();
    #endregion

    public CommandTemplateParser(IStringLocalizer<CommandTemplateParser> localizer, IStringLocalizer<CommandParameterParser> parameterLocalizer)
    {
        Localizer = localizer;
        ParameterLocalizer = parameterLocalizer;

        JsonSerializerOptions.Converters.Add(new JsonTypeMappingConverter<ICommandOption, CommandOption>());
        JsonSerializerOptions.Converters.Add(new JsonTypeMappingConverter<ICommandOptionValue, CommandOptionValue>());
    }

    public void SetTemplateNames(List<string> clients, List<(string Name, List<string> AlternativeNames, DeviceType Type)> devices)
    {
        List<(string CombinedNames, DeviceType Type)> names = [];
        foreach (var device in devices)
        {
            var combinedName = string.Join('|', new List<string>() { device.Name }.Concat(device.AlternativeNames));
            names.Add((combinedName, device.Type));
        }

        ClientNames = string.Join('|', clients);
        DeviceNames = string.Join('|', names.Select(entry => entry.CombinedNames));
        LightDeviceNames = string.Join('|', names.Where(entry => entry.Type == DeviceType.Light).Select(entry => entry.CombinedNames));
        SwitchDeviceNames = string.Join('|', names.Where(entry => entry.Type == DeviceType.Switch).Select(entry => entry.CombinedNames));
    }

    public ICommandTemplate ParseTemplate(ICommand command, string template, string language)
    {
        ArgumentException.ThrowIfNullOrEmpty(template);

        int index = 0;
        var parameters = new Dictionary<string, ICommandParameter>();
        List<ICommandOption> commandOptions = [];
        var jsonOptions = command.GetOptionsJson();
        if (!string.IsNullOrEmpty(jsonOptions) && jsonOptions != "Options")
            commandOptions = JsonSerializer.Deserialize<List<ICommandOption>>(jsonOptions, JsonSerializerOptions) ?? [];

        var regexTemplate = ParseTemplatePart(template, ref index, ref parameters, commandOptions, isInOptionalSection: false);
        var regex = new Regex(regexTemplate, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        return new CommandTemplate(command, language, template, regex, parameters);
    }

    protected string ParseTemplatePart(string template, ref int index, ref Dictionary<string, ICommandParameter> parameters, List<ICommandOption> commandOptions, bool isInOptionalSection, char? parentSectionClosingCharacter = null)
    {
        string regexTemplate = string.Empty;
        int lastIndex = 0;
        do
        {
            lastIndex = index;
            index = template.IndexOfAny(['(', '[', '{'], startIndex: index);

            if (index == -1 && parentSectionClosingCharacter == null)
                regexTemplate += template.Substring(lastIndex, template.Length - lastIndex);
            else if (index != -1)
            {
                // Check if the current section is a subsection.
                if (parentSectionClosingCharacter != null)
                {
                    var nextTemplateClosingCharacterIndex = template.IndexOf(parentSectionClosingCharacter.Value, startIndex: lastIndex);

                    // If in a subsection, check if the closing character is before one of the next sections.
                    // If it is, then current section is parsed and the next section should be parsed in the parent level.
                    if (index > nextTemplateClosingCharacterIndex)
                    {
                        index = lastIndex;
                        return regexTemplate;
                    }
                }

                // Add the plain text section from the last section to the next one as plain text to the regex template.
                regexTemplate += template.Substring(lastIndex, index - lastIndex);

                switch (template[index])
                {
                    case '(':
                        ParseOptionalTextSection(template, ref regexTemplate, ref index, ref parameters, commandOptions, isInOptionalSection);
                        break;
                    case '[':
                        ParseAlternativeTextSection(template, ref regexTemplate, ref index, ref parameters, commandOptions, isInOptionalSection);
                        break;
                    case '{':
                        ParseParameterSection(template, ref regexTemplate, ref index, ref parameters, commandOptions, isInOptionalSection);
                        break;
                }


            }
        } while (index != -1);

        index = lastIndex;
        return regexTemplate;
    }

    protected void ParseSubSectionIfExists(string template, ref string regexTemplate, ref int index, ref int closingIndex, char parentSectionOpeningCharacter, char parentSectionClosingCharacter, bool isInOptionalSection, ref Dictionary<string, ICommandParameter> parameters, List<ICommandOption> commandOptions)
    {
        // Check if the template contains another sub section in the current section.
        if (template.IndexOfAny(['(', '[', '{'], startIndex: index + 1, closingIndex - index) == -1)
            return;

        // Step one forward so the subsections begins after the current opening character.
        index++;
        regexTemplate += ParseTemplatePart(template, ref index, ref parameters, commandOptions, isInOptionalSection, parentSectionClosingCharacter);
        // Step one character backward to stand on the closing character of the last subsection.
        index--;

        // Recheck closing character with new index.
        closingIndex = GetNextClosingCharacter(template, index + 1, parentSectionOpeningCharacter, parentSectionClosingCharacter, false);
    }

    protected int GetNextClosingCharacter(string template, int startIndex, char sectionCharacter, char sectionClosingCharacter, bool checkForEmptySection)
    {
        var closingIndex = template.IndexOf(sectionClosingCharacter, startIndex);
        if (closingIndex == -1)
            throw new TemplateNotValidException($"Template is not valid, one sub section started with a '{sectionCharacter}' has no closing character '{sectionClosingCharacter}'");

        if (checkForEmptySection && startIndex + 1 == closingIndex)
            throw new TemplateNotValidException($"Template is not valid, empty sections are not allowed");

        return closingIndex;
    }

    protected void ParseOptionalTextSection(string template, ref string regexTemplate, ref int index, ref Dictionary<string, ICommandParameter> parameters, List<ICommandOption> commandOptions, bool isInOptionalSection)
    {
        var closingIndex = GetNextClosingCharacter(template, index, '(', ')', true);

        // Check for whitespace before the optional text and include it when necessary in the optional regex group.
        if (index > 0 && template[index - 1] == ' ')
        {
            regexTemplate = regexTemplate[..^1];
            regexTemplate += "(?: ";
        }
        else
            regexTemplate += "(?:";

        ParseSubSectionIfExists(template, ref regexTemplate, ref index, ref closingIndex, '(', ')', true, ref parameters, commandOptions);

        if (index != closingIndex)
            regexTemplate += template.Substring(index + 1, closingIndex - index - 1);
        regexTemplate += ")?";

        index = closingIndex + 1;
    }

    protected void ParseAlternativeTextSection(string template, ref string regexTemplate, ref int index, ref Dictionary<string, ICommandParameter> parameters, List<ICommandOption> commandOptions, bool isInOptionalSection)
    {
        var openingIndex = index;
        var closingIndex = GetNextClosingCharacter(template, index, '[', ']', true);
        var sectionValue = template.Substring(index + 1, closingIndex - index - 1);
        if (sectionValue.Contains("||"))
            throw new TemplateNotValidException("Template is not valid, empty alternative values are not allowed");

        regexTemplate += "(?:";

        ParseSubSectionIfExists(template, ref regexTemplate, ref index, ref closingIndex, '[', ']', isInOptionalSection, ref parameters, commandOptions);

        if (index != closingIndex)
            regexTemplate += template.Substring(index + 1, closingIndex - index - 1);
        regexTemplate += ")";

        index = closingIndex + 1;

        SetAlternativeParametersIfExists(template.Substring(openingIndex + 1, closingIndex - openingIndex - 1), ref parameters);
    }

    protected void ParseParameterSection(string template, ref string regexTemplate, ref int index, ref Dictionary<string, ICommandParameter> parameters, List<ICommandOption> commandOptions, bool isInOptionalSection)
    {
        var closingIndex = GetNextClosingCharacter(template, index, '{', '}', true);

        // Check if the template contains another sub section in the current section.
        if (template.IndexOfAny(['(', '[', '{'], startIndex: index + 1, closingIndex - index) != -1)
            throw new TemplateNotValidException("Template is not valid, nested sections in a parameter section is not allowed");

        var parameterText = template.Substring(index + 1, closingIndex - index - 1);
        var parameterParts = parameterText.Split(":");
        if (parameterParts.Length < 2)
            throw new TemplateNotValidException($"Template is not valid, a parameter section must contain always a name and a type e.g. {{State:Boolean}}. Invalid Parameter = '{parameterText}'");

        var parameterName = parameterParts[0];
        if (!Enum.TryParse(parameterParts[1], out CommandParameterType parameterType))
            throw new TemplateNotValidException($"Template is not valid, can not parse parameter type '{parameterParts[1]}'. Invalid Parameter = '{parameterText}'");

        if (parameterParts.Length > 2)
            throw new TemplateNotValidException($"Template is not valid, a parameter section must contain always a name and a type e.g. {{State:Boolean}}. Invalid Parameter = '{parameterText}'");

        if (parameters.Keys.Contains(parameterName))
            throw new TemplateNotValidException($"Template is not valid, the parameter name must be unique. The parameter name {parameterName} is used multiple times. Invalid Parameter = '{parameterText}'");

        switch (parameterType)
        {
            case CommandParameterType.Boolean:
                regexTemplate += $"(?'{parameterName}'{Localizer["BooleanRegexTemplate"]})";
                break;
            case CommandParameterType.Text:
                regexTemplate += $"(?'{parameterName}'.+)";
                break;
            case CommandParameterType.Integer:
                regexTemplate += $"(?'{parameterName}'\\d+)";
                break;
            case CommandParameterType.Decimal:
                regexTemplate += $"(?'{parameterName}'\\d+\\{CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator}?\\d*)";
                break;
            case CommandParameterType.Date:
                regexTemplate += $"(?'{parameterName}'{Localizer["DateRegexTemplate"]})";
                break;
            case CommandParameterType.Time:
                regexTemplate += $"(?'{parameterName}'{Localizer["TimeRegexTemplate"]})";
                break;
            case CommandParameterType.Color:
                regexTemplate += $"(?'{parameterName}'{Localizer["ColorRegexTemplate"]})";
                break;
            case CommandParameterType.ColorTemperatureColor:
                regexTemplate += $"(?'{parameterName}'{Localizer["ColorTemperatureRegexTemplate"]})";
                break;
            case CommandParameterType.Option:
                var commandOption = commandOptions.FirstOrDefault(option => option.Name == parameterName);
                if (commandOption == null || commandOption.Values.Count == 0)
                    throw new TemplateNotValidException($"Template is not valid, the defined option parameter name '{parameterName}' can not be found in the option definition or has no values. Invalid Parameter = '{parameterText}'");

                var options = commandOption.Values.SelectMany(value => value.LocalizedValues);
                regexTemplate += $"(?'{parameterName}'{string.Join('|', options)})";
                parameters.Add(parameterName, new CommandOptionParameter(parameterName, parameterType, isInOptionalSection, commandOption));
                break;
            case CommandParameterType.Area:
                regexTemplate += $"(?'{parameterName}'{AreaNames})";
                break;
            case CommandParameterType.Client:
                regexTemplate += $"(?'{parameterName}'{ClientNames})";
                break;
            case CommandParameterType.Device:
                regexTemplate += $"(?'{parameterName}'{DeviceNames})";
                break;
            case CommandParameterType.LightDevice:
                regexTemplate += $"(?'{parameterName}'{LightDeviceNames})";
                break;
            case CommandParameterType.SwitchDevice:
                regexTemplate += $"(?'{parameterName}'{SwitchDeviceNames})";
                break;
            default:
                throw new NotImplementedException();
        }

        if (parameterType != CommandParameterType.Option)
            parameters.Add(parameterName, new CommandParameter(parameterName, parameterType, isInOptionalSection));

        index = closingIndex + 1;
    }

    protected void SetAlternativeParametersIfExists(string alternativeTemplateSection, ref Dictionary<string, ICommandParameter> parameters)
    {
        // Get only top level alternative sections.
        while (true)
        {
            var openingSubSectionIndex = alternativeTemplateSection.IndexOfAny(['(', '[']);
            if (openingSubSectionIndex == -1)
                break;

            var currentOpeningSubSectionChar = alternativeTemplateSection[openingSubSectionIndex];
            var currentClosingSubSectionChar = SectionBrackets[currentOpeningSubSectionChar];

            var currentOpeningSubSectionIndex = openingSubSectionIndex;
            var currentClosingSubSectionIndex = openingSubSectionIndex;
            while (currentOpeningSubSectionIndex != -1)
            {
                currentClosingSubSectionIndex = alternativeTemplateSection.IndexOf(currentClosingSubSectionChar, currentClosingSubSectionIndex + 1);
                currentOpeningSubSectionIndex = alternativeTemplateSection.IndexOf(currentOpeningSubSectionChar, currentOpeningSubSectionIndex + 1, currentClosingSubSectionIndex - currentOpeningSubSectionIndex - 1);
            }

            alternativeTemplateSection = alternativeTemplateSection[..openingSubSectionIndex] + alternativeTemplateSection[(currentClosingSubSectionIndex + 1)..];
        }

        // Search for parameters in each alternative section
        var alternativeSections = alternativeTemplateSection.Split("|");
        var alternativeParameterSections = new List<List<ICommandParameter>>();
        foreach (var alternativeSection in alternativeSections)
        {
            var alternativeParametersInSection = new List<ICommandParameter>();
            foreach (var parameter in parameters)
                if (alternativeSection.Contains($"{{{parameter.Key}"))
                    alternativeParametersInSection.Add(parameter.Value);

            alternativeParameterSections.Add(alternativeParametersInSection);
        }

        // Add the alternative parameters to each parameter.
        foreach (var alternativeParameterSection in alternativeParameterSections)
            foreach (var alternativeParameter in alternativeParameterSection)
                alternativeParameter.AlternativeParameters.AddRange(alternativeParameterSections.Where(entry => entry != alternativeParameterSection).SelectMany(entry => entry));
    }
}