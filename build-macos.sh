#!/bin/bash
set -e

# -------------------------------------------------
# macOS build + sign + notarize script (x64 + arm64)
# Usage:
#   ./build-macos.sh all
#   ./build-macos.sh osx-x64
#   ./build-macos.sh osx-arm64
# -------------------------------------------------

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# -------------------------------------------------
# Load optional config (ignored by git)
# -------------------------------------------------
if [[ -f "./build-macos.config.sh" ]]; then
  source "./build-macos.config.sh"
fi

# -------------------------------------------------
# Configuration
# -------------------------------------------------
APP_NAME="LightroomBackupCleaner"
EXECUTABLE_NAME="LightroomBackupCleaner"
BUNDLE_ID="nl.photofactsacademy.lightroombackupcleaner"
VERSION="0.2.0"

ICON_PNG="Assets/lrcbackupcleaner.png"
ICON_ICNS="Assets/AppIcon.icns"

ENTITLEMENTS="./macos.entitlements.plist"

ARCH="${1:-all}"   # osx-arm64 | osx-x64 | all
PUBLISH_DIR="publish"

# -------------------------------------------------
# Validate arch
# -------------------------------------------------
if [[ "$ARCH" != "osx-arm64" && "$ARCH" != "osx-x64" && "$ARCH" != "all" ]]; then
  echo -e "${RED}Invalid architecture. Use osx-arm64, osx-x64 or all${NC}"
  exit 1
fi

# -------------------------------------------------
# Generate .icns from PNG (optional)
# -------------------------------------------------
if [[ -f "$ICON_PNG" ]]; then
  echo -e "${YELLOW}Generating AppIcon.icns...${NC}"
  ICONSET="Assets/AppIcon.iconset"
  rm -rf "$ICONSET"
  mkdir -p "$ICONSET"

  sips -z 16 16     "$ICON_PNG" --out "$ICONSET/icon_16x16.png"
  sips -z 32 32     "$ICON_PNG" --out "$ICONSET/icon_16x16@2x.png"
  sips -z 32 32     "$ICON_PNG" --out "$ICONSET/icon_32x32.png"
  sips -z 64 64     "$ICON_PNG" --out "$ICONSET/icon_32x32@2x.png"
  sips -z 128 128   "$ICON_PNG" --out "$ICONSET/icon_128x128.png"
  sips -z 256 256   "$ICON_PNG" --out "$ICONSET/icon_128x128@2x.png"
  sips -z 256 256   "$ICON_PNG" --out "$ICONSET/icon_256x256.png"
  sips -z 512 512   "$ICON_PNG" --out "$ICONSET/icon_256x256@2x.png"
  sips -z 512 512   "$ICON_PNG" --out "$ICONSET/icon_512x512.png"
  sips -z 1024 1024 "$ICON_PNG" --out "$ICONSET/icon_512x512@2x.png"

  iconutil -c icns "$ICONSET" -o "$ICON_ICNS"
  rm -rf "$ICONSET"
else
  echo -e "${YELLOW}Warning: icon PNG not found, skipping icon.${NC}"
fi

# -------------------------------------------------
# Helpers
# -------------------------------------------------
sign_macho_file_if_needed() {
  local f="$1"
  local main_exec="$2"

  # Only sign Mach-O files; skip main executable (signed with entitlements)
  if file "$f" | grep -q "Mach-O"; then
    if [[ "$f" != "$main_exec" ]]; then
      # Some helper binaries might already be signed; force re-sign is ok
      codesign --force --options runtime --timestamp \
        --sign "$MACOS_SIGNING_IDENTITY" "$f" >/dev/null 2>&1 || true
    fi
  fi
}

# -------------------------------------------------
# Build function
# -------------------------------------------------
build_arch() {
  local TARGET="$1"

  echo -e "${GREEN}Building for ${TARGET}...${NC}"

  local APP_DIR="${PUBLISH_DIR}/${APP_NAME}-${TARGET}.app"
  local CONTENTS_DIR="${APP_DIR}/Contents"
  local MACOS_DIR="${CONTENTS_DIR}/MacOS"
  local RESOURCES_DIR="${CONTENTS_DIR}/Resources"
  local DMG_NAME="${APP_NAME}-${TARGET}-${VERSION}.dmg"
  local DMG_PATH="${PUBLISH_DIR}/${DMG_NAME}"

  rm -rf "${APP_DIR}"
  rm -rf "${PUBLISH_DIR}/${TARGET}"
  rm -f "${DMG_PATH}"

  # Publish
  dotnet publish -c Release -r "${TARGET}" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:DebugType=None -p:DebugSymbols=false \
    -o "${PUBLISH_DIR}/${TARGET}"

  mkdir -p "${MACOS_DIR}" "${RESOURCES_DIR}"

  # Copy full publish output into Contents/MacOS (Avalonia/.NET needs native libs/resources)
  cp -R "${PUBLISH_DIR}/${TARGET}/." "${MACOS_DIR}/"

  # Remove debug artifacts that should not ship
  find "${APP_DIR}" -type f \( -name "*.pdb" -o -name "*.mdb" -o -name "*.dbg" \) -delete

  # Ensure main executable is executable
  chmod +x "${MACOS_DIR}/${EXECUTABLE_NAME}"

  # Copy icon if available
  if [[ -f "$ICON_ICNS" ]]; then
    cp "$ICON_ICNS" "${RESOURCES_DIR}/AppIcon.icns"
  fi

  # Info.plist
  cat > "${CONTENTS_DIR}/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
 "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>${EXECUTABLE_NAME}</string>
  <key>CFBundleIdentifier</key>
  <string>${BUNDLE_ID}</string>
  <key>CFBundleName</key>
  <string>${APP_NAME}</string>
  <key>CFBundleDisplayName</key>
  <string>${APP_NAME}</string>
  <key>CFBundleVersion</key>
  <string>${VERSION}</string>
  <key>CFBundleShortVersionString</key>
  <string>${VERSION}</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>LSMinimumSystemVersion</key>
  <string>10.15</string>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>CFBundleIconFile</key>
  <string>AppIcon</string>
</dict>
</plist>
EOF

  # -------------------------------------------------
  # Code signing
  # -------------------------------------------------
  if [[ -n "$MACOS_SIGNING_IDENTITY" ]]; then
    echo -e "${YELLOW}Signing app (Hardened Runtime + entitlements)...${NC}"

    if [[ ! -f "$ENTITLEMENTS" ]]; then
      echo -e "${RED}Error: Entitlements file not found: $ENTITLEMENTS${NC}"
      echo -e "${YELLOW}Create it to allow .NET JIT (otherwise the app may be killed on launch).${NC}"
      exit 1
    fi

    local MAIN_EXEC="${MACOS_DIR}/${EXECUTABLE_NAME}"

    # 1) Sign the main executable WITH entitlements (needed for .NET/Avalonia JIT)
    codesign --force --options runtime --timestamp \
      --entitlements "$ENTITLEMENTS" \
      --sign "$MACOS_SIGNING_IDENTITY" \
      "$MAIN_EXEC"

    # 2) Sign dylibs/so (NO entitlements needed)
    find "${APP_DIR}" -type f \( -name "*.dylib" -o -name "*.so" \) -print0 | while IFS= read -r -d '' f; do
      codesign --force --options runtime --timestamp \
        --sign "$MACOS_SIGNING_IDENTITY" "$f"
    done

    # 2b) Sign any other Mach-O helper binaries inside Contents/MacOS (NO entitlements)
    # (Some publish outputs include helper executables without .dylib extension.)
    find "${MACOS_DIR}" -type f -print0 | while IFS= read -r -d '' f; do
      sign_macho_file_if_needed "$f" "$MAIN_EXEC"
    done

    # 3) Sign the whole bundle last WITH entitlements (keeps signature consistent)
    codesign --force --deep --options runtime --timestamp \
      --entitlements "$ENTITLEMENTS" \
      --sign "$MACOS_SIGNING_IDENTITY" "${APP_DIR}"

    # Verify
    echo -e "${YELLOW}Verifying code signature...${NC}"
    codesign --verify --deep --strict --verbose=4 "${APP_DIR}"
    spctl --assess --verbose "${APP_DIR}"
  else
    echo -e "${YELLOW}No MACOS_SIGNING_IDENTITY set. Skipping signing/notarization.${NC}"
  fi

  # -------------------------------------------------
  # DMG
  # -------------------------------------------------
  echo -e "${YELLOW}Creating DMG...${NC}"
  local TMP="${PUBLISH_DIR}/dmg-temp"
  rm -rf "$TMP"
  mkdir -p "$TMP"

  cp -R "${APP_DIR}" "$TMP/"
  ln -s /Applications "$TMP/Applications"

  # Make volume name unique per arch to avoid confusion when both DMGs are open
  hdiutil create -volname "${APP_NAME} (${TARGET})" \
    -srcfolder "$TMP" \
    -ov -format UDZO "$DMG_PATH"

  rm -rf "$TMP"

  if [[ -n "$MACOS_SIGNING_IDENTITY" ]]; then
    codesign --force --sign "$MACOS_SIGNING_IDENTITY" "$DMG_PATH"
  fi

  # -------------------------------------------------
  # Notarization
  # -------------------------------------------------
  if [[ -n "$MACOS_SIGNING_IDENTITY" && -n "$MACOS_APPLE_ID" && -n "$MACOS_TEAM_ID" && -n "$MACOS_APP_SPECIFIC_PASSWORD" ]]; then
    echo -e "${YELLOW}Notarizing DMG...${NC}"
    xcrun notarytool submit "$DMG_PATH" \
      --apple-id "$MACOS_APPLE_ID" \
      --team-id "$MACOS_TEAM_ID" \
      --password "$MACOS_APP_SPECIFIC_PASSWORD" \
      --wait

    xcrun stapler staple "$DMG_PATH"
    echo -e "${GREEN}✓ Notarization complete${NC}"
  fi

  echo -e "${GREEN}✓ Done: $DMG_PATH${NC}"
}

# -------------------------------------------------
# Run builds
# -------------------------------------------------
mkdir -p "$PUBLISH_DIR"

if [[ "$ARCH" == "all" ]]; then
  build_arch "osx-arm64"
  build_arch "osx-x64"
else
  build_arch "$ARCH"
fi
