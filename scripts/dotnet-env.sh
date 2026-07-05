#!/usr/bin/env bash
set -euo pipefail

if command -v dotnet >/dev/null 2>&1; then
  DOTNET_BIN="$(command -v dotnet)"
elif [ -x /usr/local/share/dotnet/dotnet ]; then
  DOTNET_BIN="/usr/local/share/dotnet/dotnet"
else
  echo "dotnet CLI를 찾을 수 없습니다. .NET SDK를 설치하거나 PATH를 확인하세요." >&2
  exit 127
fi

export DOTNET_BIN
