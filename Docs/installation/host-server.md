# Host Server

The Digital Assistant Server can be installed on any device, your desktop PC, a server or a single-board computer such as the Raspberry Pi in different ways, for example as independent docker container. Decide for yourself which of the installation paths you like best.

## Installation paths

### Docker

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/).
2. Use the integrated terminal in Docker Desktop or open another command terminal of your choice.
3. Execute the following command. This will download and run the digital assistant server application inside a docker container.

```
docker run --name digital-assistant-server -p 8079:8079 -p 8080:8080 -v DigitalAssistantServer_Data:/app/DigitalAssistantServer -v DigitalAssistantServer_Keys:/home/app/.aspnet/DataProtection-Keys -d palssoftware/digital-assistant-server

```

4. Open `https://localhost:8080/` in a browser and enjoy the digital assistant. Further information's how to configure and use the digital assistant can be found in the [setup](../setup/setup.md) chapter of this documentation.

### Manually

1. Download and install the [**ASP.NET Core Runtime 8.\***](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) for your platform type.
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
4. **Optional**: Change the default configuration of the server by adjusting the `appsettings.{YourPlattform}.json` file like it is explained in the chapter "[Change default configuration](#change-default-configuration)".
5. Create a self signed certificate for the server like described in [this chapter](#create-self-signed-certificate-for-the-server).
6. Open a terminal of your choice.
7. Navigate to the extracted binary files.
8. Run the following command to start the server application: 
``` shell
dotnet DigitalAssistant.Server.dll --urls="http://localhost:8079/;https://localhost:8080/"
```
9. Open [https://localhost:8080/](https://localhost:8080/) in a browser and enjoy the digital assistant. Further information's how to configure and use the digital assistant can be found in the [setup](../setup/setup.md) chapter of this documentation.
11. Use tools depending on your operating system to auto start the application at statup of your machine.
    
### Raspberry Pi Image
1. Download the image of your choice from the [release page of the github repository](https://github.com/PALs-Software/DigitalAssistant/releases). 
2. Install and start the [Raspberry Pi Imager](https://www.raspberrypi.com/software/)
3. Choose your Raspberry Pi Modell
4. Under OS choose "Use Custom" and select the downloaded image
5. Select the sd-card
6. Preconfigure the RPI with username, password and hostname and if needed configure the Wifi Setings, in the configuration dialog
   - Note, that the preconfigured certificate for the server application is created with the hostname "digital-assistant-pi". If you choose another hostname, you must recreate the certificate like descripted in [this chapter](#create-self-signed-certificate-for-the-server).

Further information's how to connect the client with the server can be found in the [setup client](../setup/clients.md) chapter of this documentation.

### [Windows only] Host with Internet Information Service (IIS)
Under Windows, the server application can also be executed with another web server, the Internet Information Service, as an alternative to the integrated Kestrel. To set this up, follow the described steps:

1. Enable the IIS feature in the Windows-Feature Management.
2. Download and install the [**ASP.NET Core Runtime 8.\* Hosting Bundle**](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
3. Create a new website over the wizard of the IIS manager.
4. Download the binaries of your platform from the [release page of the github repository](https://github.com/PALs-Software/DigitalAssistant)
5. Extract the compressed files to the newly created website folder.
6. **Optional**: Change the default configuration of the server by adjusting the `appsettings.{YourPlattform}.json` file like it is explained in the chapter "[Change default configuration](#change-default-configuration)".
7. Create a self signed certificate in the IIS manager for the new website.
8. Make sure the option "Load User Profile" is enabled in the advanced settings of the application pool used for the website.
9.  Open configured address of the website in a browser and enjoy the digital assistant. Further information's how to configure and use the digital assistant can be found in the [setup](../setup/setup.md) chapter of this documentation.

## Create self signed certificate for the server

The following scripts will create a self signed certificate for the server, so it can be accessed under the secure https protocol. If you modify the script, note that maybe the certificate paths in the `appsettings.{YourPlattform}.json` of the server must be changed too.

> [!NOTE]
> Note that depending on your browser self signed certificates are marked in your browser as not trustworthy. Either mark the certificate as trustworthy in your operating system or ignore the warning when you initially open the website.

### Linux

Download and execute the shell script [CreateSelfSignedCertificate.sh](https://github.com/PALs-Software/DigitalAssistant/blob/main/Scripts/Certificate/Linux/CreateSelfSignedCertificate.sh).

#### Parameter
 - User Name: The user which should run the application later. In the most cases simple your username.
 - Website Address: The address for which the certificate should be created. For the beginning localhost or the computer name are just fine.
 
``` shell
wget https://github.com/PALs-Software/DigitalAssistant/raw/main/Scripts/Certificate/Linux/CreateSelfSignedCertificate.sh
chmod +x CreateSelfSignedCertificate.sh
sudo ./CreateSelfSignedCertificate.sh
```

### Windows

Download and execute the powershell script [CreateSelfSignedCertificate.ps1](https://github.com/PALs-Software/DigitalAssistant/blob/main/Scripts/Certificate/Windows/CreateSelfSignedCertificate.ps1).

### Mac

Create a self signed certificate as described in this [tutorial](https://support.apple.com/en-US/guide/keychain-access/kyca8916/mac).

## Change default configuration

The default configuration of the server application can be changed in the `appsettings.{YourPlattform}.json` file.

### Database

By default a SQLite Database will be used in the server application. The sql provider can be changed by adjusting the `DatabaseProvider` property in the `appsettings.{YourPlattform}.json` file. Currently possible provider values are:

- 'SQLite' to use a SQLite Database
- 'MSSQL' to use a Microsoft SQL Server

The connection string to the database can be specified in the `DefaultConnection` property.

### Webserver certificate

The used certificate for the kestrel webserver can be specified in the `Kestrel` -> `Certificates` properties. Depending on your platform different properties can be used to specify the certificate. For example with Location, Store and Subject can a certificate be specified located into the internal certificate storage of the operating system or with Path and KeyPath a certificate located in the local storage of the device.

### Client connection settings

The connection to the clients can be configured in the `ClientConnection` property. For that the used port can be specified and also the used certificate can be specified exactly like the webserver certificate explained [here](#webserver-certificate).

### Storage location

The location of the files downloaded and used from the server application can be specified with the following three properties: `FileStorePath`, `TempFileStorePath` and `ModelsDirectoryPath`.

## Firewall

If the server is to be used in the entire local network or even on the Internet, certain ports must be opened. By default, the web server runs with ports 8079 and 8080 which must be opened for TCP and the client connection with port 59142 with TCP and UDP.