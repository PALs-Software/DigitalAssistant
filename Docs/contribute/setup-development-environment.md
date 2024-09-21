# Setup development environment

## Clone Repositories

### Clone BlazorBase

1. Clone the [BlazorBase](https://github.com/PALs-Software/BlazorBase) repository.
2. Switch to the **develop** branch.
3. Open the `BlazorBase.sln` project in **Visual Studio** or **Visual Studio Code**.
4. Build the complete solution.

## Clone DigitalAssistant

1. [**On Windows Host only**]: Make sure that symlink support is in git enabled. Otherwise created symlinks from some library files will be lost by cloning and for example a linux docker container can not be build.
   - git config core.symlinks true
   - git config --global core.symlinks true
2. Clone the [DigitalAssistant](https://github.com/PALs-Software/DigitalAssistant) repository.

> [!NOTE]
> The cloned DigitalAssistant project must be in the same root folder as the cloned BlazorBase project.

## Configure and Run the Server

1. Open the `DigitalAssistant.sln` project in **Visual Studio** or **Visual Studio Code**.
2. Copy the `Core/DigitalAssistant.Server/appsettings.{YourPlattform}.json` file to `Core/DigitalAssistant.Server/appsettings.User.json`.
3. Change the `appsettings.User.json` file according to your needs.
4. A self signed certificate must be created to run the application under http. Also the path to the certificate must be configured in the `appsettings.User.json` file.
   - For Windows Visual Studio will create a localhost developer certificate itself. If you want to use a custom certificate create one using the script `Scripts/Certificate/Windows/CreateSelfSignedCertificate.ps1`
   - For Linux there is a script `Scripts/Certificate/Linux/CreateSelfSignedCertificate.sh` which will create a certificate and export it to /etc/ssl/digitalassistant/ssl.crt and /etc/ssl/digitalassistant/ssl.key
5. Run the **DigitalAssistant.Server** project.

## Configure and Run the Client

1. Open the `DigitalAssistant.sln` project in **Visual Studio** or **Visual Studio Code**.
2. Copy the `Core/DigitalAssistant.Client/appsettings.json` file or the `Core/DigitalAssistant.Client/appsettings.Linux.json` to `Core/DigitalAssistant.Client/appsettings.User.json`.
3. Change the `appsettings.User.json` file according to your needs.
4. Run the **DigitalAssistant.Client** project.
