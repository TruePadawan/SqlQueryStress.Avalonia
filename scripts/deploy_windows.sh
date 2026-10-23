#!/bin/bash

# Resolve the project root directory (parent of the scripts/ directory)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

# Clean up
rm -rf ./out/

# Ensure the releases folder exists
mkdir -p ./releases/windows

VERSION="${1:-0.1.0}"

# .NET publish
# self-contained is recommended, so final users won't need to install .NET
dotnet publish "./SqlQueryStress.Desktop/SqlQueryStress.Desktop.csproj" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime win-x64 \
  --output "./out/win-x64" \
  /p:PublishSingleFile=true

zip -j "./releases/windows/SqlQueryStress-${VERSION}.zip" ./out/win-x64/*
