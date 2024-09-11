# docker build -t digital-assistant-server -f ./DigitalAssistant/Scripts/Build/Dockerfile .
# docker create --name digital-assistant-server digital-assistant-server
# docker start digital-assistant-server

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ENV ASPNETCORE_URLS=http://+:8079;https://+:8080
USER app
WORKDIR /app
EXPOSE 8079
EXPOSE 8080

# Install needed modules
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS modules
WORKDIR /src

RUN apt update && apt install -y make
RUN apt install -y build-essential
RUN apt install -y openssl
RUN apt install -y tree

# Copy needed project files
FROM modules AS copy-files
COPY ./DigitalAssistant/ ./DigitalAssistant
COPY ./BlazorBase/ ./BlazorBase

# Build wrapper and copy files
FROM copy-files AS build-wrapper
WORKDIR /src/DigitalAssistant/Libs/PhonemizeESpeakWrapper
RUN make export_x86_64
RUN mkdir -p /app/publish/
RUN mkdir -p /app/publish-connectors/
RUN mkdir -p /app/publish-commands/
RUN cp ./Export/* /app/publish/

# Create ssl certificates
FROM build-wrapper AS create-certificates
WORKDIR /src/DigitalAssistant/Scripts/Build
RUN /bin/bash -c 'source /src/DigitalAssistant/Scripts/Build/Assets/docker-create-certificates.sh'
RUN mkdir -p /app/publish/Certificates/
RUN cp ./digitalassistant-ssl.crt /app/publish/Certificates/digitalassistant-ssl.crt
RUN cp ./digitalassistant-ssl.key /app/publish/Certificates/digitalassistant-ssl.key

RUN chown root:app /app/publish/Certificates/digitalassistant-ssl.crt
RUN chown root:app /app/publish/Certificates/digitalassistant-ssl.key
RUN chmod 640 /app/publish/Certificates/digitalassistant-ssl.key
RUN ls -l /app/publish/Certificates

# Publish connector projects
FROM create-certificates AS build-connectors
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/DigitalAssistant/Connectors/DigitalAssistant.HueConnector
RUN dotnet publish "./DigitalAssistant.HueConnector.csproj" -v normal -c $BUILD_CONFIGURATION -o /app/publish-connectors /p:UseAppHost=false

# Publish command projects
FROM build-connectors AS build-commands
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/DigitalAssistant/Commands
RUN dotnet publish "./DigitalAssistant.CoreCommands/DigitalAssistant.CoreCommands.csproj" -v normal -c $BUILD_CONFIGURATION -o /app/publish-commands /p:UseAppHost=false
RUN dotnet publish "./DigitalAssistant.DeviceCommands/DigitalAssistant.DeviceCommands.csproj" -v normal -c $BUILD_CONFIGURATION -o /app/publish-commands /p:UseAppHost=false
RUN dotnet publish "./DigitalAssistant.MusicCommands/DigitalAssistant.MusicCommands.csproj" -v normal -c $BUILD_CONFIGURATION -o /app/publish-commands /p:UseAppHost=false

# Publish digital assistant server project
FROM build-commands AS build-server
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/DigitalAssistant/Core/DigitalAssistant.Server
RUN dotnet publish "./DigitalAssistant.Server.csproj" -v normal -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

RUN cp /src/DigitalAssistant/Scripts/Build/Assets/docker-server-appsettings.json /app/publish/appsettings.json
RUN cp -r /src/DigitalAssistant/Core/DigitalAssistant.Server/bin/Release/net8.0/Connectors /app/publish
RUN cp -r /src/DigitalAssistant/Core/DigitalAssistant.Server/bin/Release/net8.0/Commands /app/publish

RUN rm /app/publish/appsettings.Development.json
RUN rm /app/publish/appsettings.Linux.json
RUN rm /app/publish/appsettings.User.json

RUN ls /app/publish/
RUN tree /app/publish/Connectors
RUN tree /app/publish/Commands

FROM base AS final
WORKDIR /app
COPY --from=build-server /app/publish .

ENTRYPOINT ["dotnet", "DigitalAssistant.Server.dll"]