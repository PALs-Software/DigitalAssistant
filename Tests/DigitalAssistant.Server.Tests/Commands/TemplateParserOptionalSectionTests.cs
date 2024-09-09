using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using DigitalAssistant.Server.Modules.Commands.Exceptions;
using DigitalAssistant.Server.Modules.Commands.Services;
using DigitalAssistant.Server.Modules.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Tests.Commands;

[TestClass]
public class TemplateParserOptionalSectionTests : DigitalAssistantTestContext
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
        var template = "What time (is it)";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("What time(?: is it)?", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void WithPreWhiteSpace()
    {
        // Arrange
        var template = "What time( is it)";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("What time(?: is it)?", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void SingleNotValid()
    {
        // Arrange
        var template = "What time( is it";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void EmptyNotValid()
    {
        // Arrange
        var template = "What time () is it";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void NestedEmptyNotValid()
    {
        // Arrange
        var template = "What time (is ()it)";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    public void Multiple()
    {
        // Arrange
        var template = "What('s) (the) time";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("What(?:'s)?(?: the)? time", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void MultipleDirectAfter()
    {
        // Arrange
        var template = "What('s)( the) time";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("What(?:'s)?(?: the)? time", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void Nested()
    {
        // Arrange
        var template = "A (nested (test)) template";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("A(?: nested(?: test)?)? template", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void NestedTextAfter()
    {
        // Arrange
        var template = "A (nested (test) text after) template";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("A(?: nested(?: test)? text after)? template", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void NestedNotValid()
    {
        // Arrange
        var template = "A (nested (test) template";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    public void TwoNestedSections()
    {
        // Arrange
        var template = "A (nested (test) of a (valid)) template";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("A(?: nested(?: test)? of a(?: valid)?)? template", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void NestedNested()
    {
        // Arrange
        var template = "A (nested (test (double nested)) extra) template";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("A(?: nested(?: test(?: double nested)?)? extra)? template", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void NestedAfter()
    {
        // Arrange
        var template = "A (nested (test)) (after) template";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("A(?: nested(?: test)?)?(?: after)? template", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void NestedAfterWithEmptyOptional()
    {
        // Arrange
        var template = "A (nested (test))( )(after) template";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(0, commandTemplate.Parameters.Count);
        Assert.AreEqual("A(?: nested(?: test)?)?(?: )?(?:after)? template", commandTemplate.Regex.ToString());
    }
}
