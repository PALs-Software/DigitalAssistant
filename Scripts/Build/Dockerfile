# docker build -t palssoftware/digital-assistant-server -f ./DigitalAssistant/Scripts/Build/Dockerfile .
# docker run --name digital-assistant-server -p 8079:8079 -p 8080:8080 -v DigitalAssistantServer_Data:/app/DigitalAssistantServer -v DigitalAssistantServer_Keys:/home/app/.aspnet/DataProtection-Keys -d palssoftware/digital-assistant-server

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ENV ASPNETCORE_URLS=http://+:8079;https://+:8080
USER app
WORKDIR /app
EXPOSE 8079
EXPOSE 8080

# Install needed modules
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS modules
WORKDIR /src

RUN apt update
RUN apt install -y openssl
RUN apt install -y tree

# Copy needed project files
FROM modules AS copy-files
COPY ./DigitalAssistant/ ./DigitalAssistant
COPY ./BlazorBase/ ./BlazorBase

# Create ssl certificates
FROM copy-files AS create-certificates
WORKDIR /src/DigitalAssistant/Scripts/Build
RUN /bin/bash -c 'source /src/DigitalAssistant/Scripts/Build/Assets/docker-create-certificates.sh'
RUN mkdir -p /app/publish/DigitalAssistantServer/Certificates/
RUN mkdir -p /app/publish/DigitalAssistantServer/Database/
RUN cp ./digitalassistant-ssl.crt /app/publish/DigitalAssistantServer/Certificates/digitalassistant-ssl.crt
RUN cp ./digitalassistant-ssl.key /app/publish/DigitalAssistantServer/Certificates/digitalassistant-ssl.key

RUN chown -R app:app /app/publish/DigitalAssistantServer
RUN chown root:app /app/publish/DigitalAssistantServer/Certificates/digitalassistant-ssl.crt
RUN chown root:app /app/publish/DigitalAssistantServer/Certificates/digitalassistant-ssl.key
RUN chmod 640 /app/publish/DigitalAssistantServer/Certificates/digitalassistant-ssl.key
RUN ls -l /app/publish/DigitalAssistantServer
RUN ls -l /app/publish/DigitalAssistantServer/Certificates

# Publish connector projects
FROM create-certificates AS build-connectors
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/DigitalAssistant/Connectors/DigitalAssistant.HueConnector
RUN dotnet publish "./DigitalAssistant.HueConnector.csproj" -v normal -c $BUILD_CONFIGURATION -o /app/publish-misc /p:UseAppHost=false

# Publish command projects
FROM build-connectors AS build-commands
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/DigitalAssistant/Commands
RUN dotnet publish "./DigitalAssistant.CoreCommands/DigitalAssistant.CoreCommands.csproj" -v normal -c $BUILD_CONFIGURATION -o /app/publish-misc /p:UseAppHost=false
RUN dotnet publish "./DigitalAssistant.DeviceCommands/DigitalAssistant.DeviceCommands.csproj" -v normal -c $BUILD_CONFIGURATION -o /app/publish-misc /p:UseAppHost=false
RUN dotnet publish "./DigitalAssistant.MusicCommands/DigitalAssistant.MusicCommands.csproj" -v normal -c $BUILD_CONFIGURATION -o /app/publish-misc /p:UseAppHost=false

# Publish digital assistant server project
FROM build-commands AS build-server
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/DigitalAssistant/Core/DigitalAssistant.Server
RUN dotnet publish "./DigitalAssistant.Server.csproj" -v normal -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

RUN cp /src/DigitalAssistant/Scripts/Build/Assets/docker-server-appsettings.json /app/publish/appsettings.Linux.json
RUN cp -r /app/publish-misc/Publish/* /app/publish

RUN rm /app/publish/appsettings.Development.json
RUN rm /app/publish/appsettings.User.json

RUN ls /app/publish/
RUN tree /app/publish/Connectors
RUN tree /app/publish/Commands

# Create database
FROM build-server AS create-database
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/DigitalAssistant/Core/DigitalAssistant.Server
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 8.0.8
RUN dotnet ef database update --context SQLiteDbContext --configuration $BUILD_CONFIGURATION --no-build --connection "Data Source=/app/publish/DigitalAssistantServer/Database/DigitalAssistantServer.db"
RUN chown -R app:app /app/publish/DigitalAssistantServer/Database
RUN ls -l /app/publish/DigitalAssistantServer/Database
RUN ls -l /app/publish/DigitalAssistantServer

FROM base AS final
WORKDIR /app
COPY --from=create-database /app/publish .
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys/
RUN chown -R app:app /home/app/.aspnet/
RUN ls -l /home/app/.aspnet/

ENTRYPOINT ["dotnet", "DigitalAssistant.Server.dll"]