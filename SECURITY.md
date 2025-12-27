# Security - macOS Build Credentials

This document explains how to securely handle Apple Developer credentials when building macOS apps.

## ⚠️ Never Commit Credentials

**Never commit the following to git:**
- Apple ID email
- Team ID
- App-specific passwords
- Signing identities with full details

## 🔐 Secure Methods

### Method 1: Local Config File (Recommended)

1. **Copy the example config:**
   ```bash
   cp build-macos.config.example.sh build-macos.config.sh
   ```

2. **Edit `build-macos.config.sh`** with your credentials:
   ```bash
   export MACOS_SIGNING_IDENTITY="Developer ID Application: Your Name (TEAM_ID)"
   export MACOS_APPLE_ID="your@email.com"
   export MACOS_TEAM_ID="TEAM_ID"
   export MACOS_APP_SPECIFIC_PASSWORD="xxxx-xxxx-xxxx-xxxx"
   ```

3. **The file is in `.gitignore`** - it will NOT be committed to git.

4. **Use the build script normally:**
   ```bash
   ./build-macos.sh osx-arm64
   ```

### Method 2: Environment Variables

Set environment variables in your shell session:

```bash
export MACOS_SIGNING_IDENTITY="Developer ID Application: Your Name (TEAM_ID)"
export MACOS_APPLE_ID="your@email.com"
export MACOS_TEAM_ID="TEAM_ID"
export MACOS_APP_SPECIFIC_PASSWORD="xxxx-xxxx-xxxx-xxxx"

./build-macos.sh osx-arm64
```

**For persistent setup**, add to your `~/.zshrc` or `~/.bash_profile`:
```bash
# macOS Build Credentials
export MACOS_SIGNING_IDENTITY="Developer ID Application: Your Name (TEAM_ID)"
export MACOS_APPLE_ID="your@email.com"
export MACOS_TEAM_ID="TEAM_ID"
export MACOS_APP_SPECIFIC_PASSWORD="xxxx-xxxx-xxxx-xxxx"
```

### Method 3: Command Line Arguments

For signing identity only (not notarization):
```bash
./build-macos.sh osx-arm64 "Developer ID Application: Your Name (TEAM_ID)"
```

## 🔑 Getting Your Credentials

### Signing Identity
```bash
security find-identity -v -p codesigning
```
Look for "Developer ID Application" entries.

### Team ID
- Log in to [Apple Developer](https://developer.apple.com)
- Go to Membership
- Your Team ID is displayed there

### App-Specific Password
1. Go to [Apple ID Account](https://appleid.apple.com/account/manage)
2. Sign in with your Apple ID
3. In the "Security" section, find "App-Specific Passwords"
4. Generate a new password for "macOS Notarization"
5. Copy the password (format: `xxxx-xxxx-xxxx-xxxx`)

## ✅ Verification

After building, verify your credentials are not in git:
```bash
git status
# Should NOT show build-macos.config.sh

git grep -i "your@email.com"
# Should return nothing
```

## 🚨 If Credentials Were Accidentally Committed

1. **Remove from git history immediately:**
   ```bash
   git filter-branch --force --index-filter \
     "git rm --cached --ignore-unmatch build-macos.config.sh" \
     --prune-empty --tag-name-filter cat -- --all
   ```

2. **Add to .gitignore** (already done)

3. **Change your credentials** (especially app-specific password)

4. **Force push** (if already pushed):
   ```bash
   git push origin --force --all
   ```

## 📝 Best Practices

1. ✅ Use `build-macos.config.sh` (in .gitignore)
2. ✅ Use environment variables for CI/CD
3. ✅ Never hardcode credentials in scripts
4. ✅ Rotate app-specific passwords regularly
5. ✅ Use different passwords for different purposes
6. ❌ Never commit credentials to git
7. ❌ Never share credentials in issues or pull requests

