#!/usr/bin/env python3
# ---------------------------------------------------------------------------------------------------------------
# generate-package-icons.py — regenerates the per-package NuGet icon SVG sources under bld/icons/svg.
#
# Every icon shares one 128x128 template (rounded gradient tile, accent border, Bodu suite mark, a
# per-package glyph, and a monogram chip); only accent, glyph, and monogram vary. After editing, run
# from the repository root and then rasterize with bld/icons/generate-icons.sh:
#
#   python3 bld/artwork/generate-package-icons.py && bash bld/icons/generate-icons.sh
# ---------------------------------------------------------------------------------------------------------------

import os

OUT = os.path.join(os.path.dirname(__file__), "..", "icons", "svg")

TEMPLATE = """<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128" role="img" aria-label="{pkg} package icon">
  <title>{pkg}</title>
  <defs>
    <linearGradient id="{gid}-bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#0F172A"/>
      <stop offset="1" stop-color="#1E293B"/>
    </linearGradient>
  </defs>
  <rect x="2" y="2" width="124" height="124" rx="28" fill="url(#{gid}-bg)" stroke="{accent}" stroke-width="3"/>

  <!-- Bodu suite mark -->
  <g transform="translate(21 21)">
    <circle r="7.5" fill="none" stroke="#F8FAFC" stroke-width="2.2" opacity="0.85"/>
    <circle cy="-7.5" r="2" fill="#F8FAFC" opacity="0.85"/>
  </g>

  <!-- package glyph -->
  <g transform="translate(64 56)">
{glyph}
  </g>

  <!-- monogram chip -->
  <rect x="{chipx}" y="94" width="{chipw}" height="22" rx="11" fill="{accent}"/>
  <text x="64" y="110" fill="{dark}" text-anchor="middle" font-family="'Consolas','Menlo',monospace" font-weight="700" font-size="14">{mono}</text>
</svg>
"""

A = {  # accent -> dark text colour for chips
    "#3B82F6": "#0B2545", "#60A5FA": "#0B2545", "#34D399": "#06281F",
    "#FBBF24": "#3A2A04", "#A78BFA": "#241A47", "#F472B6": "#3D0A24",
    "#2DD4BF": "#042F2A", "#FB923C": "#3B1E04", "#F87171": "#3B0A0A",
    "#E2E8F0": "#1E293B",
}


def g_ring(a):
    return f'''    <circle r="22" fill="none" stroke="{a}" stroke-width="5"/>
    <circle cy="-22" r="5.5" fill="{a}"/>'''


def g_slots(a):
    out = []
    for x, y, on in [(-22, -8, 1), (0, -20, 1), (22, -8, 1), (22, 12, 0), (0, 24, 0), (-22, 12, 1)]:
        fill = a if on else "none"
        out.append(f'    <rect x="{x - 7}" y="{y - 7}" width="14" height="14" rx="3" fill="{fill}" stroke="{a}" stroke-width="2"/>')
    return "\n".join(out)


def g_lanes(a):
    return f'''    <g stroke="{a}" stroke-width="4" stroke-linecap="round">
      <line x1="-24" y1="-14" x2="18" y2="-14"/>
      <line x1="-18" y1="2" x2="24" y2="2"/>
      <line x1="-24" y1="18" x2="18" y2="18"/>
    </g>
    <g fill="{a}">
      <polygon points="26,-14 16,-19 16,-9"/>
      <polygon points="-26,2 -16,-3 -16,7"/>
      <polygon points="26,18 16,13 16,23"/>
    </g>'''


def g_chips(a):
    return f'''    <g stroke="{a}" stroke-width="2.5" fill="none">
      <rect x="-26" y="-22" width="52" height="13" rx="6.5"/>
      <rect x="-26" y="-4" width="52" height="13" rx="6.5" fill="{a}"/>
      <rect x="-26" y="14" width="52" height="13" rx="6.5"/>
    </g>'''


def g_doclines(a):
    return f'''    <rect x="-20" y="-26" width="40" height="52" rx="5" fill="none" stroke="{a}" stroke-width="3"/>
    <g stroke="{a}" stroke-width="3" stroke-linecap="round">
      <line x1="-11" y1="-12" x2="11" y2="-12"/>
      <line x1="-11" y1="0" x2="11" y2="0"/>
      <line x1="-11" y1="12" x2="4" y2="12"/>
    </g>'''


def g_text(a, s, size=34):
    return f'''    <text y="{size * 0.36:.0f}" fill="{a}" text-anchor="middle" font-family="'Consolas','Menlo',monospace" font-weight="700" font-size="{size}">{s}</text>'''


def g_lock(a):
    return f'''    <rect x="-18" y="-4" width="36" height="28" rx="6" fill="{a}"/>
    <path d="M -10 -4 L -10 -14 A 10 10 0 0 1 10 -14 L 10 -4" fill="none" stroke="{a}" stroke-width="5"/>
    <circle cy="8" r="4" fill="#0F172A"/>'''


def g_grid(a):
    cells = []
    hl = {(1, 0), (2, 2)}
    for r in range(3):
        for c in range(3):
            x, y = -26 + c * 18, -22 + r * 16
            fill = a if (r, c) in hl else "none"
            cells.append(f'      <rect x="{x}" y="{y}" width="14" height="12" rx="2.5" fill="{fill}"/>')
    return f'''    <g stroke="{a}" stroke-width="2.5">
{chr(10).join(cells)}
    </g>'''


def g_coin(a):
    return f'''    <circle r="24" fill="none" stroke="{a}" stroke-width="5"/>
{g_text(a, "$", 30)}'''


def g_plug(a):
    return f'''    <line x1="-26" y1="0" x2="-8" y2="0" stroke="{a}" stroke-width="4" stroke-linecap="round"/>
    <rect x="-8" y="-11" width="18" height="22" rx="4" fill="{a}"/>
    <rect x="10" y="-7" width="8" height="5" fill="{a}"/>
    <rect x="10" y="2" width="8" height="5" fill="{a}"/>
    <rect x="21" y="-14" width="9" height="28" rx="4" fill="none" stroke="{a}" stroke-width="3"/>'''


def g_shield(a):
    return f'''    <path d="M 0 -26 L 22 -18 L 22 4 Q 22 20 0 28 Q -22 20 -22 4 L -22 -18 Z" fill="none" stroke="{a}" stroke-width="4"/>
    <circle cy="-2" r="5" fill="none" stroke="{a}" stroke-width="3"/>
    <rect x="-2.5" y="2" width="5" height="11" rx="2.5" fill="{a}"/>'''


def g_spark(a):
    return f'''    <polyline points="-26,18 -12,6 0,12 12,-8 20,-2 26,-16" fill="none" stroke="{a}" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
    <circle cx="26" cy="-16" r="4" fill="{a}"/>'''


def g_cylinder(a):
    return f'''    <g stroke="{a}" stroke-width="3.5" fill="none">
      <ellipse cy="-16" rx="22" ry="8"/>
      <path d="M -22 -16 L -22 16 A 22 8 0 0 0 22 16 L 22 -16"/>
      <ellipse rx="22" ry="8" cy="0" stroke-dasharray="4 4" stroke-width="2"/>
    </g>'''


def g_pills(a):
    return f'''    <g stroke="{a}" stroke-width="2.5">
      <rect x="-26" y="-24" width="44" height="13" rx="6.5" fill="{a}"/>
      <rect x="-18" y="-6" width="44" height="13" rx="6.5" fill="none"/>
      <rect x="-26" y="12" width="44" height="13" rx="6.5" fill="none"/>
    </g>'''


def g_fraction(a):
    return f'''    <text x="0" y="-6" fill="{a}" text-anchor="middle" font-family="'Consolas','Menlo',monospace" font-weight="700" font-size="24">22</text>
    <line x1="-15" y1="0" x2="15" y2="0" stroke="{a}" stroke-width="3.5" stroke-linecap="round"/>
    <text x="0" y="22" fill="{a}" text-anchor="middle" font-family="'Consolas','Menlo',monospace" font-weight="700" font-size="24">7</text>'''


def g_nodes(a):
    return f'''    <g stroke="{a}" stroke-width="3" fill="none">
      <circle cx="-16" cy="-14" r="8"/>
      <circle cx="16" cy="-14" r="8"/>
      <circle cy="16" r="8"/>
      <line x1="-10" y1="-8" x2="-4" y2="9"/>
      <line x1="10" y1="-8" x2="4" y2="9"/>
      <line x1="-8" y1="-14" x2="8" y2="-14" stroke-dasharray="3 3" stroke-width="2"/>
    </g>'''


GLYPHS = {
    "ring": g_ring, "slots": g_slots, "lanes": g_lanes, "chips": g_chips,
    "doclines": g_doclines, "lock": g_lock, "grid": g_grid, "coin": g_coin,
    "plug": g_plug, "shield": g_shield, "spark": g_spark, "cylinder": g_cylinder,
    "pills": g_pills, "fraction": g_fraction, "nodes": g_nodes,
    "braces": lambda a: g_text(a, "{ }", 36), "hash": lambda a: g_text(a, "#", 44),
    "eq": lambda a: g_text(a, "k=v", 28), "indent": lambda a: g_text(a, "- :", 34),
    "bee": lambda a: g_text(a, "d…e", 28),
}

# PackageId -> (slug/gid, accent, monogram, glyph)
ICONS = {
    "Bodu.Core":                         ("core", "#3B82F6", "CORE", "ring"),
    "Bodu.Collections":                  ("col", "#34D399", "COL", "slots"),
    "Bodu.Collections.Concurrent":       ("colc", "#60A5FA", "CONC", "lanes"),
    "Bodu.Text.Encoding":                ("tenc", "#FBBF24", "B·N", "chips"),
    "Bodu.Text.Formats":                 ("tfmt", "#34D399", "FMT", "doclines"),
    "Bodu.Text.Delimited":               ("tdlm", "#34D399", "CSV", "grid"),
    "Bodu.Text.DotEnv":                  ("tdenv", "#2DD4BF", "ENV", "doclines"),
    "Bodu.Text.Ini":                     ("tini", "#FB923C", "INI", "doclines"),
    "Bodu.Text.Bencode":                 ("tben", "#34D399", "BEN", "bee"),
    "Bodu.Text.Toml":                    ("ttoml", "#FBBF24", "TOML", "eq"),
    "Bodu.Text.Yaml":                    ("tyaml", "#60A5FA", "YAML", "indent"),
    "Bodu.Text.Configuration":           ("tcfg", "#A78BFA", "CFG", "doclines"),
    "Bodu.Extensions.Configuration.Text": ("mect", "#2DD4BF", "MECT", "plug"),
    "Bodu.IO.Hashing":                   ("iohash", "#FB923C", "CRC", "hash"),
    "Bodu.Security.Cryptography":        ("crypto", "#F87171", "AEAD", "lock"),
    "Bodu.IO.Compound":                  ("iocmp", "#60A5FA", "CFB", "doclines"),
    "Bodu.Formats.Excel.Binary":         ("xls", "#34D399", "XLS", "grid"),
    "Bodu.Formats.Outlook":              ("olk", "#60A5FA", "MAPI", "eq"),
    "Bodu.Formats.Outlook.Msg":          ("olkmsg", "#2DD4BF", "MSG", "doclines"),
    "Bodu.Numerics":                     ("num", "#A78BFA", "NUM", "fraction"),
    "Bodu.Numerics.Serialization.Json":  ("numjson", "#A78BFA", "JSON", "braces"),
    "Bodu.Financial":                    ("fin", "#34D399", "FIN", "coin"),
    "Bodu.Financial.DependencyInjection": ("findi", "#34D399", "FDI", "plug"),
    "Bodu.Financial.Serialization.Json": ("finjson", "#34D399", "JSON", "braces"),
    "Bodu.Globalization.Calendar":       ("cal", "#FBBF24", "CAL", "grid"),
    "Bodu.Globalization.Recurrence":      ("recur", "#2DD4BF", "REC", "grid"),
    "Bodu.Globalization.Calendar.Builder": ("calb", "#FBBF24", "BLD", "pills"),
    "Bodu.Globalization.Calendar.DependencyInjection": ("caldi", "#60A5FA", "CDI", "plug"),
    "Bodu.Globalization.Calendar.Plugins": ("calplg", "#2DD4BF", "PLG", "shield"),
    "Bodu.Globalization.Calendar.Africa": ("calaf", "#FBBF24", "AF", "grid"),
    "Bodu.Globalization.Calendar.Americas": ("calam", "#34D399", "AM", "grid"),
    "Bodu.Globalization.Calendar.AsiaPacific": ("calap", "#F87171", "AP", "grid"),
    "Bodu.Globalization.Calendar.Europe": ("caleu", "#60A5FA", "EU", "grid"),
    "Bodu.Globalization.Calendar.MiddleEast": ("calme", "#A78BFA", "ME", "grid"),
    "Bodu.Financial.ExchangeRates":      ("fx", "#34D399", "FX", "spark"),
    "Bodu.Financial.ExchangeRates.DependencyInjection": ("fxdi", "#34D399", "FXDI", "plug"),
    "Bodu.Financial.ExchangeRates.Rba":  ("fxrba", "#FBBF24", "RBA", "spark"),
    "Bodu.Financial.ExchangeRates.Boe":  ("fxboe", "#A78BFA", "BOE", "spark"),
    "Bodu.Financial.ExchangeRates.Ecb":  ("fxecb", "#60A5FA", "ECB", "spark"),
    "Bodu.Financial.ExchangeRates.Yahoo": ("fxyahoo", "#F472B6", "YHO", "spark"),
    "Bodu.Financial.ExchangeRates.Ofx":  ("fxofx", "#2DD4BF", "OFX", "spark"),
    "Bodu.Financial.ExchangeRates.Xe":   ("fxxe", "#F87171", "XE", "spark"),
    "Bodu.Financial.ExchangeRates.Oanda": ("fxoanda", "#FB923C", "OAN", "spark"),
    "Bodu.Financial.ExchangeRates.Caching": ("fxcache", "#34D399", "CCH", "cylinder"),
    "Bodu.Financial.ExchangeRates.Caching.Distributed": ("fxdist", "#F87171", "DST", "nodes"),
    "Bodu.Financial.ExchangeRates.Caching.Sqlite": ("fxsql", "#60A5FA", "SQL", "cylinder"),
    "Bodu.Globalization.Calendar.Caching": ("calcache", "#FB923C", "CCH", "cylinder"),
    "Bodu.Globalization.Calendar.Caching.Distributed": ("caldist", "#F87171", "DST", "nodes"),
    "Bodu.Globalization.Calendar.Caching.Sqlite": ("calsql", "#60A5FA", "SQL", "cylinder"),
    "Bodu.Text.Serialization": ("tser", "#A78BFA", "SER", "braces"),
}


def main():
    os.makedirs(OUT, exist_ok=True)
    for pkg, (gid, accent, mono, glyph) in ICONS.items():
        chipw = max(40, 16 + len(mono) * 10)
        svg = TEMPLATE.format(
            pkg=pkg, gid=gid, accent=accent, dark=A[accent], mono=mono,
            glyph=GLYPHS[glyph](accent), chipx=64 - chipw // 2, chipw=chipw)
        path = os.path.join(OUT, pkg + ".svg")
        with open(path, "w") as f:
            f.write(svg)
    print("wrote", len(ICONS), "icon svgs")


if __name__ == "__main__":
    main()
