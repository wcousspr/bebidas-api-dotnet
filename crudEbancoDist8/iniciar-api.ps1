$projeto = ".\crudEbancoDist8\crudEbancoDist8.csproj"
$url = "https://localhost:7113/swagger"

Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "dotnet run --project `"$projeto`" --launch-profile https"
)

Start-Sleep -Seconds 4
Start-Process $url