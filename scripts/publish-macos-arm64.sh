#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${SCRIPT_DIR}/dotnet-env.sh"

rm -rf build/macos-arm64
"${DOTNET_BIN}" publish src/NewGreen.App/SiteSnap.App.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=false -o build/macos-arm64
