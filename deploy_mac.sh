#!/usr/bin/env bash
set -e

# Default to 1.0.0 if no version argument is passed
APP_VERSION="$1"
if [ -z "$APP_VERSION" ]; then
  APP_VERSION="1.0.0"
fi

RIDS=("osx-arm64" "osx-x64")
mkdir -p releases/macos

# Download standalone rcodesign binary for Linux if not already cached
RCODESIGN_BIN="/tmp/rcodesign"
if [ ! -f "$RCODESIGN_BIN" ]; then
  echo "Downloading rcodesign for macOS ad-hoc bundle signing..."
  curl -sSL https://github.com/indygreg/apple-platform-rs/releases/download/apple-codesign%2F0.29.0/apple-codesign-0.29.0-x86_64-unknown-linux-musl.tar.gz \
    | tar -xz -C /tmp --strip-components=1 apple-codesign-0.29.0-x86_64-unknown-linux-musl/rcodesign
  chmod +x "$RCODESIGN_BIN"
fi

for RID in "${RIDS[@]}"; do
  echo "=========================================="
  echo " Packaging SqlQueryStress for $RID"
  echo " Target Version: $APP_VERSION"
  echo "=========================================="

  STAGING_DIR="bin/staging-$RID"
  APP_DIR="$STAGING_DIR/SqlQueryStress.app"

  # Clean any stale staging build
  rm -rf "$STAGING_DIR"

  # 1. Build .app bundle directory layout FIRST
  mkdir -p "$APP_DIR/Contents/MacOS"
  mkdir -p "$APP_DIR/Contents/Resources"

  # 2. Publish Desktop executable directly into Contents/MacOS
  dotnet publish SqlQueryStress.Desktop/SqlQueryStress.Desktop.csproj \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishTrimmed=false \
    -p:Version="$APP_VERSION" \
    -o "$APP_DIR/Contents/MacOS"

  # 3. Rename the native apphost executable to match CFBundleExecutable (SqlQueryStress)
  mv "$APP_DIR/Contents/MacOS/SqlQueryStress.Desktop" "$APP_DIR/Contents/MacOS/SqlQueryStress"
  chmod +x "$APP_DIR/Contents/MacOS/SqlQueryStress"
  chmod +x "$APP_DIR/Contents/MacOS/"*.dylib 2>/dev/null || true

  # Optional: Remove .pdb debug files to keep the zip lightweight
  rm -f "$APP_DIR/Contents/MacOS/"*.pdb

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

  # 6. Ad-hoc sign the entire .app bundle so macOS Gatekeeper doesn't report it as "damaged"
  echo "Signing $APP_DIR with rcodesign..."
  "$RCODESIGN_BIN" sign "$APP_DIR"

  # 7. Archive bundle into .zip for GitHub Release distribution
  ARCHIVE_NAME="SqlQueryStress-${APP_VERSION}-${RID}.zip"
  (cd "$STAGING_DIR" && zip -ry "../../releases/macos/$ARCHIVE_NAME" "SqlQueryStress.app")
  echo "Created: releases/macos/$ARCHIVE_NAME"
done

echo "------------------------------------------"
echo "All macOS packages generated in releases/macos/"