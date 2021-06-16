FROM mcr.microsoft.com/dotnet/sdk:5.0 AS base
WORKDIR /app
EXPOSE 8099

ENV ASPNETCORE_URLS=http://+:8099;http://+:20777

# Copy project template
COPY src/hhnl.HomeAssistantNet/ProjectTemplate/ ProjectTemplate/

# Configure supervisor
ENV SupervisorConfig__DeployDirectory=/config/deploy/
ENV SupervisorConfig__SourceDirectory=/config/source/

# Configure supervisor client
ENV SupervisorUrl=http://localhost:8099
ENV IsLocal=true

# Build CSharpForHomeAssistant
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.CSharpForHomeAssistant/hhnl.HomeAssistantNet.CSharpForHomeAssistant.csproj", "hhnl.HomeAssistantNet.CSharpForHomeAssistant/"]
RUN dotnet restore "hhnl.HomeAssistantNet.CSharpForHomeAssistant/hhnl.HomeAssistantNet.CSharpForHomeAssistant.csproj"
COPY src/hhnl.HomeAssistantNet .
WORKDIR "/src/hhnl.HomeAssistantNet.CSharpForHomeAssistant"
RUN dotnet build "hhnl.HomeAssistantNet.CSharpForHomeAssistant.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "hhnl.HomeAssistantNet.CSharpForHomeAssistant.csproj" -c Release -o /app/publish

# Copy to final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "hhnl.HomeAssistantNet.CSharpForHomeAssistant.dll"]
