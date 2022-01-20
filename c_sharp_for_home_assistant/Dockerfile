FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app
EXPOSE 8099

ENV ASPNETCORE_URLS=http://+:8099;http://+:20777

# Copy project template
COPY src/hhnl.HomeAssistantNet/ProjectTemplate/ ProjectTemplate/

# Configure supervisor
ENV SupervisorConfig__DeployDirectory=/config/c-sharp-for-homeassistant/deploy/
ENV SupervisorConfig__SourceDirectory=/config/c-sharp-for-homeassistant/source/
ENV SupervisorConfig__ConfigDirectory=/config/c-sharp-for-homeassistant/config/
ENV SupervisorConfig__BuildDirectory=/temp/build/
ENV FileLogging__PathFormat=/config/c-sharp-for-homeassistant/logs/{Date}.log

# Configure supervisor client
ENV SupervisorUrl=http://localhost:8099
ENV IsLocal=true

# Enable debug log
ENV Logging__LogLevel__Default=Debug

# Build CSharpForHomeAssistant
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.CSharpForHomeAssistant/hhnl.HomeAssistantNet.CSharpForHomeAssistant.csproj", "hhnl.HomeAssistantNet.CSharpForHomeAssistant/"]
RUN dotnet restore "hhnl.HomeAssistantNet.CSharpForHomeAssistant/hhnl.HomeAssistantNet.CSharpForHomeAssistant.csproj"
COPY src/hhnl.HomeAssistantNet .
WORKDIR "/src/hhnl.HomeAssistantNet.CSharpForHomeAssistant"
#RUN dotnet build "hhnl.HomeAssistantNet.CSharpForHomeAssistant.csproj" -c Release -o /app/build
RUN dotnet build "hhnl.HomeAssistantNet.CSharpForHomeAssistant.csproj" -o /app/build

# Publish
FROM build AS publish
#RUN dotnet publish "hhnl.HomeAssistantNet.CSharpForHomeAssistant.csproj" -c Release -o /app/publish
RUN dotnet publish "hhnl.HomeAssistantNet.CSharpForHomeAssistant.csproj" -o /app/publish

# Copy to final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "hhnl.HomeAssistantNet.CSharpForHomeAssistant.dll"]
