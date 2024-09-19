using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Commands.Services;
using DigitalAssistant.Server.Modules.Devices.Models;
using DigitalAssistant.Server.Modules.Localization;
using Microsoft.Extensions.Localization;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalAssistant.Server.Tests.Commands;

[TestClass]
public class TemplateParserCombinedSectionsTests : DigitalAssistantTestContext
{
    #region Injects
    protected IBaseDbContext DbContext = null!;
    protected CommandTemplateParser TemplateParser = null!;
    protected CommandParameterParser ParameterParser = null!;
    protected ICommand DummyCommand = null!;
    #endregion

    #region Members
    protected string Language = "en";
    #endregion

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();

        DbContext = Services.GetRequiredService<IBaseDbContext>();
        TemplateParser = Services.GetRequiredService<CommandTemplateParser>();
        ParameterParser = Services.GetRequiredService<CommandParameterParser>();
        var localizer = (IStringLocalizer)Services.GetRequiredService(typeof(IStringLocalizer<>).MakeGenericType(typeof(DummyCommand)));
        var jsonLocalizer = (IJsonStringLocalizer)Services.GetRequiredService(typeof(JsonStringLocalizer<>).MakeGenericType(typeof(DummyCommand)));
        DummyCommand = new DummyCommand(localizer, jsonLocalizer);
    }

    [TestMethod]
    [Timeout(100000)]
    public void TestAlternativeParameters()
    {
        // Arrange
        var template = "Turn the brightness to [{IntegerValue:Integer}|{DecimalValue:Decimal}]";

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(2, commandTemplate.Parameters.Count);
        Assert.AreEqual(1, commandTemplate.Parameters["DecimalValue"].AlternativeParameters.Count);
        Assert.AreEqual(1, commandTemplate.Parameters["IntegerValue"].AlternativeParameters.Count);
        Assert.AreEqual(commandTemplate.Parameters["IntegerValue"], commandTemplate.Parameters["DecimalValue"].AlternativeParameters[0]);
        Assert.AreEqual(commandTemplate.Parameters["DecimalValue"], commandTemplate.Parameters["IntegerValue"].AlternativeParameters[0]);
        Assert.AreEqual("Turn the brightness to (?:(?'IntegerValue'\\d+)|(?'DecimalValue'\\d+\\.?\\d*))", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(100000)]
    public void TestOptionalParameters()
    {
        // Arrange
        var template = "Turn the {Light:LightDevice} ({State:Boolean})";
        TemplateParser.SetTemplateNames([], [("light", [], DeviceType.Light), ("light2", [], DeviceType.Light), ("switch", [], DeviceType.Switch)]);

        // Act
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);

        // Assert
        Assert.AreEqual(2, commandTemplate.Parameters.Count);
        Assert.IsFalse(commandTemplate.Parameters["Light"].IsOptional);
        Assert.IsTrue(commandTemplate.Parameters["State"].IsOptional);
        Assert.AreEqual("Turn the (?'Light'light|light2)(?: (?'State'0|false|off|down|switch off|turn off|1|true|on|up|switch on|turn on))?", commandTemplate.Regex.ToString());
    }

    [TestMethod]
    [Timeout(100000)]
    public async Task ParseDeviceTypeAsync()
    {
        // Arrange
        string language = "de";
        var template = "[Schalte|Setze|Wechsle|Ändere|Stelle] ([das|die]) {Light:LightDevice}( )(Licht) (auf) {State:Boolean}";
        SwitchToLanguage(language);
        TemplateParser.SetTemplateNames([], [("Küchenlicht", [], DeviceType.Light), ("light", [], DeviceType.Light), ("switch", [], DeviceType.Switch)]);
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, language);
        var match = commandTemplate.Regex.Match("Schalte das Küchenlicht an");

        var lightDevice = new LightDevice { InternalId = "123", Name = "Küchenlicht", Type = DeviceType.Light, Status = DeviceStatus.Online, Connector = "LightConnector", Manufacturer = "Light Manufacturer", ProductName = "Light" };
        await DbContext.AddAsync(lightDevice);
        await DbContext.SaveChangesAsync();

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, language, ClientBase.Browser);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual("(?:Schalte|Setze|Wechsle|Ändere|Stelle)(?: (?:das|die))? (?'Light'Küchenlicht|light)(?: )?(?:Licht)?(?: auf)? (?'State'0|false|aus|runter|ausmachen|aus machen|ein|einschalten|1|true|an|hoch|anmachen|an machen|ausschalten)", commandTemplate.Regex.ToString());
        Assert.AreEqual(2, parameters.Parameters.Count);
        Assert.AreEqual(true, parameters.Parameters["State"].Value);
        Assert.AreEqual(lightDevice.Name, ((ILightDevice)parameters.Parameters["Light"].Value!).Name);
    }
}
