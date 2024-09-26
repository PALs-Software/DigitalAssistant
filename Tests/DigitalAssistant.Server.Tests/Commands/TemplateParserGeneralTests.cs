using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using DigitalAssistant.Server.Modules.Commands.Parser;
using DigitalAssistant.Server.Modules.Localization;
using Microsoft.Extensions.Localization;
using System;

namespace DigitalAssistant.Server.Tests.Commands;

[TestClass]
public class TemplateParserGeneralTests : DigitalAssistantTestContext
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
    [ExpectedException(typeof(ArgumentNullException))]
    public void Null()
    {
        // Arrange
        string? template = null;

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template!, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(ArgumentException))]
    public void Empty()
    {
        // Arrange
        var template = "";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    public void JustPlainText()
    {
        // Arrange
        var template = "This is a test";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("This is a test", commandTemplate.Regex.ToString());
    }
}
