# Create a new connector

## Preparation

1. Setup your local development environment like it is explained [here](setup-development-environment.md).
2. Create a new Project in the `Connectors` folder from the type `Microsoft.NET.Sdk.Razor`
3. Add a project reference to the **DigitalAssistant.Abstractions** project

> [!NOTE]
> It is planned that a NuGet package will be provided for the **DigitalAssistant.Abstractions** project, so new connectors can be provided independently of a local development environment of the server components.

## Create connector service

1. Create a new class that inherits from interface `IConnector`.
2. Create a constructor which matches the following.

```c#
    public ConnectorServiceName(string? connectorSettingsJson,
        Func<IConnector, string, DeviceType, Task<IDevice?>> getDeviceAsync,
        Func<List<IDeviceChangeArgs>, Task> onDeviceChangedAsync,
        IDeviceFactory deviceFactory,
        IDeviceChangeArgsFactory deviceChangeArgsFactory,
        IDataProtectionService dataProtectionService,
        IStringLocalizer localizer,
        ILogger logger)
```

3. Implement the needed methods from the interface.

> [!Tip]
> Use the **DigitalAssistant.HueConnector** project as an example and guide.

## Save connector settings

1. Create a class with the required settings as properties and inherits from the interface `IConnectorSettings`.
2. In the `RegisterAsync` method return an instance of that class which the settings provided needed to run the connector.
3. The settings provided by the registration of the connector will then be passed by every startup of the connector in his constructor. Then use the `JsonSerializer.Deserialize<TheSettingClass?>(connectorSettingsJson)` method to change the serialized settings back to the settings class.

## Create connector setup wizard

When the user activates the new connector, settings may need to be made and information may need to be requested from the user. These settings should be made via a wizard. The blazor component that represents the wizard can be customized by each connector according to its needs. To do this, the type of Blazor component that represents the wizard is stored in the connector service in the **SetupComponentType** property, so the framework knows which component must be rendered.

1. Create a new Blazor component in the project of the connector.
2. Specify in the connector service the **SetupComponentType** property with that value as the following example shows:

```c#
public Type SetupComponentType => typeof(MyBlazorSetupWizardComponent);
```

3. Specify the following two parameters inside the wizard. The first parameter represents an instance of the connector service that can be interacted with. The second parameter is an event callback that can be called when the wizard has finished or the user has canceled it.

```c#
[Parameter] public IConnector Connector { get; set; } = null!;
[Parameter] public EventCallback<(bool Success, IConnectorSettings? Settings)> OnConnectorSetupFinished { get; set; }
```

4. Implement the wizard logic with all things the connector needs to run.
5. Once all the required settings have been made, execute the implemented method `RegisterAsync` of your connector and, if successful, invoke the callback `OnConnectorSetupFinished` to close the wizard.

> [!Tip]
> Use the **EnableConnectorSetup.razor** component from the **DigitalAssistant.HueConnector** project as template.

### Import the connector to the Digital Assistant Server
The created .dll library and translation files must be copied to the `Connectors` folder of the server application. After a restart of the server components, the connector will be loaded automatically.

To automate this for development, adjust and add the following lines to the connector's project `.csproj` file. This will copy the files after each build of the connector project.

``` xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Message Importance="High" Text="COPY MyConnector output to Server output" />
    <ItemGroup>
        <MySourceFiles Include="$(OutDir)\DigitalAssistant.MyConnector.*" />
    </ItemGroup>
    <Copy SourceFiles="@(MySourceFiles)" DestinationFolder="$(ProjectDir)..\..\Core\DigitalAssistant.Server\bin\$(Configuration)\net8.0\Connectors\MyConnector" />
</Target>
```