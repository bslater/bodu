#!/usr/bin/env bash
# Packs the in-repo Bodu.CodeStyle.XmlDocumentation analyzer into ./local-packages so the main
# library projects (referenced via bld/Bodu.props) can restore it through the bodu-local NuGet
# source declared in the repo-root NuGet.config.
#
# Run once after cloning the repo, and again whenever you change anything under Bodu.CodeStyle/
# that should be picked up by Bodu.Core and the other library projects.
#
# Because the analyzer keeps a constant 1.0.0 version, NuGet's global cache must be evicted so
# that consumer projects re-extract the freshly-packed contents on their next restore.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
codestyle_dir="$repo_root/Bodu.CodeStyle"
output_dir="$repo_root/local-packages"
package_id="bodu.codestyle.xmldocumentation"

echo ">> Building + packing the analyzer..."
mkdir -p "$output_dir"
cd "$codestyle_dir"
dotnet pack Bodu.CodeStyle.XmlDocumentation/Bodu.CodeStyle.XmlDocumentation.csproj \
  --configuration Release \
  --output "$output_dir"

echo ">> Evicting NuGet global cache for $package_id so consumers re-extract the new payload..."
nuget_global="${NUGET_PACKAGES:-${HOME}/.nuget/packages}"
if [ -d "$nuget_global/$package_id" ]; then
  rm -rf "$nuget_global/$package_id"
fi
dotnet nuget locals http-cache --clear >/dev/null 2>&1 || true

echo ">> Done. Consumer projects must re-restore (e.g. dotnet restore --force) on next build."
