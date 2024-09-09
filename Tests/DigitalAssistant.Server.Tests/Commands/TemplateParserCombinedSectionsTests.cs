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
using System;
using System.Globalization;
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
    public async Task TestAlternativeParametersAsync()
    {
        // alternative parameter einmal testen für doku
       throw new NotImplementedException();
    }

    [TestMethod]
    [Timeout(100000)]
    public async Task TestOptionalParametersAsync()
    {
        // optionsal parameter einmal testen für doku
        throw new NotImplementedException();
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
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Schalte das Küchenlicht an");

        var lightDevice = new LightDevice { InternalId = "123", Name = "Küchenlicht", Type = DeviceType.Light, Status = DeviceStatus.Online, Connector = "LightConnector", Manufacturer = "Light Manufacturer", ProductName = "Light" };
        await DbContext.AddAsync(lightDevice);
        await DbContext.SaveChangesAsync();

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual("(?:Schalte|Setze|Wechsle|Ändere|Stelle)(?: (?:das|die))? (?'Light'Küchenlicht|light)(?: )?(?:Licht)?(?: auf)? (?'State'0|false|aus|runter|ausmachen|aus machen|einschalten|1|true|an|hoch|anmachen|an machen|ausschalten)", commandTemplate.Regex.ToString());
        Assert.AreEqual(2, parameters.Parameters.Count);
        Assert.AreEqual(true, parameters.Parameters["State"].Value);
        Assert.AreEqual(lightDevice.Name, ((ILightDevice)parameters.Parameters["Light"].Value!).Name);
    }

    #region MISC
    protected void SwitchToLanguage(string langugae)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(langugae);
        CultureInfo.DefaultThreadCurrentCulture = Thread.CurrentThread.CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = Thread.CurrentThread.CurrentCulture;
    }
    #endregion
}
