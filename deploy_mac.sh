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
  # NOTE: PublishSingleFile is NOT used because it does not bundle native libraries
  # like libSkiaSharp.dylib. Instead, we publish normally and copy all files.
  dotnet publish SqlQueryStress.Desktop/SqlQueryStress.Desktop.csproj \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishTrimmed=false \
    -p:Version="$APP_VERSION" \
    -o "$STAGING_DIR/bin"

  # 2. Build .app bundle directory layout FIRST
  mkdir -p "$APP_DIR/Contents/MacOS"
  mkdir -p "$APP_DIR/Contents/Resources"

  # 3. Copy the main binary into the .app bundle
  cp "$STAGING_DIR/bin/SqlQueryStress.Desktop" "$APP_DIR/Contents/MacOS/SqlQueryStress"
  chmod +x "$APP_DIR/Contents/MacOS/SqlQueryStress"

  # 4. Copy all native libraries (.dylib) into MacOS folder alongside the executable
  # .NET resolves native libraries relative to the executable path, so they must be
  # in the same directory as the main binary for SkiaSharp to load correctly.
  if ls "$STAGING_DIR/bin"/*.dylib 1>/dev/null 2>&1; then
    cp "$STAGING_DIR/bin"/*.dylib "$APP_DIR/Contents/MacOS/"
    echo "Copied native .dylib libraries to MacOS folder."
  else
    echo "Warning: No .dylib files found in publish output!"
  fi

  # 5. Copy all managed DLLs and runtime files into MacOS folder alongside the executable
  cp "$STAGING_DIR/bin"/*.dll "$APP_DIR/Contents/MacOS/" 2>/dev/null || true
  cp "$STAGING_DIR/bin"/*.pdb "$APP_DIR/Contents/MacOS/" 2>/dev/null || true
  cp "$STAGING_DIR/bin"/*.xml "$APP_DIR/Contents/MacOS/" 2>/dev/null || true
  cp "$STAGING_DIR/bin"/*.json "$APP_DIR/Contents/MacOS/" 2>/dev/null || true
  cp "$STAGING_DIR/bin"/*.config "$APP_DIR/Contents/MacOS/" 2>/dev/null || true
  cp "$STAGING_DIR/bin"/*.sql "$APP_DIR/Contents/MacOS/" 2>/dev/null || true
  # Copy any additional resource directories (e.g. runtimes, resources)
  if [ -d "$STAGING_DIR/bin/runtimes" ]; then
    cp -r "$STAGING_DIR/bin/runtimes" "$APP_DIR/Contents/MacOS/"
  fi
  if [ -d "$STAGING_DIR/bin/resources" ]; then
    cp -r "$STAGING_DIR/bin/resources" "$APP_DIR/Contents/MacOS/"
  fi

  # 6. Dynamic Version Replacement into Info.plist using sed
  if [ -f packaging/macos/Info.plist ]; then
    echo "Injecting version $APP_VERSION into Info.plist..."
    sed "s/__VERSION__/$APP_VERSION/g" packaging/macos/Info.plist > "$APP_DIR/Contents/Info.plist"
  else
    echo "Warning: packaging/macos/Info.plist not found! Skipping..."
  fi

  # 7. Copy Icon
  if [ -f packaging/macos/SqlQueryStress.icns ]; then
    cp packaging/macos/SqlQueryStress.icns "$APP_DIR/Contents/Resources/SqlQueryStress.icns"
  else
    echo "Warning: packaging/macos/SqlQueryStress.icns not found! Skipping..."
  fi

  # 8. Archive bundle into .zip for GitHub Release distribution
  ARCHIVE_NAME="SqlQueryStress-${APP_VERSION}-${RID}.zip"
  (cd "$STAGING_DIR" && zip -ry "../../releases/macos/$ARCHIVE_NAME" "SqlQueryStress.app")
  echo "Created: releases/macos/$ARCHIVE_NAME"
done

echo "------------------------------------------"
echo "All macOS packages generated in releases/macos/"