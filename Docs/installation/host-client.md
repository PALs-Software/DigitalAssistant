# Host Client

A Digital Assistant Client can be installed on any device, your desktop PC, a server or a single-board computer such as the Raspberry Pi or a microcontroller like the ESP32. The main thing is that a microphone and a loudspeaker are connected to it. Multiple clients can be connected to the server, for example to use the digital assistant in several rooms.

## Installation

### Manually

1. Download and install the [**.NET Runtime 8.\***](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) for your platform type.
2. Download the binaries of your platform from the [release page of the github repository](https://github.com/PALs-Software/DigitalAssistant).
3. Extract the compressed files.
4. **Optional**: Change the default configuration of the client by adjusting the `appsettings.json` file like it is explained in the chapter "[Change default configuration](#change-default-configuration)".
5. Add the server certificate as trusted in the operating system.
6. Open a terminal of your choice.
7. Install mpg123, it's used to play the internet radio streams
``` shell
sudo apt-get install mpg123
```
9. Navigate to the extracted binary files.
10. Run the following command to start the client application: `dotnet DigitalAssistant.Client.dll`

Further information's how to connect the client with the server can be found in the [setup client](../setup/clients.md) chapter of this documentation.

### Microcontroller

ToDo...

### Raspberry Pi Image

Currently not possible, but it is planned to provide a Raspberry Pi image for the client application.

## Change default configuration

The default configuration of the client application can be changed in the `appsettings.json` file.

### Client Settings

The client settings can be changed in the `ClientSettings` property. But it is easier to configure these settings over the administration website of the server in the client settings.

### Server Connection

By default only the `ServerPort` property is specified in the settings of the `ServerConnection` in the `appsettings.json` file. The client discovers the server automatically in the local network and after a configuration in the administration website the client will fill the rest of the settings manually. But it is also possibly to manually configure the client by setting manually the `ServerName` and `ServerAccessToken` property.

## Troubleshooting

### Certificate issues

The client does not connect to the server and the log says something about certificate errors by validating server certificate. Then the client does not trust the server certificate. To secure the connection between client and server, the client must trust the used certificate of the server by default. To fix this problem, add the server certificate used as trusted in the client.

For debugging proposes you can also set the `IgnoreServerCertificateErrors` property in the `appsettings.json` file of the server to true, to ignore those certificate errors, but this is not recommended for production environments.
