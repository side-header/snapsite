$ErrorActionPreference = "Stop"

dotnet publish src/NewGreen.App/SiteSnap.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o build/windows-x64
