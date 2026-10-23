#!/bin/bash
set -e

# Resolve the project root directory (parent of the scripts/ directory)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

# Pre-flight checks
for cmd in dotnet fpm zip; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "ERROR: '$cmd' is not installed. Please install it and try again."
    exit 1
  fi
done

# Verify fpm actually works (it may be installed but broken due to Ruby version incompatibility)
if ! fpm --version &>/dev/null; then
  echo "ERROR: 'fpm' is installed but failing to run."
  echo ""
  echo "This is usually caused by a Ruby version incompatibility."
  echo "The GitHub Actions workflow uses Ruby 3.2, which is the recommended version."
  echo ""
  echo "To fix this, you can:"
  echo "  1. Install Ruby 3.2 using a version manager (rbenv, rvm, asdf, etc.)"
  echo "  2. Then reinstall fpm: gem install fpm"
  echo ""
  echo "Alternatively, if you have Ruby 3.2 available, you can run:"
  echo "  ruby-3.2 /usr/local/bin/fpm --version"
  echo ""
  exit 1
fi

# Clean up
rm -rf ./out/
rm -rf ./staging/
rm -rf ./releases/linux

# .NET publish
# self-contained is recommended, so final users won't need to install .NET
dotnet publish "./SqlQueryStress.Desktop/SqlQueryStress.Desktop.csproj" \
  --verbosity quiet \
  --nologo  \
  --configuration Release \
  --self-contained true \
  --runtime linux-x64 \
  --output "./out/linux-x64"  \
  /p:PublishSingleFile=true

# --- SHARED METADATA ---
NAME="sqlquerystress"
VERSION="${1:-0.1.0}"
MAINTAINER="Chisom Hermes Chigoziri <hermeschigoziri@gmail.com>, Abraham Semilore Bankole <abraham.o.bankole@gmail.com>"
DESC="A cross-platform SQL Server benchmarking tool for stress-testing T-SQL queries"
URL="https://github.com/TruePadawan/SqlQueryStress"
ARCH="amd64"
CATEGORY="Database"

# 1. Prepare Staging
rm -rf ./staging
mkdir -p ./staging/usr/bin
mkdir -p ./staging/usr/lib/sqlquerystress
mkdir -p ./staging/usr/share/applications
mkdir -p ./staging/usr/share/pixmaps
mkdir -p ./staging/usr/share/icons/hicolor/scalable/apps
mkdir -p ./staging/usr/share/metainfo
mkdir -p ./releases/linux

# 2. Copy Files
# Copy binary to lib
cp -r ./out/linux-x64/* ./staging/usr/lib/sqlquerystress/
# Copy wrapper script to bin
cp ./SqlQueryStress.Desktop/DEBIAN/sqlquerystress.sh ./staging/usr/bin/sqlquerystress
# Copy Desktop assets
cp ./SqlQueryStress.Desktop/DEBIAN/SqlQueryStress.desktop ./staging/usr/share/applications/
cp ./SqlQueryStress.Desktop/DEBIAN/sqlquerystress.png ./staging/usr/share/pixmaps/
cp ./SqlQueryStress.Desktop/DEBIAN/sqlquerystress.svg ./staging/usr/share/icons/hicolor/scalable/apps/
cp ./SqlQueryStress.Desktop/DEBIAN/sqlquerystress.metainfo.xml ./staging/usr/share/metainfo/

# 3. Set Permissions
chmod +x ./staging/usr/bin/sqlquerystress
chmod +x ./staging/usr/lib/sqlquerystress/SqlQueryStress.Desktop

# 4. PACKAGE EVERYTHING
echo "Packaging DEB..."
fpm -s dir -t deb \
    -n "$NAME" -v "$VERSION" -a "$ARCH" \
    --license mit \
    -m "$MAINTAINER" \
    --description "$DESC" \
    --category "$CATEGORY" \
    --url "$URL" \
    -C ./staging \
    -p "./releases/linux/sqlquerystress_${VERSION}_amd64.deb" \
    usr/

echo "Packaging RPM..."
fpm -s dir -t rpm \
    -n "$NAME" -v "$VERSION" -a "x86_64" \
    --license mit \
    -m "$MAINTAINER" \
    --description "$DESC" \
    --category "$CATEGORY" \
    --url "$URL" \
    -C ./staging \
    -p "./releases/linux/sqlquerystress-${VERSION}.x86_64.rpm" \
    usr/

echo "Packaging Pacman..."
fpm -s dir -t pacman \
    -n "$NAME" -v "$VERSION" -a "x86_64" \
    --license mit \
    -m "$MAINTAINER" \
    --description "$DESC" \
    --category "$CATEGORY" \
    --url "$URL" \
    -C ./staging \
    -p "./releases/linux/sqlquerystress-${VERSION}.pkg.tar.zst" \
    usr/

echo "Done!"
