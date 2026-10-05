#!/usr/bin/env bash
set -euo pipefail

# Run this on macOS. It creates separate framework-dependent builds for
# Intel and Apple Silicon; signing and notarization are separate Apple steps.
project="Flicker.Mac/Flicker.Mac.csproj"

dotnet publish "$project" -c Release -r osx-x64 --self-contained false -o artifacts/osx-x64
dotnet publish "$project" -c Release -r osx-arm64 --self-contained false -o artifacts/osx-arm64

echo "Published to macos/artifacts/osx-x64 and macos/artifacts/osx-arm64"
