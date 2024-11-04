# Update your system to the newest version

## Manual installation or raspberry pi image
1. Download the newest version from the [release page of the github repository](https://github.com/PALs-Software/DigitalAssistant/releases).
2. Replace the binary files in the installation folder with the new files.
   1. Make sure the configuration files like `appsettings*.json` and `web.config` files are not replaced. These files must remain the same.

## Docker
Just delete the running container and image and pull the newest image of the digital assistant. After recreating the container with the command mentioned in the installation chapter, your system will now use the newest version of the digital assistant. All necessary data are saved in a seperate volume in docker, which the new version will reuse.