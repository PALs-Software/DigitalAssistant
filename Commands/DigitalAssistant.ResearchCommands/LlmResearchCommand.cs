using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace DigitalAssistant.ResearchCommands;

public class LlmResearchCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 80000;

    public override string[] LlmFunctionTemplates => ["LlmResearch(Question: Text)"];
    public override string LlmFunctionDescription => "Request information on a specific topic.";

    #region Members
    protected string SystemPrompt = @"You are a digital assistant designed to help users by answering their questions efficiently.
Follow these style guidelines when generating content:
Tone: Be friendly and approachable.
Clarity: Use simple, straightforward language that is easy for all users to understand.
Structure: Keep sentences and paragraphs short and concise. Answer the question as briefly as possible.
Avoid: Jargon, technical terms, or overly complex language unless absolutely necessary.
Language: Use the user's language {0} to answer the question.
Your goal is to provide clear, helpful responses that users can quickly absorb and act on.";
    #endregion

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (!parameters.TryGetValue<string>("Question", out var question))
            return Task.FromResult(CreateResponse(success: false));
    
        var args = new LlmActionArgs()
        {
            SystemPrompt = String.Format(SystemPrompt, new CultureInfo(parameters.Language).DisplayName),
            UserPrompt = question,
            MaxLength = 1024
        };

        return Task.FromResult(CreateResponse(success: true, null, [(parameters.Client, args)]));
    }
}
