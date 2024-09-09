#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DigitalAssistant.Server/DigitalAssistant.Server.csproj", "DigitalAssistant.Server/"]
COPY ["../BlazorBase/BlazorBase.AudioRecorder/BlazorBase.AudioRecorder.csproj", "../BlazorBase/BlazorBase.AudioRecorder/"]
COPY ["../BlazorBase/BlazorBase.CRUD/BlazorBase.CRUD.csproj", "../BlazorBase/BlazorBase.CRUD/"]
COPY ["../BlazorBase/BlazorBase.MessageHandling/BlazorBase.MessageHandling.csproj", "../BlazorBase/BlazorBase.MessageHandling/"]
COPY ["../BlazorBase/BlazorBase/BlazorBase.csproj", "../BlazorBase/BlazorBase/"]
COPY ["../BlazorBase/BlazorBase.Files/BlazorBase.Files.csproj", "../BlazorBase/BlazorBase.Files/"]
COPY ["../BlazorBase/BlazorBase.Backup/BlazorBase.Backup.csproj", "../BlazorBase/BlazorBase.Backup/"]
COPY ["../BlazorBase/BlazorBase.DataUpgrade/BlazorBase.DataUpgrade.csproj", "../BlazorBase/BlazorBase.DataUpgrade/"]
COPY ["../BlazorBase/BlazorBase.Mailing/BlazorBase.Mailing.csproj", "../BlazorBase/BlazorBase.Mailing/"]
COPY ["../BlazorBase/BlazorBase.RecurringJobQueue/BlazorBase.RecurringBackgroundJobQueue.csproj", "../BlazorBase/BlazorBase.RecurringJobQueue/"]
COPY ["../BlazorBase/BlazorBase.Server/BlazorBase.Server.csproj", "../BlazorBase/BlazorBase.Server/"]
COPY ["../BlazorBase/BlazorBase.Themes/BlazorBase.Themes.csproj", "../BlazorBase/BlazorBase.Themes/"]
COPY ["../BlazorBase/BlazorBase.User/BlazorBase.User.csproj", "../BlazorBase/BlazorBase.User/"]
COPY ["Libs/Quickenshtein/Quickenshtein.csproj", "Libs/Quickenshtein/"]
COPY ["Libs/TextToSpeech/TextToSpeech.csproj", "Libs/TextToSpeech/"]
COPY ["Libs/PhonemizeESpeakWrapper/PhonemizeESpeakWrapper.vcxproj", "Libs/PhonemizeESpeakWrapper/"]
RUN dotnet restore "./DigitalAssistant.Server/DigitalAssistant.Server.csproj"
COPY . .
WORKDIR "/src/DigitalAssistant.Server"
RUN dotnet build "./DigitalAssistant.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DigitalAssistant.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DigitalAssistant.Server.dll"]