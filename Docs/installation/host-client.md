# Host Client

A Digital Assistant Client can be installed on any device, your desktop PC, a server or a single-board computer such as the Raspberry Pi or a microcontroller. The main thing is that a microphone and a loudspeaker are connected to it. Multiple clients can be connected to the server, for example to use the digital assistant in several rooms.

## Installation

### Manually

1. Download and install the [**.NET Runtime 8.\***](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) for your platform type.
``` shell
# For Linux run:
sudo apt-get update && sudo apt-get install -y aspnetcore-runtime-8.0

# For Raspberry Pi OS run:
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
sudo chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 8.0 --runtime aspnetcore
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> /home/pi/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet' >> /home/pi/.bashrc
source ~/.bashrc
```
2. Download the binaries of your platform from the [release page of the github repository](https://github.com/PALs-Software/DigitalAssistant/releases).
3. Extract the compressed files.
4. **Optional**: Change the default configuration of the client by adjusting the `appsettings.json` file like it is explained in the chapter "[Change default configuration](#change-default-configuration)".
5. Add the server certificate as trusted in the operating system.
6. Open a terminal of your choice.
7. [**Only on Linux**]: Install mpg123, it's used to play the internet radio streams
``` shell
sudo apt-get install mpg123
```
7. [**Only on Mac**]: Install mpg123 and ffmpeg, it's used to play the internet radio streams and to record your voice commands
``` shell
brew install mpg123
brew install ffmpeg
```
9. Navigate to the extracted binary files.
10. Run the following command to start the client application:
``` shell
 dotnet DigitalAssistant.Client.dll
```
11. Use tools depending on your operating system to auto start the application at statup of your machine.

Further information's how to connect the client with the server can be found in the [setup client](../setup/clients.md) chapter of this documentation.

### Microcontroller

See chapter [Host MicroClient](host-micro-client.md) for more details.

### Docker
This method is only working on a linux host system, because there only the driver for microphone and speaker can be mapped into the docker container.

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/).
2. Use the integrated terminal in Docker Desktop or open another command terminal of your choice.
3. Execute the following command. This will download and run the digital assistant client application inside a docker container.

```
# Run in host network
docker run --name digital-assistant-client --network host --device /dev/snd:/dev/snd -v DigitalAssistantClient_Data:/app/DockerStorage -v DigitalAssistantClient_Keys:/home/app/.aspnet/DataProtection-Keys -d palssoftware/digital-assistant-client

# Run in internal docker network (If server is also a docker container on the same host)
docker run --name digital-assistant-client --device /dev/snd:/dev/snd -v DigitalAssistantClient_Data:/app/DockerStorage -v DigitalAssistantClient_Keys:/home/app/.aspnet/DataProtection-Keys -d palssoftware/digital-assistant-client

```

Further information's how to connect the client with the server can be found in the [setup client](../setup/clients.md) chapter of this documentation.
   
### Raspberry Pi Image

1. Download the image of your choice from the [release page of the github repository](https://github.com/PALs-Software/DigitalAssistant/releases). 
2. Install and start the [Raspberry Pi Imager](https://www.raspberrypi.com/software/)
3. Choose your Raspberry Pi Modell
4. Under OS choose "Use Custom" and select the downloaded image
5. Select the sd-card
6. Preconfigure the RPI with username, password and hostname and if needed configure the Wifi Setings, in the configuration dialog

Further information's how to connect the client with the server can be found in the [setup client](../setup/clients.md) chapter of this documentation.

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
