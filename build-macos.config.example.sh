#!/bin/bash

# Example configuration file for build-macos.sh
# Copy this file to build-macos.config.sh and fill in your credentials
# build-macos.config.sh is in .gitignore and will NOT be committed to git

# Code Signing Identity
# Find your identity with: security find-identity -v -p codesigning
# Example: "Developer ID Application: Your Name (TEAM_ID)"
export MACOS_SIGNING_IDENTITY="Developer ID Application: Your Name (TEAM_ID)"

# Apple ID for notarization
export MACOS_APPLE_ID="your@email.com"

# Team ID (found in Apple Developer account)
export MACOS_TEAM_ID="TEAM_ID"

# App-specific password for notarization
# Create at: https://appleid.apple.com/account/manage
# Format: xxxx-xxxx-xxxx-xxxx
export MACOS_APP_SPECIFIC_PASSWORD="xxxx-xxxx-xxxx-xxxx"



