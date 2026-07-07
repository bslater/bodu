#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------------------------
# generate-icons.sh — rasterizes every bld/icons/svg/<PackageId>.svg to a 128x128 bld/icons/<PackageId>.png.
#
# The PNGs are checked in and packed as each package's NuGet <PackageIcon> (see bld/Packaging.props), so this
# script only needs to run when an icon SVG changes. It uses rsvg-convert when available and otherwise falls
# back to the cairosvg Python package (`pip install cairosvg`).
#
#   bash bld/icons/generate-icons.sh
# ---------------------------------------------------------------------------------------------------------------
set -eu

dir="$(cd "$(dirname "$0")" && pwd)"

for svg in "$dir"/svg/*.svg; do
  id="$(basename "$svg" .svg)"
  png="$dir/$id.png"

  if command -v rsvg-convert >/dev/null 2>&1; then
    rsvg-convert -w 128 -h 128 -o "$png" "$svg"
  else
    python3 -c "import cairosvg, sys; cairosvg.svg2png(url=sys.argv[1], write_to=sys.argv[2], output_width=128, output_height=128)" "$svg" "$png"
  fi

  echo "wrote $png"
done
