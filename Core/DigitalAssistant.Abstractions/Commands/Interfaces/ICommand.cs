using DigitalAssistant.Abstractions.Commands.Enums;

namespace DigitalAssistant.Abstractions.Commands.Interfaces;

public interface ICommand
{
    CommandType Type { get; }
    int Priority { get; }
    string LlmFunctionTemplate { get; }
    string LlmFunctionDescription { get; }

    Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters);

    string GetName();
    string GetDescription();
    List<string> GetTemplates();
    string GetOptionsJson();

    #region Llm
    string? GetLlmFunctionName();
    Dictionary<string, string> GetLlmParameters();
    #endregion

    public static string? GetLlmFunctionName(string template)
    {
        if (String.IsNullOrEmpty(template))
            return null;

        if (!template.Contains("("))
            throw new Exception($"The llm function template \"{template}\" is not valid. Make sure it has the format like this: MyFunction(Parameter1: Type)");

        return template.Split('(')[0].Trim();
    }

    public static Dictionary<string, string> GetLlmParameters(string parametersText)
    {
        if (String.IsNullOrEmpty(parametersText))
            return [];

        var parameterDictionary = new Dictionary<string, string>();
        var parameters = parametersText.Split('(')[1].TrimEnd(')').Split(",");
        foreach (var parameter in parameters)
        {
            if (String.IsNullOrEmpty(parameter))
                continue;

            var splittedParameter = parameter.Split(":");
            if (splittedParameter.Length == 2)
                parameterDictionary.Add(splittedParameter[0].Trim(), splittedParameter[1].Trim());
        }

        return parameterDictionary;
    }
}
