#!/bin/bash

# macOS Build Script with Code Signing and DMG Creation
# Usage: ./build-macos.sh [osx-x64|osx-arm64] [signing-identity]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
APP_NAME="LightroomBackupCleaner"
BUNDLE_NAME="${APP_NAME}.app"
VERSION="0.2.0"
ARCH="${1:-osx-arm64}"
SIGNING_IDENTITY="${2:-}"

# Validate architecture
if [[ "$ARCH" != "osx-x64" && "$ARCH" != "osx-arm64" ]]; then
    echo -e "${RED}Error: Invalid architecture. Use 'osx-x64' or 'osx-arm64'${NC}"
    exit 1
fi

echo -e "${GREEN}Building ${APP_NAME} for ${ARCH}...${NC}"

# Clean previous builds
echo -e "${YELLOW}Cleaning previous builds...${NC}"
rm -rf "publish/${ARCH}"
rm -rf "publish/${ARCH}-app"
rm -rf "publish/${BUNDLE_NAME}"
rm -f "publish/${APP_NAME}-${ARCH}.dmg"

# Publish the app
echo -e "${YELLOW}Publishing application...${NC}"
dotnet publish -c Release -r "${ARCH}" -o "publish/${ARCH}" --self-contained true /p:PublishSingleFile=true

# Create .app bundle structure
echo -e "${YELLOW}Creating .app bundle...${NC}"
APP_DIR="publish/${BUNDLE_NAME}"
CONTENTS_DIR="${APP_DIR}/Contents"
MACOS_DIR="${CONTENTS_DIR}/MacOS"
RESOURCES_DIR="${CONTENTS_DIR}/Resources"

mkdir -p "${MACOS_DIR}"
mkdir -p "${RESOURCES_DIR}"

# Copy executable (the executable name matches AssemblyName from .csproj)
EXECUTABLE_NAME="LightroomBackupCleaner"

# Check if executable exists
if [[ ! -f "publish/${ARCH}/${EXECUTABLE_NAME}" ]]; then
    echo -e "${RED}Error: Executable not found at publish/${ARCH}/${EXECUTABLE_NAME}${NC}"
    exit 1
fi

cp "publish/${ARCH}/${EXECUTABLE_NAME}" "${MACOS_DIR}/${EXECUTABLE_NAME}"
chmod +x "${MACOS_DIR}/${EXECUTABLE_NAME}"

# Create Info.plist
echo -e "${YELLOW}Creating Info.plist...${NC}"
cat > "${CONTENTS_DIR}/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>${EXECUTABLE_NAME}</string>
    <key>CFBundleIdentifier</key>
    <string>nl.photofactsacademy.lightroombackupcleaner</string>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key>
    <string>Lightroom Backup Cleaner</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSRequiresAquaSystemAppearance</key>
    <false/>
</dict>
</plist>
EOF

# Copy icon if available (convert ICO to ICNS would be needed, but for now we'll skip)
# For a proper icon, you'd need to create an .icns file from the PNG

# Code signing
if [[ -n "$SIGNING_IDENTITY" ]]; then
    echo -e "${YELLOW}Code signing with identity: ${SIGNING_IDENTITY}...${NC}"
    
    # Sign the executable
    codesign --force --deep --sign "${SIGNING_IDENTITY}" "${MACOS_DIR}/${EXECUTABLE_NAME}"
    
    # Sign the entire bundle
    codesign --force --deep --sign "${SIGNING_IDENTITY}" "${APP_DIR}"
    
    # Verify signing
    echo -e "${YELLOW}Verifying code signature...${NC}"
    codesign --verify --verbose "${APP_DIR}"
    spctl --assess --verbose "${APP_DIR}" || echo -e "${YELLOW}Warning: Gatekeeper assessment failed. This is normal for ad-hoc signing.${NC}"
else
    echo -e "${YELLOW}No signing identity provided. Skipping code signing.${NC}"
    echo -e "${YELLOW}Note: Users will need to remove quarantine attributes manually:${NC}"
    echo -e "${YELLOW}  xattr -dr com.apple.quarantine ${APP_DIR}${NC}"
fi

# Create DMG
echo -e "${YELLOW}Creating DMG...${NC}"
DMG_NAME="${APP_NAME}-${ARCH}-${VERSION}.dmg"
DMG_PATH="publish/${DMG_NAME}"

# Create a temporary directory for DMG contents
DMG_TEMP="publish/dmg-temp"
rm -rf "${DMG_TEMP}"
mkdir -p "${DMG_TEMP}"

# Copy app to DMG temp
cp -R "${APP_DIR}" "${DMG_TEMP}/"

# Create Applications symlink (optional, for easier installation)
ln -s /Applications "${DMG_TEMP}/Applications"

# Create DMG
hdiutil create -volname "${APP_NAME}" -srcfolder "${DMG_TEMP}" -ov -format UDZO "${DMG_PATH}"

# Clean up temp directory
rm -rf "${DMG_TEMP}"

# Sign DMG if signing identity provided
if [[ -n "$SIGNING_IDENTITY" ]]; then
    echo -e "${YELLOW}Signing DMG...${NC}"
    codesign --sign "${SIGNING_IDENTITY}" "${DMG_PATH}"
fi

echo -e "${GREEN}✓ Build complete!${NC}"
echo -e "${GREEN}  App bundle: ${APP_DIR}${NC}"
echo -e "${GREEN}  DMG: ${DMG_PATH}${NC}"

if [[ -z "$SIGNING_IDENTITY" ]]; then
    echo ""
    echo -e "${YELLOW}Note: To code sign, provide a signing identity:${NC}"
    echo -e "${YELLOW}  ./build-macos.sh ${ARCH} \"Developer ID Application: Your Name (TEAM_ID)\"${NC}"
    echo ""
    echo -e "${YELLOW}To notarize (requires Apple Developer account):${NC}"
    echo -e "${YELLOW}  xcrun notarytool submit ${DMG_PATH} --apple-id YOUR_APPLE_ID --team-id TEAM_ID --password APP_SPECIFIC_PASSWORD --wait${NC}"
fi

