#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${SCRIPT_DIR}/dotnet-env.sh"

"${DOTNET_BIN}" publish src/SnapSite.App/SiteSnap.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o build/windows-x64
