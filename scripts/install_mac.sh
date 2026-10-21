#!/usr/bin/env bash
set -e

REPO="TruePadawan/SqlQueryStress.Avalonia"
APP_NAME="SqlQueryStress.app"
INSTALL_DIR="/Applications"

echo "==> Detecting macOS architecture..."
ARCH=$(uname -m)
if [ "$ARCH" = "arm64" ]; then
  RID="osx-arm64"
elif [ "$ARCH" = "x86_64" ]; then
  RID="osx-x64"
else
  echo "Error: Unsupported architecture: $ARCH"
  exit 1
fi

echo "==> Fetching latest release version from GitHub..."
LATEST_TAG=$(curl -sSL "https://api.github.com/repos/${REPO}/releases/latest" | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')

if [ -z "$LATEST_TAG" ]; then
  echo "Error: Could not determine the latest release tag for $REPO."
  exit 1
fi

VERSION="${LATEST_TAG#v}"
ZIP_NAME="SqlQueryStress-${VERSION}-${RID}.zip"
DOWNLOAD_URL="https://github.com/${REPO}/releases/download/${LATEST_TAG}/${ZIP_NAME}"

TMP_DIR=$(mktemp -d)
trap 'rm -rf "$TMP_DIR"' EXIT

echo "==> Downloading SqlQueryStress v${VERSION} (${RID})..."
curl -fL# "$DOWNLOAD_URL" -o "$TMP_DIR/$ZIP_NAME"

echo "==> Extracting bundle..."
unzip -q "$TMP_DIR/$ZIP_NAME" -d "$TMP_DIR"

echo "==> Installing to ${INSTALL_DIR}/${APP_NAME}..."
if [ -d "${INSTALL_DIR}/${APP_NAME}" ]; then
  rm -rf "${INSTALL_DIR}/${APP_NAME}"
fi
mv "$TMP_DIR/$APP_NAME" "$INSTALL_DIR/"

# Clear any extended quarantine attributes so Gatekeeper never blocks launch
xattr -cr "${INSTALL_DIR}/${APP_NAME}" 2>/dev/null || true

echo "==> SqlQueryStress v${VERSION} installed to ${INSTALL_DIR}/${APP_NAME}!"
echo "==> Launching SqlQueryStress..."
open -a "${INSTALL_DIR}/${APP_NAME}"