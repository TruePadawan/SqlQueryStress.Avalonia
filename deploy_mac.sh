#!/usr/bin/env bash
set -e

# Default to 1.0.0 if no version argument is passed
APP_VERSION="$1"
if [ -z "$APP_VERSION" ]; then
  APP_VERSION="1.0.0"
fi

RIDS=("osx-arm64" "osx-x64")
mkdir -p releases/macos

for RID in "${RIDS[@]}"; do
  echo "=========================================="
  echo " Packaging SqlQueryStress for $RID"
  echo " Target Version: $APP_VERSION"
  echo "=========================================="

  STAGING_DIR="bin/staging-$RID"
  APP_DIR="$STAGING_DIR/SqlQueryStress.app"

  # Clean any stale staging build
  rm -rf "$STAGING_DIR"

  # 1. Publish Desktop executable
  dotnet publish SqlQueryStress.Desktop/SqlQueryStress.Desktop.csproj \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:Version="$APP_VERSION" \
    -o "$STAGING_DIR/bin"

  # 2. Build .app bundle directory layout FIRST
  mkdir -p "$APP_DIR/Contents/MacOS"
  mkdir -p "$APP_DIR/Contents/Resources"

  # 3. Copy and rename the binary into the .app bundle
  cp "$STAGING_DIR/bin/SqlQueryStress.Desktop" "$APP_DIR/Contents/MacOS/SqlQueryStress"
  chmod +x "$APP_DIR/Contents/MacOS/SqlQueryStress"

  # 4. Dynamic Version Replacement into Info.plist using sed
  if [ -f packaging/macos/Info.plist ]; then
    echo "Injecting version $APP_VERSION into Info.plist..."
    sed "s/__VERSION__/$APP_VERSION/g" packaging/macos/Info.plist > "$APP_DIR/Contents/Info.plist"
  else
    echo "Warning: packaging/macos/Info.plist not found! Skipping..."
  fi

  # 5. Copy Icon
  if [ -f packaging/macos/SqlQueryStress.icns ]; then
    cp packaging/macos/SqlQueryStress.icns "$APP_DIR/Contents/Resources/SqlQueryStress.icns"
  else
    echo "Warning: packaging/macos/SqlQueryStress.icns not found! Skipping..."
  fi

  # 6. Archive bundle into .zip for GitHub Release distribution
  ARCHIVE_NAME="SqlQueryStress-${APP_VERSION}-${RID}.zip"
  (cd "$STAGING_DIR" && zip -ry "../../releases/macos/$ARCHIVE_NAME" "SqlQueryStress.app")
  echo "Created: releases/macos/$ARCHIVE_NAME"
done

echo "------------------------------------------"
echo "All macOS packages generated in releases/macos/"