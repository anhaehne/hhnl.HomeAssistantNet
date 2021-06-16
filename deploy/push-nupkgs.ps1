dotnet pack ../src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.Automations/hhnl.HomeAssistantNet.Automations.csproj -o .
dotnet pack ../src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.Shared/hhnl.HomeAssistantNet.Shared.csproj -o .
dotnet pack ../src/hhnl.HomeAssistantNet/hhnl.HomeAssistantNet.Generator/hhnl.HomeAssistantNet.Generator.csproj -o .
dotnet nuget push "*.nupkg" --source nuget.org --skip-duplicate
Get-ChildItem $Path | Where-Object { $_.Name -Match ".*\.nupkg" } | Remove-Item