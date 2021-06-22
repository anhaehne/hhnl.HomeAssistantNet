dotnet build ../src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.Automations/hhnl.HomeAssistantNet.Automations.csproj -c Release --force
dotnet build ../src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.Shared/hhnl.HomeAssistantNet.Shared.csproj -c Release --force
dotnet build ../src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.Generator/hhnl.HomeAssistantNet.Generator.csproj -c Release --force
dotnet pack ../src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.Automations/hhnl.HomeAssistantNet.Automations.csproj -c Release -o .
dotnet pack ../src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.Shared/hhnl.HomeAssistantNet.Shared.csproj -c Release -o .
dotnet pack ../src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.Generator/hhnl.HomeAssistantNet.Generator.csproj -c Release -o .
dotnet nuget push "*.nupkg" --source nuget.org --skip-duplicate
Get-ChildItem $Path | Where-Object { $_.Name -Match ".*\.nupkg" } | Remove-Item