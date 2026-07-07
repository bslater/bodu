# Package icons

Each packable project ships a 128×128 `icon.png` in its NuGet package, wired convention-first in
`bld/Packaging.props`: when `bld/icons/<PackageId>.png` exists, it is packed as the package's
`<PackageIcon>`. The CI docs workflow (`.github/workflows/docfx-build-publish.yml`, *Validate package
artwork*) fails the build when a packable project has no icon PNG, when a PNG is not 128×128, or when
an orphan icon no longer maps to a project.

## Layout

| Path | Purpose |
|---|---|
| `svg/<PackageId>.svg` | Icon source, emitted by `bld/artwork/generate-package-icons.py`. |
| `<PackageId>.png` | Checked-in 128×128 raster packed into the `.nupkg`. |
| `generate-icons.sh` | Rasterizes every `svg/*.svg` to its sibling PNG. |

## Regenerating

1. Edit the per-package accent/monogram/glyph table in `bld/artwork/generate-package-icons.py`
   (new packages need a row there **and** a hero banner row in `bld/artwork/generate-hero-banners.py`
   plus `docs/images/hero-manifest.txt`).
2. Run `python3 bld/artwork/generate-package-icons.py` to re-emit the SVG sources.
3. Run `bash bld/icons/generate-icons.sh` to re-rasterize the PNGs (uses `rsvg-convert` when
   installed, otherwise the `cairosvg` Python package — `pip install cairosvg`).
4. Commit both the SVGs and PNGs — packing consumes the PNGs directly, so builds stay
   toolchain-free and deterministic.
