# SiteSnap

SiteSnap is a C# + Avalonia desktop app for sorting photos by work phase (`before / progress / after`) and exporting them as `docx` and `hwpx` documents.

## Development Run

Development runs require the .NET 10 SDK.

Run on macOS or Windows:

```bash
./scripts/run-avalonia.sh
```

## Build

Release publishing requires the .NET 10 SDK.

Publish for macOS:

```bash
./scripts/publish-macos-arm64.sh
```

Publish for Windows:

```bash
./scripts/publish-windows-x64.sh
```

On Windows PowerShell, you can also use:

```powershell
.\scripts\publish-windows-x64.ps1
```

## Run Published App

Publishing creates an OS-specific output folder.

macOS:

```bash
./build/macos-arm64/SiteSnap
```

Windows:

```text
build/windows-x64/SiteSnap.exe
```

The app is published with `--self-contained true`, so end-user PCs do not need a separate .NET Runtime or SDK installation. The development machine that runs publish still needs the .NET 10 SDK.

The Windows build is published as a single executable, so you can usually send only `build/windows-x64/SiteSnap.exe` to a Windows user. Extra files such as `.pdb` may be generated, but they are not required for normal execution.

## Workflow

1. Select the base folder with `폴더 열기`.
2. Create a work category with `공종 추가`.
3. Select photos from the unclassified photo area.
4. Use the `전`, `중`, and `후` buttons in the right-side group area to classify photos.
5. Save the working state to `sitesnape_manifest.json` with `저장하기`.
6. Export files as `exports/sitesnap-yyMMddHHmmss.docx` and `exports/sitesnap-yyMMddHHmmss.hwpx` with `내보내기`.

## Cache Location

SiteSnap stores generated photo thumbnails in a disk cache. The `캐시 클리어` menu removes the cache below.

| OS | Thumbnail cache path |
| --- | --- |
| macOS | `~/Library/Caches/SiteSnap/thumbnails` |
| Windows | `%LOCALAPPDATA%\\SiteSnap\\Cache\\thumbnails` |
| Linux/other | `$XDG_CACHE_HOME/SiteSnap/thumbnails` or `SiteSnap/Cache/thumbnails` under the local app-data folder |

## Documents

- [Requirements](docs/requirements.md)
- [Architecture](docs/architecture.md)
- [Manifest JSON](docs/manifest.md)
