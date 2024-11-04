using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Commands.Parser;
using DigitalAssistant.Server.Modules.Devices.Models;
using DigitalAssistant.Server.Modules.Localization;
using Microsoft.Extensions.Localization;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalAssistant.Server.Tests.Commands;

[TestClass]
public class CommandParameterParserTests : DigitalAssistantTestContext
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
    [Timeout(10000)]
    public async Task ParseBooleanTypeAsync()
    {
        // Arrange
        var template = "Turn the Light {State:Boolean}";
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Turn the Light on");

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual(true, parameters.Parameters["State"].Value);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseBooleanTypeOtherLanguageAsync()
    {
        // Arrange
        var language = "de";
        SwitchToLanguage(language);
        var template = "Schalte das Licht {State:Boolean}";
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, language);
        var match = commandTemplate.Regex.Match("Schalte das Licht an");

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual(true, parameters.Parameters["State"].Value);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseTextTypeAsync()
    {
        // Arrange
        var template = "Turn the {Name:Text} on";
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Turn the kitchen light on");

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual("kitchen light", parameters.Parameters["Name"].Value);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseIntegerTypeAsync()
    {
        // Arrange
        var template = "Turn the light to {Brightness:Integer} percent";
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Turn the light to 50 percent");

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual(50, parameters.Parameters["Brightness"].Value);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseDecimalTypeAsync()
    {
        // Arrange
        var template = "Turn the light to {Brightness:Decimal} percent";
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Turn the light to 50.5 percent");

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual(50.5m, parameters.Parameters["Brightness"].Value);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseDateTypeAsync()
    {
        // Arrange
        var template = "Remind me to set a timer on {Date:Date}";
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Remind me to set a timer on 7/24");

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual(new DateTime(DateTime.Now.Year, 7, 24), parameters.Parameters["Date"].Value);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseTimeTypeAsync()
    {
        // Arrange
        var template = "Set a timer for {Time:Time}";
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Set a timer for 3:30 pm");

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual(new TimeSpan(15, 30, 0), parameters.Parameters["Time"].Value);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseColorTypeAsync()
    {
        // Arrange
        var template = "Set the light to {Color:Color}";
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Set the light to blue");

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual(Color.Blue, parameters.Parameters["Color"].Value);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseOptionTypeAsync()
    {
        // Arrange
        var template = "Set the light to {Color:Option}";
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Set the light to yellow");

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual("Yellow", parameters.Parameters["Color"].Value);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseDeviceTypeAsync()
    {
        // Arrange
        var template = "Turn the {Device:Device} off";
        TemplateParser.SetTemplateNames([], [("light", [], DeviceType.Light), ("light2", [], DeviceType.Light), ("switch", [], DeviceType.Switch)], [("group1", []), ("group2", [])]);
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Turn the light off");

        var lightDevice = new LightDevice { InternalId = "123", Name = "light", Type = DeviceType.Light, Status = DeviceStatus.Online, Connector = "LightConnector", Manufacturer = "Light Manufacturer", ProductName = "Light" };
        await DbContext.AddAsync(lightDevice);
        await DbContext.SaveChangesAsync();

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual(lightDevice.Name, ((ILightDevice)parameters.Parameters["Device"].Value!).Name);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseLightDeviceTypeAsync()
    {
        // Arrange
        var template = "Turn the {Device:LightDevice} off";
        TemplateParser.SetTemplateNames([], [("light", [], DeviceType.Light), ("light2", [], DeviceType.Light), ("switch", [], DeviceType.Switch)], [("group1", []), ("group2", [])]);
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Turn the light off");

        var lightDevice = new LightDevice { InternalId = "123", Name = "light", Type = DeviceType.Light, Status = DeviceStatus.Online, Connector = "LightConnector", Manufacturer = "Light Manufacturer", ProductName = "Light" };
        await DbContext.AddAsync(lightDevice);
        await DbContext.SaveChangesAsync();

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual(lightDevice.Name, ((ILightDevice)parameters.Parameters["Device"].Value!).Name);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseSwitchDeviceTypeAsync()
    {
        // Arrange
        var template = "Turn the {Device:SwitchDevice} off";
        TemplateParser.SetTemplateNames([], [("light", [], DeviceType.Light), ("light2", [], DeviceType.Light), ("switch", [], DeviceType.Switch)], [("group1", []), ("group2", [])]);
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Turn the switch off");

        var switchDevice = new SwitchDevice { InternalId = "123", Name = "switch", Type = DeviceType.Switch, Status = DeviceStatus.Online, Connector = "SwitchConnector", Manufacturer = "Switch Manufacturer", ProductName = "Switch" };
        await DbContext.AddAsync(switchDevice);
        await DbContext.SaveChangesAsync();

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(1, parameters.Parameters.Count);
        Assert.AreEqual(switchDevice.Name, ((ISwitchDevice)parameters.Parameters["Device"].Value!).Name);
    }

    [TestMethod]
    [Timeout(100000)]
    public async Task AlternativeParametersTestAsync()
    {
        // Arrange
        var template = "Turn the [{LightDevice:LightDevice}|{SwitchDevice:SwitchDevice}] off";
        TemplateParser.SetTemplateNames([], [("light", [], DeviceType.Light), ("light2", [], DeviceType.Light), ("switch", [], DeviceType.Switch)], [("group1", []), ("group2", [])]);
        var commandTemplate = TemplateParser.ParseTemplate(DummyCommand, template, Language);
        var match = commandTemplate.Regex.Match("Turn the switch off");

        var lightDevice = new LightDevice { InternalId = "123", Name = "light", Type = DeviceType.Light, Status = DeviceStatus.Online, Connector = "LightConnector", Manufacturer = "Light Manufacturer", ProductName = "Light" };
        var switchDevice = new SwitchDevice { InternalId = "123", Name = "switch", Type = DeviceType.Switch, Status = DeviceStatus.Online, Connector = "SwitchConnector", Manufacturer = "Switch Manufacturer", ProductName = "Switch" };
        await DbContext.AddAsync(lightDevice);
        await DbContext.AddAsync(switchDevice);
        await DbContext.SaveChangesAsync();

        // Act
        (bool success, ICommandParameters? parameters) = await ParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, Language, ClientBase.Browser, InterpreterMode.RegularExpression);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(parameters);
        Assert.AreEqual(2, parameters.Parameters.Count);
        Assert.IsNull(parameters.Parameters["LightDevice"].Value);
        Assert.AreEqual(switchDevice.Name, ((ISwitchDevice)parameters.Parameters["SwitchDevice"].Value!).Name);
        Assert.AreEqual(DeviceType.Switch, ((ISwitchDevice)parameters.Parameters["SwitchDevice"].Value!).Type);
    }
}
