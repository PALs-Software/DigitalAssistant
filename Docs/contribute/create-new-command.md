# Create a new command

## Preparation

1. Setup your local development environment like it is explained [here](setup-development-environment.md).
2. Create a new Project in the `Commands` folder from the type `Microsoft.NET.Sdk`
3. Add a project reference to the **DigitalAssistant.Abstractions** project

> [!NOTE]
> It is planned that a NuGet package will be provided for the **DigitalAssistant.Abstractions** project, so new commands can be provided independently of a local development environment of the server components.

## Create command

1. Create a new class that inherits from class `Command`.
2. Add the following constructor and parse the needed values to the base class constructor:

```c#
    public class MyCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
```

3. Add the file `MyCommand.en.json`.
4. Fill the file with the follwing template:

```json
{
  "Name": "Name of the command",
  "Description": "Description of the command",
  "Templates": [
    "What must the user say to run this command",
    "Add several variants here in the Templates property"
  ],
  "Options": [],
  "Responses": ["Add some possible responses here"]
}
```

5. More details about specifying the templates with possible word phrases and parameter can be found in the chapter [Understanding the template language](#understanding-the-template-language).
6. Implement the `ExecuteAsync` method.
   - Values of Parameters specified by the user can be get over the parameters variable like `parameters.TryGetValue<TheTypeOfTheParameter>("TheNameOfTheParameter", out var parameter)`
   - Implement what ever your command is about.
   - Return a response filled with the words the digital assistant should respond.
   - Optionally, device and client actions can also be returned to control a specific device or perform some client actions. The following list shows all implemented action arguments. If there is no action that meets your needs, you can add and implement it yourself.
     - SystemActionArgs
     - MusicActionArgs
     - TimerActionArgs
     - LightActionArgs
7. Translate the `MyCommand.en.json` in all languages you can provide and open create a task in github for the community to translate the rest.

> [!Tip]
> Use the **DigitalAssistant.DeviceCommands** project or one of the other commands as an example and guide.

## Understanding the template language

The templates are compared with the user's words to find out which command should be executed. The templates are later converted into a regex syntax for this purpose.

As the same command can often be expressed in different ways, it is important to include enough variations in the templates.

With the help of special symbols in the templates, optional and alternative words and areas can be defined and also parameters can be defined that contain certain words from the user that are important for executing the command. For example, the name of a smart home device that is to be controlled or the brightness level to which a lamp is to be set.

### Optional/Alternative letters, words, sentences

Optional and Alternative phrases can be used to combine multiple templates to one, so it must not be created a separate template for every added or removed fill-in word for example. A optional phrase starts with the `(` symbol and ends with `)` and an alternative phrase starts with the `[` symbol and ends with `]` where the alternative parts are separated by the `|` symbol. These special symbols can also be nested inside each other.

```
  "Templates": [
    "What time (is it)",
    "What('s) (the) time (right now)",
    "[Turn|Set|Switch] ([the|a]) light on"
  ],
```

### Parameters

Parameters are placeholders whose value can later be used for the command execution. Parameters are specified in the symbols `{` and `}`. The first text inside these brackets specify the name of the parameter and the text after the `:` separation symbol specify the type of the parameter.

Currently the following parameter types are supported:

- Boolean
- Text
- Integer
- Decimal
- Date
- Time
- Color
- ColorTemperatureColor
- Option
- Area
- Client
- Device
- LightDevice
- SwitchDevice

The values can be read from the parameter variable of the `ExecuteAsync` method

```c#
parameters.TryGetValue<TheTypeOfTheParameter>("TheNameOfTheParameter", out var parameter)`
```

```
  "Templates": [
    "How much time is left on the timer {Name:Text}",
    "([Set|Set up]) (a) timer ((named) {Name:Text}) [to|for|in] {Duration:Decimal} {DurationType:Option}",
    "[Turn|Set|Switch|Change] (the) {Light:LightDevice} {State:Boolean}"
  ],
```

### Option Parameters

The option parameter is a special case. To use this, the option values must also be specified in the json file. Note that the specified Option parameter name must match with the name of the option specified.

```
"Templates": [
    "([Set|Set up]) (a) timer ((named) {Name:Text}) [to|for|in] {Duration:Decimal} {DurationType:Option}"
 ],
"Options": [
    {
        "Name": "DurationType",
        "Values": [
            {
                "Name": "Seconds",
                "LocalizedValues": [ "Second", "Seconds" ]
            },
            {
                "Name": "Minutes",
                "LocalizedValues": [ "Minute", "Minutes" ]
            },
            {
                "Name": "Hours",
                "LocalizedValues": [ "Hour", "Hours" ]
            }
        ]
    }
]
```

### Import the command to the Digital Assistant Server
The created .dll library and translation files must be copied to the `Commands` folder of the server application. After a restart of the server components, the command will be loaded automatically.

To automate this for development, adjust and add the following lines to the command's project `.csproj` file. This will copy the files after each build of the command project.

``` xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Message Importance="High" Text="COPY MyCommand output to Server output" />
    <ItemGroup>
        <DllFiles Include="$(OutDir)\DigitalAssistant.MyCommand.*" />
        <TranslationFiles Include="$(OutDir)\**\*.json" />
    </ItemGroup>
    <Copy SourceFiles="@(DllFiles)" DestinationFolder="$(ProjectDir)..\..\Core\DigitalAssistant.Server\bin\$(Configuration)\net8.0\Commands\DigitalAssistant.MyCommand" />
    <Copy SourceFiles="@(TranslationFiles)" DestinationFolder="$(ProjectDir)..\..\Core\DigitalAssistant.Server\bin\$(Configuration)\net8.0\Commands\DigitalAssistant.MyCommand\%(RecursiveDir)" />
</Target>
```