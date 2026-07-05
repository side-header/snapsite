$ErrorActionPreference = "Stop"

dotnet publish src/SnapSite.App/SiteSnap.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o build/windows-x64
