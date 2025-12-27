# macOS Build Instructions

This document explains how to build the macOS version of Lightroom Backup Cleaner with proper code signing and DMG creation.

## Prerequisites

- macOS (for building macOS apps)
- .NET 8 SDK
- Xcode Command Line Tools: `xcode-select --install`
- (Optional) Apple Developer account for code signing

## Quick Start

### Basic Build (No Code Signing)

```bash
# Build for Apple Silicon (M1/M2/M3)
./build-macos.sh osx-arm64

# Build for Intel Macs
./build-macos.sh osx-x64
```

This creates:
- `publish/LightroomBackupCleaner.app` - The application bundle
- `publish/LightroomBackupCleaner-osx-arm64-0.2.0.dmg` - DMG for distribution

**Note**: Without code signing, users will need to remove quarantine attributes:
```bash
xattr -dr com.apple.quarantine LightroomBackupCleaner.app
```

### Build with Code Signing

To code sign the app (recommended for distribution):

1. **Get your signing identity**:
   ```bash
   security find-identity -v -p codesigning
   ```
   Look for "Developer ID Application" entries.

2. **Build with signing**:
   ```bash
   ./build-macos.sh osx-arm64 "Developer ID Application: Your Name (TEAM_ID)"
   ```

3. **Verify signing**:
   ```bash
   codesign --verify --verbose LightroomBackupCleaner.app
   spctl --assess --verbose LightroomBackupCleaner.app
   ```

## Code Signing Options

### Ad-Hoc Signing (Free, but limited)
```bash
./build-macos.sh osx-arm64 "-"
```
This creates a basic signature but Gatekeeper may still warn users.

### Developer ID Signing (Recommended for distribution)
Requires an Apple Developer account ($99/year):
```bash
./build-macos.sh osx-arm64 "Developer ID Application: Your Name (TEAM_ID)"
```

### Notarization (Optional, for best user experience)

After building and signing, you can notarize the DMG:

```bash
# Submit for notarization
xcrun notarytool submit LightroomBackupCleaner-osx-arm64-0.2.0.dmg \
  --apple-id YOUR_APPLE_ID \
  --team-id TEAM_ID \
  --password APP_SPECIFIC_PASSWORD \
  --wait

# Staple the notarization ticket
xcrun stapler staple LightroomBackupCleaner-osx-arm64-0.2.0.dmg
```

**Note**: Notarization requires:
- Apple Developer account
- App-specific password (create at appleid.apple.com)
- Can take 5-30 minutes

## What the Script Does

1. **Publishes the app** using `dotnet publish`
2. **Creates .app bundle structure**:
   - `Contents/MacOS/` - Contains the executable
   - `Contents/Resources/` - For resources (icons, etc.)
   - `Contents/Info.plist` - App metadata
3. **Code signs** (if identity provided):
   - Signs the executable
   - Signs the entire bundle
   - Verifies the signature
4. **Creates DMG**:
   - Creates a disk image
   - Includes the app bundle
   - Adds Applications symlink for easy installation
   - Signs the DMG (if identity provided)

## Troubleshooting

### "codesign: no identity found"
- Make sure you have Xcode Command Line Tools installed
- For Developer ID signing, you need an Apple Developer account
- Check available identities: `security find-identity -v -p codesigning`

### "Gatekeeper assessment failed"
- This is normal for ad-hoc signing
- Users can still run the app by removing quarantine: `xattr -dr com.apple.quarantine LightroomBackupCleaner.app`
- For distribution, use Developer ID signing

### "hdiutil: create failed"
- Make sure the publish directory exists and has write permissions
- Check available disk space

### App won't run after building
- Remove quarantine: `xattr -dr com.apple.quarantine LightroomBackupCleaner.app`
- Check Console.app for error messages
- Verify the executable has execute permissions: `chmod +x Contents/MacOS/LightroomBackupCleaner`

## Manual Steps (Alternative)

If you prefer to build manually:

```bash
# 1. Publish
dotnet publish -c Release -r osx-arm64 -o publish/osx-arm64

# 2. Create .app bundle
mkdir -p LightroomBackupCleaner.app/Contents/MacOS
mkdir -p LightroomBackupCleaner.app/Contents/Resources

# 3. Copy executable
cp publish/osx-arm64/LightroomBackupCleaner LightroomBackupCleaner.app/Contents/MacOS/
chmod +x LightroomBackupCleaner.app/Contents/MacOS/LightroomBackupCleaner

# 4. Create Info.plist (see build-macos.sh for template)

# 5. Code sign
codesign --force --deep --sign "Developer ID Application: Your Name (TEAM_ID)" LightroomBackupCleaner.app

# 6. Create DMG
hdiutil create -volname "Lightroom Backup Cleaner" \
  -srcfolder LightroomBackupCleaner.app \
  -ov -format UDZO LightroomBackupCleaner.dmg
```

## Distribution

For best user experience:
1. ✅ Use Developer ID signing
2. ✅ Notarize the DMG
3. ✅ Test on a clean macOS system
4. ✅ Provide clear installation instructions

The DMG can be distributed directly. Users can:
1. Download the DMG
2. Open it
3. Drag the app to Applications
4. Run it (no manual steps needed if properly signed and notarized)

