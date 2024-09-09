using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using DigitalAssistant.Server.Modules.Commands.Exceptions;
using DigitalAssistant.Server.Modules.Commands.Services;
using DigitalAssistant.Server.Modules.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Tests.Commands;

[TestClass]
public class TemplateParserAlternativeSectionTests : DigitalAssistantTestContext
{
    #region Injects
    protected CommandTemplateParser TemplateParser = null!;
    protected ICommand DummyCommand = null!;
    #endregion

    #region Members
    protected string Language = "en";
    #endregion

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();

        TemplateParser = Services.GetRequiredService<CommandTemplateParser>();
        var localizer = (IStringLocalizer)Services.GetRequiredService(typeof(IStringLocalizer<>).MakeGenericType(typeof(DummyCommand)));
        var jsonLocalizer = (IJsonStringLocalizer)Services.GetRequiredService(typeof(JsonStringLocalizer<>).MakeGenericType(typeof(DummyCommand)));
        DummyCommand = new DummyCommand(localizer, jsonLocalizer);
    }

    [TestMethod]
    [Timeout(10000)]
    public void SingleSection()
    {
        // Arrange
        var template = "Turn the [light|switch] on";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("Turn the (?:light|switch) on", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void WithPreWhiteSpace()
    {
        // Arrange
        var template = "Turn the[ light|switch] on";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("Turn the(?: light|switch) on", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void SingleNotValid()
    {
        // Arrange
        var template = "Turn the [light|switch on";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void NotValidDoublePipe()
    {
        // Arrange
        var template = "Turn the [light||switch] on";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void NotValidNoValues()
    {
        // Arrange
        var template = "Turn the [] on";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    public void Multiple()
    {
        // Arrange
        var template = "Turn the [light|switch|roller shutter] to [blue|yellow|red|on|off|up|down]";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("Turn the (?:light|switch|roller shutter) to (?:blue|yellow|red|on|off|up|down)", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void MultipleDirectAfter()
    {
        // Arrange
        var template = "Turn the [light|switch|roller shutter][blue|yellow|red|on|off|up|down]";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("Turn the (?:light|switch|roller shutter)(?:blue|yellow|red|on|off|up|down)", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void Nested()
    {
        // Arrange
        var template = "Turn the [light to [blue|yellow|red]|switch to [on|off]|roller shutter to [up|down]]";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("Turn the (?:light to (?:blue|yellow|red)|switch to (?:on|off)|roller shutter to (?:up|down))", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void NestedTextAfter()
    {
        // Arrange
        var template = "Turn the [light to [blue|yellow|red] text after|switch to [on|off] text after|roller shutter to [up|down] text after] text after";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("Turn the (?:light to (?:blue|yellow|red) text after|switch to (?:on|off) text after|roller shutter to (?:up|down) text after) text after", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void NestedNotValid()
    {
        // Arrange
        var template = "A [nested not [valid|test] template";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    public void TwoNestedSections()
    {
        // Arrange
        var template = "A [nested [test|validation] of a [valid|not valid]] template";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("A (?:nested (?:test|validation) of a (?:valid|not valid)) template", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void NestedNested()
    {
        // Arrange
        var template = "A [nested [test|[double nested|double test]] extra] template";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("A (?:nested (?:test|(?:double nested|double test)) extra) template", commandTemplate.Regex.ToString());
    }
}
