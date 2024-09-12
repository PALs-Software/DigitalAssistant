using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Localization;
using DigitalAssistant.Server.Modules.Commands.Exceptions;
using DigitalAssistant.Server.Modules.Commands.Services;
using DigitalAssistant.Server.Modules.Localization;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace DigitalAssistant.Server.Tests.Commands;

[TestClass]
public class TemplateParserParameterSectionTests : DigitalAssistantTestContext
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
        var template = "Turn the {Device:Text} on";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(1, commandTemplate.Parameters.Count);
        Assert.AreEqual("Device", commandTemplate.Parameters["Device"].Name);
        Assert.AreEqual(CommandParameterType.Text, commandTemplate.Parameters["Device"].Type);

        Assert.AreEqual("Turn the (?'Device'.+) on", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(RegexParseException))]
    public void NotValidWithPreWhiteSpace()
    {
        // Arrange
        var template = "Turn the{ Device:LightDevice} on";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void SingleNotValid()
    {
        // Arrange
        var template = "Turn the {Device:LightDevice on";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void NotValidTooManyParameter()
    {
        // Arrange
        var template = "Turn the {Device:LightDevice:} on";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void NotValidNoValues()
    {
        // Arrange
        var template = "Turn the {} on";

        // Act
        TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void NotValidMultipleParametersWithSameName()
    {
        // Arrange
        var template = "Turn the {x:LightDevice} {x:Boolean}";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    public void Multiple()
    {
        // Arrange
        var template = "Turn the {Device:LightDevice} {State:Boolean}";
        TemplateParser.SetTemplateNames([], [("light", [], DeviceType.Light), ("light2", [], DeviceType.Light), ("switch", [], DeviceType.Switch)]);

        // Act
        SwitchToLanguage(Language);
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(2, commandTemplate.Parameters.Count);
        Assert.AreEqual("Device", commandTemplate.Parameters["Device"].Name);
        Assert.AreEqual(CommandParameterType.LightDevice, commandTemplate.Parameters["Device"].Type);

        Assert.AreEqual("State", commandTemplate.Parameters["State"].Name);
        Assert.AreEqual(CommandParameterType.Boolean, commandTemplate.Parameters["State"].Type);

        Assert.AreEqual("Turn the (?'Device'light|light2) (?'State'0|false|off|down|switch off|turn off|1|true|on|up|switch on|turn on)", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public void MultipleDirectAfter()
    {
        // Arrange
        var template = "Turn the {Device:LightDevice}{State:Boolean}";
        TemplateParser.SetTemplateNames([], [("light", [], DeviceType.Light), ("light2", [], DeviceType.Light), ("switch", [], DeviceType.Switch)]);

        // Act
        SwitchToLanguage(Language);
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(2, commandTemplate.Parameters.Count);
        Assert.AreEqual("Device", commandTemplate.Parameters["Device"].Name);
        Assert.AreEqual(CommandParameterType.LightDevice, commandTemplate.Parameters["Device"].Type);

        Assert.AreEqual("State", commandTemplate.Parameters["State"].Name);
        Assert.AreEqual(CommandParameterType.Boolean, commandTemplate.Parameters["State"].Type);

        Assert.AreEqual("Turn the (?'Device'light|light2)(?'State'0|false|off|down|switch off|turn off|1|true|on|up|switch on|turn on)", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void NotValidNested()
    {
        // Arrange
        var template = "Turn the {Device:LightDevice {State:Boolean}}";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    public void Option()
    {
        // Arrange
        var template = "Turn the light {State:Option}";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(1, commandTemplate.Parameters.Count);
        Assert.AreEqual("State", commandTemplate.Parameters["State"].Name);
        Assert.AreEqual(CommandParameterType.Option, commandTemplate.Parameters["State"].Type);
        Assert.IsTrue(commandTemplate.Parameters["State"] is ICommandOptionParameter);

        Assert.AreEqual("State", commandTemplate.Parameters["State"].AsOptionParameter().Option.Name);
        Assert.AreEqual("On", commandTemplate.Parameters["State"].AsOptionParameter().Option.Values[0].Name);
        Assert.AreEqual("Off", commandTemplate.Parameters["State"].AsOptionParameter().Option.Values[1].Name);
        CollectionAssert.AreEqual(new List<string>() { "On", "Turn on" }, commandTemplate.Parameters["State"].AsOptionParameter().Option.Values[0].LocalizedValues);
        CollectionAssert.AreEqual(new List<string>() { "Off", "Turn off" }, commandTemplate.Parameters["State"].AsOptionParameter().Option.Values[1].LocalizedValues);

        Assert.AreEqual("Turn the light (?'State'On|Turn on|Off|Turn off)", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void OptionNotValidNoValues()
    {
        // Arrange
        var template = "Turn the {Device:LightDevice} {State:Option}";
        
        // Act
        SwitchToLanguage("nl");
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }

    [TestMethod]
    [Timeout(10000)]
    [ExpectedException(typeof(TemplateNotValidException))]
    public void OptionNotValidNoValues2()
    {
        // Arrange
        var template = "Turn the {Device:LightDevice} {State:Option}";

        // Act
        SwitchToLanguage("be");
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
    }
}
