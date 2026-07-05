#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${SCRIPT_DIR}/dotnet-env.sh"

"${DOTNET_BIN}" run --project src/NewGreen.App/SiteSnap.App.csproj
