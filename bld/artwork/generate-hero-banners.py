#!/usr/bin/env python3
# ---------------------------------------------------------------------------------------------------------------
# generate-hero-banners.py — regenerates every docs/images/hero-*.svg from one canonical template.
#
# All hero banners share identical scaffolding (480x220 dark gradient card, a 150x160 left panel, a
# bidirectional arrow pair, a 142x160 right panel, and a bottom caption); only the accent colour, the
# panel contents, the arrow labels, and the caption vary per banner. Run from the repository root:
#
#   python3 bld/artwork/generate-hero-banners.py
#
# The authoritative PackageId -> banner mapping lives in docs/images/hero-manifest.txt and is validated
# by the "Validate package artwork" step in .github/workflows/docfx-build-publish.yml.
# ---------------------------------------------------------------------------------------------------------------
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "docs", "images")

# Accent -> chip/header text colour dark enough for contrast on that accent.
DARK = {
    "#3B82F6": "#F8FAFC", "#60A5FA": "#0B2545", "#34D399": "#06281F",
    "#FBBF24": "#3A2A04", "#A78BFA": "#241A47", "#F472B6": "#3D0A24",
    "#2DD4BF": "#042F2A", "#FB923C": "#3B1E04", "#F87171": "#3B0A0A",
}

TEMPLATE = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 220" role="img" aria-label="{aria}">
  <title>{title}</title>
  <defs>
    <linearGradient id="{gid}-bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#0F172A"/>
      <stop offset="1" stop-color="#1E293B"/>
    </linearGradient>
  </defs>
  <rect width="480" height="220" rx="12" fill="url(#{gid}-bg)"/>

  <!-- Left panel -->
  <g transform="translate(28 30)" font-family="'Segoe UI','Helvetica Neue',Arial,sans-serif" font-size="11">
    <rect width="150" height="160" rx="8" fill="#1E293B" stroke="#334155" stroke-width="1.2"/>
    <rect width="150" height="22" rx="8" fill="{accent}"/>
    <rect y="14" width="150" height="8" fill="{accent}"/>
    <text x="75" y="16" fill="{ldark}" text-anchor="middle" font-weight="700" font-size="{lhsize}">{lheader}</text>

{lbody}
  </g>

  <!-- Bidirectional pipeline arrows -->
  <g transform="translate(186 80)" fill="none" stroke="{accent}" stroke-width="2" stroke-linecap="round">
    <line x1="0" y1="0" x2="104" y2="0"/>
    <polygon points="104,0 96,-4 96,4" fill="{accent}" stroke="none"/>
    <line x1="104" y1="36" x2="0" y2="36"/>
    <polygon points="0,36 8,32 8,40" fill="{accent}" stroke="none"/>
  </g>
  <g transform="translate(186 80)" fill="{accent}" font-family="'Consolas','Menlo',monospace" font-size="10" text-anchor="middle">
    <text x="52" y="-6">{up}</text>
    <text x="52" y="56">{down}</text>
  </g>

  <!-- Right panel -->
  <g transform="translate(310 30)" font-family="'Segoe UI','Helvetica Neue',Arial,sans-serif" font-size="11">
    <rect width="142" height="160" rx="8" fill="#1E293B" stroke="#334155" stroke-width="1.2"/>
    <rect width="142" height="22" rx="8" fill="#3B82F6"/>
    <rect y="14" width="142" height="8" fill="#3B82F6"/>
    <text x="71" y="16" fill="#F8FAFC" text-anchor="middle" font-weight="700" font-size="{rhsize}">{rheader}</text>

{rbody}
  </g>

  <!-- Bottom caption -->
  <g fill="#94A3B8" font-family="'Consolas','Menlo',monospace" font-size="10">
    <text x="240" y="206" text-anchor="middle">{caption}</text>
  </g>
</svg>
"""


def mono(rows, x=10, y0=44, dy=17, size=10.5):
    """A block of monospace body lines; each row is raw inner SVG text markup."""
    out = [f'    <g fill="#E2E8F0" font-family="\'Consolas\',\'Menlo\',monospace" font-size="{size}">']
    y = y0
    for row in rows:
        if row is None:
            y += dy // 2
            continue
        out.append(f'      <text x="{x}" y="{y}">{row}</text>')
        y += dy
    out.append("    </g>")
    return "\n".join(out)


def note(text, x=10, y=150):
    return f'    <text x="{x}" y="{y}" fill="#94A3B8" font-family="\'Consolas\',\'Menlo\',monospace" font-size="9">{text}</text>'


def ring(accent):
    """Circular-buffer slot ring for the collections banner (panel-relative)."""
    slots = [(105, 78, 1), (90, 112, 1), (66, 126, 1), (42, 112, 0), (27, 78, 0), (42, 44, 0), (66, 30, 1), (90, 44, 1)]
    out = [f'    <g stroke="#334155" stroke-width="1.2">']
    for x, y, on in slots:
        fill = accent if on else "#0F172A"
        out.append(f'      <rect x="{x}" y="{y}" width="18" height="18" rx="3" fill="{fill}"/>')
    out.append("    </g>")
    out.append(f'''    <g fill="#94A3B8" font-family="'Consolas','Menlo',monospace" font-size="9">
      <text x="75" y="26" text-anchor="middle">head</text>
      <text x="128" y="74">tail</text>
    </g>
    <line x1="126" y1="78" x2="118" y2="84" stroke="#94A3B8" stroke-width="1.4" stroke-linecap="round"/>''')
    out.append(note("bounded · O(1) ends", y=152))
    return "\n".join(out)


def calendar_grid(accent, highlights, y0=38, cols=4, rows=3, w=28, h=22, dx=33, dy=30, x0=13):
    out = ['    <g stroke="#334155" stroke-width="1">']
    i = 0
    for r in range(rows):
        for c in range(cols):
            fill = accent if i in highlights else "#0F172A"
            out.append(f'      <rect x="{x0 + c * dx}" y="{y0 + r * dy}" width="{w}" height="{h}" rx="3" fill="{fill}"/>')
            i += 1
    out.append("    </g>")
    return "\n".join(out)


def chips(codes, accent, x0=12, y0=34, per_row=4, w=28, h=16, dx=31, dy=24):
    out = []
    for i, code in enumerate(codes[:12]):
        r, c = divmod(i, per_row)
        x, y = x0 + c * dx, y0 + r * dy
        out.append(f'    <rect x="{x}" y="{y}" width="{w}" height="{h}" rx="8" fill="#0F172A" stroke="#334155" stroke-width="1"/>')
        out.append(f'    <text x="{x + w // 2}" y="{y + 12}" fill="{accent}" font-family="\'Consolas\',\'Menlo\',monospace" font-size="9.5" text-anchor="middle">{code}</text>')
    return "\n".join(out)


def sparkline(accent, pair, y0=96):
    return f'''    <g transform="translate(10 {y0})">
      <polyline points="0,34 22,26 43,30 65,16 87,22 108,8 130,14" fill="none" stroke="{accent}" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
      <circle cx="130" cy="14" r="3" fill="{accent}"/>
      <text x="0" y="52" fill="#94A3B8" font-family="'Consolas','Menlo',monospace" font-size="9">{pair} daily reference</text>
    </g>'''


def format_chip(accent, label, y=70):
    w = 12 + int(len(label) * 5.6)
    return f'''    <rect x="10" y="{y}" width="{w}" height="18" rx="9" fill="#0F172A" stroke="{accent}" stroke-width="1.2"/>
    <text x="{10 + w // 2}" y="{y + 13}" fill="{accent}" text-anchor="middle" font-family="'Consolas','Menlo',monospace" font-size="9.5">{label}</text>'''


def provider_left(accent, s1, s2, chip, pair):
    return "\n".join([
        mono([s1, f'<tspan fill="#94A3B8">{s2}</tspan>'], y0=44, dy=17),
        format_chip(accent, chip, y=74),
        sparkline(accent, pair, y0=100),
    ])


def provider_right(accent, history, note_text):
    return mono([
        "IDatedRateProvider", None, "RateBook", None,
        f'<tspan fill="{accent}">{history}</tspan>', None,
        f'<tspan fill="#94A3B8" font-size="9">{note_text}</tspan>',
    ], y0=46, dy=16, size=9.5)


B = {}  # filename (without .svg) -> spec


def add(name, title, aria, accent, lheader, lbody, up, down, rheader, rbody, caption, gid=None):
    B[name] = dict(title=title, aria=aria, accent=accent, lheader=lheader, lbody=lbody,
                   up=up, down=down, rheader=rheader, rbody=rbody, caption=caption,
                   gid=gid or name.replace("hero-", "").replace("-", ""))


# --- Foundation ---------------------------------------------------------------------------------------------

add("hero-core", "Bodu.Core", "Bodu.Core — the foundation package: buffers, threading, functional seams, text, validation",
    "#3B82F6", "Bodu.Core",
    mono(["PooledBufferBuilder", "WeekPattern", "AsyncLock · RateGate", "Option · Result",
          "Memoizer · Either", "SequenceGenerator"], y0=44, dy=18),
    "extend", "validate", "Extensions",
    mono(["enumerable ops", "dictionary · list ops", "EncodingDetection", "StringEncoding",
          "XML helpers", '<tspan fill="#60A5FA">ThrowHelper</tspan>'], y0=44, dy=18),
    "buffers · threading · railway seams · text · xml")

add("hero-collections", "Bodu.Collections", "Bodu.Collections — bounded, ordered, and probabilistic collections",
    "#34D399", "CircularBuffer&lt;T&gt;", ring("#34D399"),
    "enqueue", "dequeue", "Catalogue",
    mono(["Deque&lt;T&gt;", "EvictingDictionary", "IndexedPriorityQueue", "RangeDictionary",
          '<tspan fill="#60A5FA">IntervalTree</tspan>', '<tspan fill="#60A5FA">Trie · Graph&lt;T&gt;</tspan>',
          '<tspan fill="#34D399">BloomFilter · HLL</tspan>'], y0=42, dy=17),
    "bounded · ordered · navigable · probabilistic", gid="col")

add("hero-collections-concurrent", "Bodu.Collections.Concurrent", "Bodu.Collections.Concurrent — thread-safe collection variants",
    "#60A5FA", "Threads",
    "\n".join([
        mono(["producer 1", None, "producer 2", None, "consumer"], y0=48, dy=19, size=10),
        '''    <g fill="none" stroke="#60A5FA" stroke-width="1.6" stroke-linecap="round" stroke-dasharray="4 4">
      <line x1="10" y1="58" x2="138" y2="58"/>
      <line x1="10" y1="96" x2="138" y2="96"/>
      <line x1="138" y1="134" x2="10" y2="134"/>
    </g>''',
        note("no locks on the hot path", y=152)]),
    "TryAdd", "TryTake", "MPMC ring",
    "\n".join([
        '''    <g stroke="#334155" stroke-width="1.2">
      <rect x="12" y="38" width="26" height="22" rx="3" fill="#60A5FA"/>
      <rect x="44" y="38" width="26" height="22" rx="3" fill="#60A5FA"/>
      <rect x="76" y="38" width="26" height="22" rx="3" fill="#0F172A"/>
      <rect x="108" y="38" width="26" height="22" rx="3" fill="#0F172A"/>
    </g>
    <g fill="#0B2545" font-family="'Consolas','Menlo',monospace" font-size="9" text-anchor="middle">
      <text x="25" y="52">s=4</text>
      <text x="57" y="52">s=5</text>
      <text x="89" y="52" fill="#94A3B8">s=6</text>
      <text x="121" y="52" fill="#94A3B8">s=7</text>
    </g>''',
        mono(["ConcurrentCircular", "Buffer&lt;T&gt;", None,
              '<tspan fill="#60A5FA">ConcurrentHashSet&lt;T&gt;</tspan>',
              '<tspan fill="#94A3B8" font-size="9">lock-free membership</tspan>'], x=12, y0=86, dy=16, size=10)]),
    "Vyukov MPMC ring · lock-free set", gid="colc")

# --- Hashing & crypto ---------------------------------------------------------------------------------------

add("hero-io", "Bodu.IO.Hashing", "Bodu.IO.Hashing — non-cryptographic hashing, CRC catalogue, and check digits",
    "#FB923C", "input bytes",
    "\n".join([
        mono(['<tspan fill="#94A3B8">0x</tspan>a7 <tspan fill="#94A3B8">0x</tspan>f3 <tspan fill="#94A3B8">0x</tspan>18',
              '<tspan fill="#94A3B8">0x</tspan>c2 <tspan fill="#94A3B8">0x</tspan>9d <tspan fill="#94A3B8">0x</tspan>44'], y0=46, dy=18),
        '''    <g stroke="#334155" stroke-width="1.2">
      <rect x="10" y="88" width="20" height="20" rx="3" fill="#FB923C"/>
      <rect x="36" y="88" width="20" height="20" rx="3" fill="#0F172A"/>
      <rect x="62" y="88" width="20" height="20" rx="3" fill="#FB923C"/>
      <rect x="88" y="88" width="20" height="20" rx="3" fill="#0F172A"/>
      <rect x="114" y="88" width="20" height="20" rx="3" fill="#FB923C"/>
    </g>
    <text x="10" y="126" fill="#94A3B8" font-family="'Consolas','Menlo',monospace" font-size="9">shift register · polynomial</text>''',
        note("crc32 = ec6c8f7d", y=148)]),
    "append", "digest", "Catalogue",
    mono(["Crc <tspan fill=\"#94A3B8\">×85 standards</tspan>", "Fletcher-16/32/64", "Adler32 · Adler64",
          "Fnv1a32 · Fnv1a64", None, '<tspan fill="#60A5FA">Luhn · IBAN · ISBN</tspan>',
          '<tspan fill="#60A5FA">Damm · ISO 7064</tspan>'], y0=44, dy=16, size=10),
    "RevEng CRC catalogue · streaming · resumable · check digits", gid="iohash")

add("hero-crypto", "Bodu.Security.Cryptography", "Bodu.Security.Cryptography — block ciphers, AEAD, keyed hashes, and post-quantum algorithms",
    "#F87171", "symmetric",
    mono(["Threefish · Skein", "Blowfish · Twofish", "Camellia · Skipjack", "Ascon AEAD",
          "BLAKE2 · BLAKE3", "SipHash · Poly1305", '<tspan fill="#94A3B8" font-size="9">EAX · GCM · OCB · SIV</tspan>'],
         y0=44, dy=17, size=10),
    "encrypt", "decrypt", "asymmetric",
    mono(["X25519", "Ed25519", None, '<tspan fill="#F87171">ML-KEM 512/768/1024</tspan>',
          '<tspan fill="#F87171">ML-DSA 44/65/87</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">FIPS 203 · FIPS 204</tspan>'], y0=44, dy=16, size=10),
    "block ciphers · AEAD · keyed hashes · post-quantum")

# --- Text ----------------------------------------------------------------------------------------------------

add("hero-text", "Bodu.Text", "Bodu.Text — text-encoding utilities in the Bodu.Core foundation package",
    "#F472B6", "Bodu.Text",
    mono(["EncodingDetection", "EncodingExtensions", "StringEncoding", "Extensions", None,
          '<tspan fill="#94A3B8" font-size="9">namespace in Bodu.Core</tspan>'], y0=46, dy=18),
    "detect", "decode", "Detection",
    mono(['BOM <tspan fill="#94A3B8">UTF-8/16/32</tspan>', "heuristic sniffing", "charset fallbacks", None,
          '<tspan fill="#60A5FA">GetString(span)</tspan>', '<tspan fill="#60A5FA">TryGetBytes(…)</tspan>'], y0=44, dy=17, size=10),
    "encoding detection · span-first conversions")

add("hero-text-encoding", "Bodu.Text.Encoding", "Bodu.Text.Encoding — binary-to-text encodings from Base16 to Base85",
    "#FBBF24", "bytes",
    "\n".join([
        mono(['<tspan fill="#94A3B8">0x</tspan>48 <tspan fill="#94A3B8">0x</tspan>65 <tspan fill="#94A3B8">0x</tspan>6C',
              '<tspan fill="#94A3B8">0x</tspan>6C <tspan fill="#94A3B8">0x</tspan>6F <tspan fill="#94A3B8">0x</tspan>2C',
              '<tspan fill="#94A3B8">0x</tspan>20 <tspan fill="#94A3B8">0x</tspan>42 <tspan fill="#94A3B8">0x</tspan>6F',
              '<tspan fill="#94A3B8">0x</tspan>64 <tspan fill="#94A3B8">0x</tspan>75 <tspan fill="#94A3B8">0x</tspan>21'],
             y0=46, dy=20, size=11),
        note("span · UTF-8 · streaming", y=140)]),
    "encode", "decode", "Base•N",
    "\n".join([
        '''    <g stroke="#334155" stroke-width="1">
      <rect x="12" y="34" width="118" height="18" rx="9" fill="#0F172A"/>
      <rect x="12" y="58" width="118" height="18" rx="9" fill="#0F172A"/>
      <rect x="12" y="82" width="118" height="18" rx="9" fill="#0F172A"/>
      <rect x="12" y="106" width="118" height="18" rx="9" fill="#0F172A"/>
      <rect x="12" y="130" width="118" height="18" rx="9" fill="#0F172A"/>
    </g>
    <g fill="#E2E8F0" font-family="'Consolas','Menlo',monospace" font-size="10" text-anchor="middle">
      <text x="71" y="47">Base16 <tspan fill="#94A3B8">48656C</tspan></text>
      <text x="71" y="71">Base32 <tspan fill="#94A3B8">JBSWY3</tspan></text>
      <text x="71" y="95">Base58 <tspan fill="#94A3B8">9Ajdvzr</tspan></text>
      <text x="71" y="119">Base64 <tspan fill="#94A3B8">SGVsbG8</tspan></text>
      <text x="71" y="143">Base85 <tspan fill="#94A3B8">87cURDZ</tspan></text>
    </g>''']),
    "RFC 4648 · Crockford · Base64Url · Z85 · Ascii85", gid="tenc")

add("hero-formats", "Bodu.Text.Formats", "Bodu.Text.Formats — Delimited, DotEnv, and INI document formats",
    "#34D399", "documents",
    mono(["Delimited", '<tspan fill="#94A3B8">RFC 4180 CSV · TSV</tspan>', None,
          "DotEnv", '<tspan fill="#94A3B8">.env key=value</tspan>', None,
          "Ini", '<tspan fill="#94A3B8">sections · comments</tspan>'], y0=42, dy=15, size=10),
    "parse", "write", "Documents",
    mono(["DelimitedParse", "Options", None, "DotEnvDocument", None, "IniDocument",
          '<tspan fill="#94A3B8" font-size="9">round-trip preserving</tspan>'], y0=44, dy=16, size=10),
    "three text document formats · one package", gid="fmt")

add("hero-toml", "Bodu.Text.Toml", "Bodu.Text.Toml — TOML reader and writer over a typed value model",
    "#FBBF24", "TOML text",
    mono(['title <tspan fill="#FBBF24">=</tspan> <tspan fill="#60A5FA">"app"</tspan>', None,
          '<tspan fill="#FBBF24">[</tspan>owner<tspan fill="#FBBF24">]</tspan>',
          'name <tspan fill="#FBBF24">=</tspan> <tspan fill="#60A5FA">"Tom"</tspan>',
          'dob  <tspan fill="#FBBF24">=</tspan> 1979-05-27', None,
          '<tspan fill="#FBBF24">[</tspan>db<tspan fill="#FBBF24">]</tspan>',
          'ports <tspan fill="#FBBF24">=</tspan> <tspan fill="#FBBF24">[</tspan>8000<tspan fill="#FBBF24">]</tspan>'],
         y0=42, dy=14, size=11),
    "read", "write", "TomlTable",
    mono(["table", '<tspan fill="#60A5FA">  title</tspan>', '<tspan fill="#60A5FA">  owner</tspan>',
          "    name", "    dob", '<tspan fill="#60A5FA">  db</tspan>', "    ports[]"], y0=42, dy=16, size=11),
    "typed · v1.0 / v1.1 · canonical", gid="tml")

add("hero-bencode", "Bodu.Text.Bencode", "Bodu.Text.Bencode — framed BEP 3 serialization",
    "#34D399", "object tree",
    mono(["dict", '<tspan fill="#60A5FA">  "announce"</tspan>', '    "tracker..."',
          '<tspan fill="#60A5FA">  "info"</tspan>', "    dict",
          '<tspan fill="#60A5FA">    "length"</tspan>', "      1024"], y0=42, dy=16, size=11),
    "encode", "decode", "byte stream",
    mono(['<tspan fill="#FBBF24">d</tspan>8:announce', "12:tracker...", "4:info",
          '<tspan fill="#FBBF24">d</tspan>6:length', '<tspan fill="#FBBF24">i</tspan>1024<tspan fill="#FBBF24">e</tspan>',
          '<tspan fill="#FBBF24">e</tspan>', '<tspan fill="#FBBF24">e</tspan>'], y0=42, dy=16, size=11),
    "framed · self-describing · canonical", gid="bnc")

add("hero-yaml", "Bodu.Text.Yaml", "Bodu.Text.Yaml — YAML reader and writer over a typed node model",
    "#60A5FA", "YAML text",
    mono(['title<tspan fill="#FBBF24">:</tspan> <tspan fill="#60A5FA">app</tspan>', None,
          'owner<tspan fill="#FBBF24">:</tspan>',
          '  name<tspan fill="#FBBF24">:</tspan> <tspan fill="#60A5FA">Tom</tspan>',
          '  born<tspan fill="#FBBF24">:</tspan> 1979-05-27', None,
          'db<tspan fill="#FBBF24">:</tspan>',
          '  ports<tspan fill="#FBBF24">:</tspan> <tspan fill="#FBBF24">[</tspan>8000<tspan fill="#FBBF24">]</tspan>'],
         y0=42, dy=14, size=11),
    "read", "write", "YamlNode",
    mono(["mapping", '<tspan fill="#60A5FA">  title</tspan>', '<tspan fill="#60A5FA">  owner</tspan>',
          "    name", "    born", '<tspan fill="#60A5FA">  db</tspan>', "    ports[]"], y0=42, dy=16, size=11),
    "block · flow · anchors · 1.2 core", gid="yml")

add("hero-text-serialization", "Bodu.Text.Serialization",
    "Bodu.Text.Serialization — shared primitives for the System.Text.Json-shaped serializers",
    "#A78BFA", "shared core",
    mono(['<tspan fill="#A78BFA">[BoduPropertyName]</tspan>',
          '<tspan fill="#A78BFA">[BoduIgnore]</tspan>',
          '<tspan fill="#A78BFA">[BoduRequired]</tspan>', None,
          'NamingPolicy', 'UnmappedMemberHandling', None,
          '<tspan fill="#94A3B8" font-size="9">one attribute family,</tspan>',
          '<tspan fill="#94A3B8" font-size="9">every format</tspan>'], y0=42, dy=15, size=10),
    "declare", "consume", "Serializers",
    mono(['<tspan fill="#A78BFA">Bencode</tspan>Serializer',
          '<tspan fill="#A78BFA">Toml</tspan>Serializer', None,
          '<tspan fill="#94A3B8">callbacks · creation ·</tspan>',
          '<tspan fill="#94A3B8">ignore · naming enums</tspan>'], y0=44, dy=17, size=10.5),
    "format-agnostic seams · consumed per format", gid="tser")

add("hero-serializers", "Bodu serializers — Bencode · TOML · YAML",
    "Bodu serializers — Bencode, TOML, and YAML sharing one System.Text.Json-aligned shape",
    "#A78BFA", "three formats",
    "\n".join([
        '''    <g stroke="#334155" stroke-width="1.2">
      <rect x="10" y="34" width="130" height="34" rx="6" fill="#0F172A"/>
      <rect x="10" y="76" width="130" height="34" rx="6" fill="#0F172A"/>
      <rect x="10" y="118" width="130" height="34" rx="6" fill="#0F172A"/>
    </g>
    <g stroke="none">
      <rect x="10" y="34" width="5" height="34" rx="2.5" fill="#34D399"/>
      <rect x="10" y="76" width="5" height="34" rx="2.5" fill="#FBBF24"/>
      <rect x="10" y="118" width="5" height="34" rx="2.5" fill="#60A5FA"/>
    </g>
    <g font-family="'Consolas','Menlo',monospace">
      <text x="24" y="48" fill="#34D399" font-weight="700" font-size="10">bencode</text>
      <text x="24" y="62" fill="#94A3B8" font-size="9">d3:key5:valuee</text>
      <text x="24" y="90" fill="#FBBF24" font-weight="700" font-size="10">TOML</text>
      <text x="24" y="104" fill="#94A3B8" font-size="9">key = "value"</text>
      <text x="24" y="132" fill="#60A5FA" font-weight="700" font-size="10">YAML</text>
      <text x="24" y="146" fill="#94A3B8" font-size="9">key: value</text>
    </g>''']),
    "serialize", "deserialize", "One shape",
    mono(["•Serializer", '•Node <tspan fill="#94A3B8">mutable DOM</tspan>',
          '•Document <tspan fill="#94A3B8">read-only</tspan>', "Utf8•Reader", "Utf8•Writer", None,
          '<tspan fill="#94A3B8" font-size="9">System.Text.Json-aligned</tspan>'], y0=46, dy=17, size=10),
    "three self-contained libraries · one architecture", gid="ser")

add("hero-configuration", "Bodu.Text.Configuration", "Bodu.Text.Configuration — file-targeted configuration layering",
    "#A78BFA", "sections",
    mono(['<tspan fill="#94A3B8">preamble</tspan>', "root = true", None,
          '<tspan fill="#2DD4BF">[*.cs]</tspan>', "indent = 4", None,
          '<tspan fill="#A78BFA">[src/**]</tspan>', "indent = 2"], y0=42, dy=14, size=10.5),
    "resolve", "layer", "resolved view",
    mono(['<tspan fill="#60A5FA">indent:style</tspan> = space', '<tspan fill="#60A5FA">indent:size</tspan>  = 2',
          '<tspan fill="#60A5FA">log:level</tspan>    = Warn', '<tspan fill="#60A5FA">root</tspan>         = true', None,
          '<tspan fill="#94A3B8">GetInt32 · GetEnum&lt;T&gt;</tspan>'], y0=44, dy=18, size=10),
    "layer · glob-match · last-wins · resolve", gid="tcfg")

add("hero-extensions-config", "Bodu.Extensions.Configuration.Text",
    "Bodu.Extensions.Configuration.Text — Microsoft.Extensions.Configuration bridge",
    "#2DD4BF", "providers",
    mono(['<tspan fill="#94A3B8">appsettings.json</tspan>', '<tspan fill="#94A3B8">env. variables</tspan>',
          '<tspan fill="#2DD4BF">Bodu .editorconfig</tspan>', '<tspan fill="#94A3B8">command line</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">layered · last wins</tspan>'], y0=46, dy=19, size=10.5),
    "Build()", "bind", "IConfiguration",
    mono(['cfg<tspan fill="#FBBF24">[</tspan>"indent:size"<tspan fill="#FBBF24">]</tspan>',
          '.GetSection("log")', None, '<tspan fill="#A78BFA">IOptions&lt;T&gt;</tspan>', None,
          '<tspan fill="#94A3B8">DI · reload · binding</tspan>'], y0=44, dy=18, size=10),
    "AddTextConfiguration* · layered source · IOptions", gid="mect")

# --- Formats -------------------------------------------------------------------------------------------------

add("hero-delimited", "Bodu.Text.Delimited", "Bodu.Text.Delimited — character-separated record parsing",
    "#34D399", "row model",
    mono(['<tspan fill="#F8FAFC" font-weight="700">name  age  city</tspan>',
          "Alice  30   Paris", "Bob    25   London", "Eve    35   Rome", None,
          '<tspan fill="#94A3B8" font-size="9">header-keyed access</tspan>'], y0=44, dy=19, size=10.5),
    "format", "parse", "csv text",
    mono(['name<tspan fill="#FBBF24">,</tspan>age<tspan fill="#FBBF24">,</tspan>city',
          'Alice<tspan fill="#FBBF24">,</tspan>30<tspan fill="#FBBF24">,</tspan>Paris',
          'Bob<tspan fill="#FBBF24">,</tspan>25<tspan fill="#FBBF24">,</tspan>London',
          'Eve<tspan fill="#FBBF24">,</tspan>35<tspan fill="#FBBF24">,</tspan>Rome'], y0=44, dy=20, size=11),
    "delimited · quoted · header-keyed", gid="dlm")

add("hero-dotenv", "Bodu.Text.DotEnv", "Bodu.Text.DotEnv — environment-file key/value parsing",
    "#FBBF24", "entries",
    mono(['<tspan fill="#F8FAFC" font-weight="700">HOST </tspan> localhost',
          '<tspan fill="#F8FAFC" font-weight="700">PORT </tspan> 8080',
          '<tspan fill="#F8FAFC" font-weight="700">USER </tspan> admin',
          '<tspan fill="#F8FAFC" font-weight="700">DEBUG</tspan> true'], y0=48, dy=24, size=11),
    "format", "parse", ".env text",
    mono(['<tspan fill="#94A3B8"># app config</tspan>',
          '<tspan fill="#60A5FA">HOST</tspan><tspan fill="#FBBF24">=</tspan>localhost',
          '<tspan fill="#60A5FA">PORT</tspan><tspan fill="#FBBF24">=</tspan>8080',
          '<tspan fill="#60A5FA">USER</tspan><tspan fill="#FBBF24">=</tspan>admin',
          '<tspan fill="#60A5FA">DEBUG</tspan><tspan fill="#FBBF24">=</tspan>true'], y0=42, dy=16, size=11),
    "key=value · quoted · ordered", gid="env")

add("hero-ini", "Bodu.Text.Ini", "Bodu.Text.Ini — section-organised configuration documents",
    "#60A5FA", "document",
    mono(['<tspan fill="#94A3B8">preamble</tspan>', "  root = true", None,
          '<tspan fill="#F8FAFC" font-weight="700">[server]</tspan>', "  host = ...", "  port = 8080", None,
          '<tspan fill="#F8FAFC" font-weight="700">[logging]</tspan>'], y0=42, dy=14, size=11),
    "save", "parse", "ini text",
    mono(['<tspan fill="#94A3B8">; config sample</tspan>', "root = true", None,
          '<tspan fill="#FBBF24">[server]</tspan>',
          '<tspan fill="#60A5FA">host</tspan> = localhost', '<tspan fill="#60A5FA">port</tspan> = 8080', None,
          '<tspan fill="#FBBF24">[logging]</tspan>'], y0=42, dy=14, size=11),
    "sections · comments · round-trip", gid="ini")

# --- Numerics & financial ------------------------------------------------------------------------------------

add("hero-numerics", "Bodu.Numerics", "Bodu.Numerics — exact rational arithmetic and bounded intervals",
    "#A78BFA", "Fraction&lt;T&gt;",
    "\n".join([
        '''    <g font-family="'Consolas','Menlo',monospace">
      <text x="34" y="58" fill="#F8FAFC" text-anchor="middle" font-size="18" font-weight="700">3</text>
      <text x="34" y="92" fill="#F8FAFC" text-anchor="middle" font-size="18" font-weight="700">4</text>
      <text x="62" y="76" fill="#94A3B8" font-size="18">=</text>
      <text x="82" y="62" fill="#A78BFA" font-size="13" font-weight="700">0.75</text>
      <text x="82" y="92" fill="#FBBF24" font-size="13" font-weight="700">[0; 1, 3]</text>
    </g>
    <line x1="22" y1="68" x2="46" y2="68" stroke="#A78BFA" stroke-width="2.5" stroke-linecap="round"/>''',
        note("BigInteger-promoted · canonical", y=130)]),
    "normalize", "promote", "Interval&lt;T&gt;",
    "\n".join([
        '''    <g font-family="'Consolas','Menlo',monospace" font-size="10">
      <line x1="14" y1="70" x2="128" y2="70" stroke="#334155" stroke-width="2"/>
      <line x1="20" y1="62" x2="62" y2="62" stroke="#60A5FA" stroke-width="4" stroke-linecap="round"/>
      <line x1="82" y1="62" x2="118" y2="62" stroke="#A78BFA" stroke-width="4" stroke-linecap="round" stroke-dasharray="1 6"/>
      <text x="41" y="50" fill="#60A5FA" text-anchor="middle">[0, 1]</text>
      <text x="100" y="50" fill="#A78BFA" text-anchor="middle">(2, 3)</text>
    </g>''',
        mono(["IntervalSet&lt;T&gt;", '<tspan fill="#94A3B8">union · complement</tspan>'], y0=104, dy=17, size=10),
        note("INumber&lt;T&gt; generic math", x=14, y=146)]),
    "canonical · BigInteger-promoted · INumber&lt;T&gt;", gid="num")

add("hero-numerics-json", "Bodu.Numerics.Serialization.Json",
    "Bodu.Numerics.Serialization.Json — System.Text.Json converters for fractions and intervals",
    "#A78BFA", "Bodu.Numerics",
    "\n".join([
        mono(["Fraction&lt;T&gt;"], y0=48, dy=17),
        '''    <g font-family="'Consolas','Menlo',monospace" font-size="14" fill="#A78BFA">
      <text x="24" y="76">22</text>
      <text x="24" y="100">7</text>
    </g>
    <line x1="22" y1="82" x2="46" y2="82" stroke="#A78BFA" stroke-width="1.6"/>''',
        mono(['Interval&lt;T&gt; <tspan fill="#A78BFA">[1,2)</tspan>', "IntervalSet&lt;T&gt;"], y0=126, dy=18)]),
    "write", "read", "System.Text.Json",
    mono(['<tspan fill="#FBBF24">{</tspan>',
          '  <tspan fill="#60A5FA">"fraction"</tspan>: <tspan fill="#34D399">"22/7"</tspan>,',
          '  <tspan fill="#60A5FA">"interval"</tspan>: <tspan fill="#FBBF24">{</tspan>',
          '    <tspan fill="#60A5FA">"min"</tspan>: 1,',
          '    <tspan fill="#60A5FA">"max"</tspan>: 2',
          '  <tspan fill="#FBBF24">}</tspan>', '<tspan fill="#FBBF24">}</tspan>'], y0=42, dy=16, size=10),
    "options.AddNumericsJsonConverters() · NumericsJsonPolicy", gid="numjson")

add("hero-financial", "Bodu.Financial", "Bodu.Financial — type-safe monetary primitives with audit-grade FX",
    "#34D399", "Money&lt;USD&gt;",
    "\n".join([
        '''    <g font-family="'Consolas','Menlo',monospace" text-anchor="middle">
      <text x="75" y="66" fill="#F8FAFC" font-size="22" font-weight="700">$1,234.56</text>
      <text x="75" y="88" fill="#94A3B8" font-size="9">ISO 4217 · minor=2</text>
    </g>''',
        mono(["CurrencyCode", "allocation policies"], y0=116, dy=17, size=10),
        note("fair remainder distribution", y=150)]),
    "convert", "allocate", "ExchangeRate",
    mono(['<tspan fill="#34D399">× 0.93</tspan> <tspan fill="#94A3B8">USD→EUR</tspan>',
          '<tspan fill="#94A3B8">2026-06-01</tspan>', None, "RateBook", "IDatedRateProvider", None,
          '<tspan fill="#94A3B8" font-size="9">audit-grade provenance</tspan>'], y0=44, dy=15, size=10),
    "type-safe · ISO 4217 · audit-grade FX · fair allocation", gid="fin")

add("hero-financial-di", "Bodu.Financial.DependencyInjection",
    "Bodu.Financial.DependencyInjection — service registration for financial services",
    "#34D399", "Bodu.Financial",
    mono(["Money · Money&lt;T&gt;", "CurrencyCode", "ICurrency catalogue", "ExchangeRate", None,
          '<tspan fill="#94A3B8">rounding · allocation</tspan>'], y0=46, dy=16, size=10.5),
    "register", "resolve", "IServiceCollection",
    mono(["services", '<tspan fill="#34D399"> .AddFinancialService()</tspan>',
          '<tspan fill="#34D399"> .UseCurrencyResolution</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">named monetary contexts</tspan>',
          '<tspan fill="#94A3B8" font-size="9">currency lookup</tspan>'], y0=44, dy=16, size=9.5),
    "IFinancialServiceBuilder · FinancialOptions", gid="findi")

add("hero-financial-json", "Bodu.Financial.Serialization.Json",
    "Bodu.Financial.Serialization.Json — System.Text.Json converters for money and exchange rates",
    "#34D399", "Bodu.Financial",
    mono(["Money · Money&lt;T&gt;", "MoneyBag", "ExchangeRate", "CurrencyPair", None,
          '<tspan fill="#94A3B8">serialization-agnostic core</tspan>'], y0=46, dy=17),
    "write", "read", "System.Text.Json",
    mono(['<tspan fill="#FBBF24">{</tspan>',
          '  <tspan fill="#60A5FA">"amount"</tspan>: <tspan fill="#34D399">19.99</tspan>,',
          '  <tspan fill="#60A5FA">"currency"</tspan>: <tspan fill="#34D399">"USD"</tspan>',
          '<tspan fill="#FBBF24">}</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">Strict · Lenient · Compact</tspan>'], y0=42, dy=16, size=10),
    "options.AddFinancialJsonConverters() · FinancialJsonPolicy", gid="finjson")

# --- Calendar ------------------------------------------------------------------------------------------------

add("hero-calendar", "Bodu.Globalization.Calendar", "Bodu.Globalization.Calendar — resource-driven notable-date engine",
    "#FBBF24", "April",
    "\n".join([
        calendar_grid("#FBBF24", {2, 5, 9}),
        note("Easter · Anzac Day · observed", y=148)]),
    "resolve", "observe", "NotableDates",
    mono(["NotableDateService", "GetNotableDates(year)", None, "IsWorkingDay(date)",
          "observed-date shifts", None, '<tspan fill="#94A3B8" font-size="9">astronomical algorithms</tspan>'],
         y0=44, dy=16, size=10),
    "rule-driven notable dates · range resolution", gid="cal")

add("hero-recurrence", "Bodu.Globalization.Recurrence",
    "Bodu.Globalization.Recurrence — RFC 5545 RRULE and cron recurrence evaluation",
    "#2DD4BF", "RRULE",
    "\n".join([
        calendar_grid("#2DD4BF", {1, 8, 15}),
        note("weekly · monthly · yearly · cron", y=148)]),
    "parse", "expand", "Occurrences",
    mono(["RecurrenceRule.Parse(\"FREQ=WEEKLY\")", "GetOccurrences(start)", None,
          "CronExpression.Parse(\"0 9 * * 1-5\")", "GetNextOccurrence(after)", None,
          '<tspan fill="#94A3B8" font-size="9">RDATE · EXDATE · BYSETPOS</tspan>'],
         y0=44, dy=16, size=10),
    "RRULE occurrence enumeration · cron next/previous", gid="recur")

add("hero-calendar-builder", "Bodu.Globalization.Calendar.Builder",
    "Bodu.Globalization.Calendar.Builder — fluent authoring for notable-date documents",
    "#FBBF24", "DocumentBuilder",
    "\n".join([
        '''    <g stroke="#334155" stroke-width="1">
      <rect x="10" y="36" width="130" height="20" rx="10" fill="#0F172A"/>
      <rect x="10" y="64" width="130" height="20" rx="10" fill="#0F172A"/>
      <rect x="10" y="92" width="130" height="20" rx="10" fill="#0F172A"/>
      <rect x="10" y="120" width="130" height="20" rx="10" fill="#FBBF24"/>
    </g>
    <g fill="#E2E8F0" font-family="'Consolas','Menlo',monospace" font-size="9.5">
      <text x="18" y="49">.AddFixedDate(…)</text>
      <text x="18" y="77">.AddNthWeekday(…)</text>
      <text x="18" y="105">.AddEasterOffset(…)</text>
      <text x="18" y="133" fill="#3A2A04" font-weight="700">.Save("au.xml")</text>
    </g>
    <g fill="none" stroke="#FBBF24" stroke-width="1.4">
      <path d="M 75 56 L 75 64"/>
      <path d="M 75 84 L 75 92"/>
      <path d="M 75 112 L 75 120"/>
    </g>''']),
    "ToXml", "FromXml", "Document",
    mono(['<tspan fill="#60A5FA">&lt;notableDates&gt;</tspan>',
          '  &lt;rule name=<tspan fill="#34D399">"Anzac"</tspan>',
          '    month=<tspan fill="#34D399">"4"</tspan> day=<tspan fill="#34D399">"25"</tspan>/&gt;',
          '  &lt;rule name=<tspan fill="#34D399">"Easter"</tspan>',
          '    algorithm=<tspan fill="#34D399">"…"</tspan>/&gt;',
          '<tspan fill="#60A5FA">&lt;/notableDates&gt;</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">XML · JSON subset</tspan>'], y0=42, dy=15, size=9.5),
    "author · serialize · load · resolve", gid="calb")

add("hero-calendar-di", "Bodu.Globalization.Calendar.DependencyInjection",
    "Bodu.Globalization.Calendar.DependencyInjection — service registration for the notable-date engine",
    "#60A5FA", "NotableDateService",
    "\n".join([
        calendar_grid("#60A5FA", {2, 5, 11}),
        note("GetNotableDates · IsWorkingDay", y=148)]),
    "register", "resolve", "IServiceCollection",
    mono(["services", ' <tspan fill="#60A5FA">.AddNotableDate</tspan>',
          '  <tspan fill="#60A5FA">Service(</tspan><tspan fill="#34D399">"AU"</tspan><tspan fill="#60A5FA">)</tspan>', None,
          ' <tspan fill="#60A5FA">.AddReloadable…</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">hosted · reloadable</tspan>'], y0=44, dy=16, size=9.5),
    "using Bodu.Globalization.Calendar;", gid="caldi")

add("hero-calendar-plugins", "Bodu.Globalization.Calendar.Plugins",
    "Bodu.Globalization.Calendar.Plugins — trust-gated loading of external date algorithms",
    "#2DD4BF", "External assembly",
    "\n".join([
        mono(["MyAlgorithms.dll", None, '<tspan fill="#2DD4BF">INotableDate</tspan>',
              '<tspan fill="#2DD4BF">Algorithm</tspan>'], y0=44, dy=16),
        '''    <g transform="translate(102 108)">
      <path d="M 0 -14 L 12 -9 L 12 3 Q 12 12 0 17 Q -12 12 -12 3 L -12 -9 Z" fill="none" stroke="#2DD4BF" stroke-width="2"/>
      <circle cy="-2" r="2.6" fill="none" stroke="#2DD4BF" stroke-width="1.6"/>
      <rect x="-1.3" y="0" width="2.6" height="6" rx="1.3" fill="#2DD4BF"/>
    </g>
    <text x="10" y="118" fill="#94A3B8" font-family="'Consolas','Menlo',monospace" font-size="9">signed ·</text>
    <text x="10" y="130" fill="#94A3B8" font-family="'Consolas','Menlo',monospace" font-size="9">allow-listed</text>'''],),
    "load", "gate", "Calendar engine",
    "\n".join([
        '''    <g stroke="#334155" stroke-width="1">
      <rect x="12" y="36" width="118" height="20" rx="4" fill="#0F172A"/>
      <rect x="12" y="62" width="118" height="20" rx="4" fill="#0F172A"/>
      <rect x="12" y="88" width="118" height="20" rx="4" fill="#0F172A" stroke="#2DD4BF" stroke-dasharray="4 3"/>
    </g>
    <g fill="#E2E8F0" font-family="'Consolas','Menlo',monospace" font-size="9.5">
      <text x="20" y="50">EasterCalculator</text>
      <text x="20" y="76">LunarPhaseCalculator</text>
      <text x="20" y="102" fill="#2DD4BF">← plugin slot</text>
    </g>''',
        mono(['<tspan fill="#94A3B8" font-size="9">strategies resolve by</tspan>',
              '<tspan fill="#94A3B8" font-size="9">algorithm name</tspan>'], x=12, y0=128, dy=14)]),
    "trust-gated assembly loading", gid="calplg")

add("hero-calendar-tool", "Bodu.Globalization.Calendar.Tool",
    "Bodu.Globalization.Calendar.Tool — the bodu-calendar command-line compiler and lint",
    "#60A5FA", "bodu-calendar",
    mono(['<tspan fill="#60A5FA">$</tspan> lint holidays.xml',
          '  <tspan fill="#34D399">0 errors, 0 warnings</tspan>', None,
          '<tspan fill="#60A5FA">$</tspan> compile holidays.xml',
          '  wrote holidays.bcal', None,
          '<tspan fill="#60A5FA">$</tspan> info holidays.bcal'], y0=44, dy=16, size=9.5),
    "lint", "compile", "Sealed pack",
    mono(["holidays.bcal", None,
          '<tspan fill="#94A3B8" font-size="9">BODU-CAL-* diagnostics</tspan>',
          '<tspan fill="#94A3B8" font-size="9">integrity digest</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">dotnet tool install</tspan>'], y0=44, dy=16, size=10),
    "lint · compile · info", gid="caltool")

add("hero-calendar-build", "Bodu.Globalization.Calendar.Build",
    "Bodu.Globalization.Calendar.Build — MSBuild compilation of notable-date rule packs",
    "#A78BFA", "app.csproj",
    mono(['<tspan fill="#94A3B8">&lt;ItemGroup&gt;</tspan>',
          ' <tspan fill="#A78BFA">&lt;NotableDatePack</tspan>',
          '  Include=<tspan fill="#34D399">"holidays.xml"</tspan>/&gt;',
          '<tspan fill="#94A3B8">&lt;/ItemGroup&gt;</tspan>', None,
          '<tspan fill="#34D399">dotnet build</tspan>'], y0=44, dy=16, size=9.5),
    "compile", "pack", "Build output",
    mono(["holidays.bcal", None,
          '<tspan fill="#94A3B8" font-size="9">incremental</tspan>',
          '<tspan fill="#94A3B8" font-size="9">build-time only</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">bundled bodu-calendar</tspan>'], y0=44, dy=16, size=10),
    "NotableDatePack → sealed .bcal", gid="calbld")

add("hero-calendar-caching", "Bodu.Globalization.Calendar.Caching",
    "Bodu.Globalization.Calendar.Caching — per-territory, per-year caching for notable-date services",
    "#FB923C", "NotableDateService",
    mono(['<tspan fill="#FB923C">CachingNotableDateService</tspan>', None,
          'AU <tspan fill="#94A3B8">2026 ✓ 2027 ✓</tspan>',
          'GB <tspan fill="#94A3B8">2026 ✓</tspan>',
          'US <tspan fill="#94A3B8">2026 ✓ 2027 ✓</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">in-memory · one TOML/JSON</tspan>',
          '<tspan fill="#94A3B8" font-size="9">file per territory</tspan>'], y0=44, dy=16, size=10),
    "resolve", "cache", "Computed years",
    mono(["GetNotableDates(…)", None,
          '<tspan fill="#FB923C">hit</tspan> <tspan fill="#94A3B8">→ cached year</tspan>',
          '<tspan fill="#FB923C">miss</tspan> <tspan fill="#94A3B8">→ inner service</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">TTL or resource-version</tspan>',
          '<tspan fill="#94A3B8" font-size="9">change refreshes a year</tspan>'], y0=44, dy=16, size=10),
    "compute a territory-year once · serve it cached", gid="calcache")

add("hero-calendar-caching-distributed", "Bodu.Globalization.Calendar.Caching.Distributed",
    "Bodu.Globalization.Calendar.Caching.Distributed — IDistributedCache / Redis backend for the notable-date cache",
    "#F87171", "IDistributedCache",
    mono(['<tspan fill="#F87171">DistributedNotableDateCache</tspan>', None,
          '<tspan fill="#94A3B8">app-1 ─┐</tspan>',
          '<tspan fill="#94A3B8">app-2 ─┼→</tspan> Redis',
          '<tspan fill="#94A3B8">app-3 ─┘</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">one notable-date cache</tspan>',
          '<tspan fill="#94A3B8" font-size="9">for the whole fleet</tspan>'], y0=44, dy=16, size=10),
    "get", "set", "Notable dates",
    mono(['<tspan fill="#F87171">AddDistributedNotableDateCache</tspan>',
          '<tspan fill="#F87171">AddRedisNotableDateCache</tspan>', None,
          'INotableDateCache', None,
          '<tspan fill="#94A3B8" font-size="9">shared across instances</tspan>'], y0=44, dy=16, size=9.5),
    "Redis · SQL Server · any backend", gid="caldist")

add("hero-calendar-caching-sqlite", "Bodu.Globalization.Calendar.Caching.Sqlite",
    "Bodu.Globalization.Calendar.Caching.Sqlite — durable SQLite backend for the notable-date cache",
    "#60A5FA", "calendar.db",
    "\n".join([
        '''    <g stroke="#60A5FA" stroke-width="1.6" fill="#0F172A">
      <g transform="translate(47 40)">
        <ellipse cx="28" cy="8" rx="28" ry="8"/>
        <path d="M 0 8 L 0 50 A 28 8 0 0 0 56 50 L 56 8" fill="#0F172A"/>
        <ellipse cx="28" cy="29" rx="28" ry="8" fill="none" stroke-dasharray="3 3" stroke-width="1"/>
      </g>
    </g>''',
        mono(['years <tspan fill="#94A3B8">(territory,year)</tspan>',
              'dates <tspan fill="#94A3B8">(rule,date,kind)</tspan>'], x=14, y0=122, dy=17, size=9.5)]),
    "lookup", "persist", "Notable dates",
    mono(['<tspan fill="#60A5FA">AddSqliteNotableDateCache(…)</tspan>', None, "CachingNotableDate",
          'Service <tspan fill="#94A3B8">read-through</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">survives restarts —</tspan>',
          '<tspan fill="#94A3B8" font-size="9">compute once per year</tspan>'], y0=44, dy=16, size=9.5),
    "durable single-file cache · zero server", gid="calsql")

CAL_PACKS = [
    ("africa", "Africa", "#FBBF24", {2, 5, 9}, ["ZA", "NG", "KE", "GH", "ET", "EG", "MA"],
     "national + subdivision rules", "7 country packs · Islamic · Coptic · national days"),
    ("americas", "Americas", "#34D399", {0, 6, 11}, ["US", "CA", "MX", "BR", "AR", "CL", "CO", "PE"],
     "national + subdivision rules", "8 country packs · nth-weekday · observed shifts"),
    ("asiapacific", "Asia-Pacific", "#F87171", {1, 4, 10}, ["AU", "CN", "IN", "JP", "KR", "MY", "NZ", "SG", "ID", "TH", "PH", "VN"],
     "+ HK · TW subdivisions", "14 country packs · lunar · Hindu · Buddhist rules"),
    ("europe", "Europe", "#60A5FA", {3, 7, 8}, ["GB", "FR", "DE", "IT", "ES", "NL", "SE", "NO", "DK", "FI", "IE", "PT"],
     "28 EU/EEA territories", "28 territory packs · Easter (western + orthodox)"),
    ("middleeast", "Middle East", "#A78BFA", {2, 6, 9}, ["AE", "SA", "IL", "TR", "QA", "JO"],
     "Umm al-Qura + Hebrew rules", "6 country packs · Hijri · Hebrew calendars"),
]
CAL_IDS = {"africa": "Africa", "americas": "Americas", "asiapacific": "AsiaPacific",
           "europe": "Europe", "middleeast": "MiddleEast"}

for slug, region, accent, hl, codes, lnote, caption in CAL_PACKS:
    pkg = "Bodu.Globalization.Calendar." + CAL_IDS[slug]
    add(f"hero-calendar-{slug}", pkg, f"{pkg} — embedded notable-date resource pack for {region}",
        accent, region,
        "\n".join([calendar_grid(accent, hl), note(lnote, x=12, y=148)]),
        "resolve", "import", "Territories",
        "\n".join([chips(codes, accent), note(CAL_IDS[slug] + "CalendarData", x=12, y=150)]),
        caption, gid="cal" + slug)

# --- Exchange rates ------------------------------------------------------------------------------------------

FX_PROVIDERS = [
    ("rba", "RBA", "Reserve Bank", "of Australia", "#FBBF24", ".xls workbook", "AUD/USD",
     "full history", "BIFF8 reader over", "AddRbaExchangeRates", "Reserve Bank of Australia", "Rba"),
    ("boe", "BOE", "Bank of England", "daily spot rates", "#A78BFA", "CSV", "GBP/USD",
     "full history", "IADB series codes", "AddBoeExchangeRates", "Bank of England", "Boe"),
    ("ecb", "ECB", "European Central", "Bank reference rates", "#60A5FA", "XML eurofxref", "EUR/USD",
     "full history", "90-day + full feeds", "AddEcbExchangeRates", "European Central Bank", "Ecb"),
    ("yahoo", "Yahoo", "Yahoo Finance", "market quotes", "#F472B6", "JSON v8 chart", "USD/JPY",
     "intraday + range", "symbol =X pairs", "AddYahooExchangeRates", "Yahoo Finance", "Yahoo"),
    ("ofx", "OFX", "OFX historical", "rate service", "#2DD4BF", "JSON", "AUD/NZD",
     "full history", "public rate API", "AddOfxExchangeRates", "OFX", "Ofx"),
    ("xe", "XE", "XE.com currency", "data service", "#F87171", "JSON + token", "USD/EUR",
     "full history", "account-scoped API", "AddXeExchangeRates", "XE.com", "Xe"),
    ("oanda", "OANDA", "OANDA fxDS", "historical rates", "#FB923C", "JSON · ~180d", "USD/CAD",
     "rolling window", "RateHistoryAvailability", "AddOandaExchangeRates", "OANDA", "Oanda"),
]

for slug, short, s1, s2, accent, chip, pair, history, pnote, register, desc, pid in FX_PROVIDERS:
    pkg = f"Bodu.Financial.ExchangeRates.{pid}"
    add(f"hero-fx-{slug}", pkg, f"{pkg} — {desc} exchange-rate provider",
        accent, short, provider_left(accent, s1, s2, chip, pair),
        "fetch", "parse", "Provider", provider_right(accent, history, pnote),
        f"services.{register}()", gid="fx" + slug)

add("hero-fx", "Bodu.Financial.ExchangeRates",
    "Bodu.Financial.ExchangeRates — web exchange-rate provider infrastructure",
    "#34D399", "WebRateProvider",
    "\n".join([
        '''    <g stroke="#334155" stroke-width="1">
      <rect x="10" y="38" width="130" height="24" rx="5" fill="#0F172A"/>
      <rect x="10" y="74" width="130" height="24" rx="5" fill="#0F172A"/>
      <rect x="10" y="110" width="130" height="24" rx="5" fill="#0F172A"/>
    </g>
    <g fill="#E2E8F0" font-family="'Consolas','Menlo',monospace" font-size="10">
      <text x="18" y="54">single-flight <tspan fill="#94A3B8">coalesce</tspan></text>
      <text x="18" y="90">byte cache <tspan fill="#94A3B8">on disk</tspan></text>
      <text x="18" y="126">pair loads <tspan fill="#94A3B8">· ranges</tspan></text>
    </g>
    <g fill="none" stroke="#34D399" stroke-width="1.4">
      <path d="M 75 62 L 75 74"/>
      <path d="M 75 98 L 75 110"/>
    </g>''',
        note("RateProviderHttpClientFactory", y=152)]),
    "fetch", "serve", "Provider base",
    mono(['<tspan fill="#60A5FA">WebRateProvider</tspan>', None,
          '<tspan fill="#60A5FA">PairWebRateProvider&lt;T&gt;</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">the base every source</tspan>',
          '<tspan fill="#94A3B8" font-size="9">package derives from</tspan>'], y0=44, dy=15, size=9.5),
    "base classes for BOE \u00b7 ECB \u00b7 RBA \u00b7 Yahoo \u00b7 OFX \u00b7 XE \u00b7 OANDA", gid="fx")

add("hero-fx-di", "Bodu.Financial.ExchangeRates.DependencyInjection",
    "Bodu.Financial.ExchangeRates.DependencyInjection — resilient HttpClient wiring for exchange-rate providers",
    "#34D399", "named HttpClient",
    "\n".join([
        '''    <g stroke="#334155" stroke-width="1">
      <rect x="10" y="38" width="130" height="24" rx="5" fill="#0F172A"/>
      <rect x="10" y="74" width="130" height="24" rx="5" fill="#0F172A"/>
      <rect x="10" y="110" width="130" height="24" rx="5" fill="#0F172A"/>
    </g>
    <g fill="#E2E8F0" font-family="'Consolas','Menlo',monospace" font-size="10">
      <text x="18" y="54">Polly retry <tspan fill="#94A3B8">backoff</tspan></text>
      <text x="18" y="90">circuit breaker</text>
      <text x="18" y="126">timeout <tspan fill="#94A3B8">· jitter</tspan></text>
    </g>
    <g fill="none" stroke="#34D399" stroke-width="1.4">
      <path d="M 75 62 L 75 74"/>
      <path d="M 75 98 L 75 110"/>
    </g>''',
        note("SingleFlightCoordinator", y=152)]),
    "register", "delegate", "Provider base",
    mono(["AddWebExchangeRate", "Provider&lt;T&gt;(…)", None,
          '<tspan fill="#60A5FA">WebRateProvider</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">every source package</tspan>',
          '<tspan fill="#94A3B8" font-size="9">builds on this wiring</tspan>'], y0=44, dy=15, size=9.5),
    "shared machinery for BOE · ECB · RBA · Yahoo · OFX · XE · OANDA", gid="fxdi")

add("hero-fx-caching", "Bodu.Financial.ExchangeRates.Caching",
    "Bodu.Financial.ExchangeRates.Caching — read-through caching and aggregation for exchange-rate providers",
    "#34D399", "IRateCache",
    "\n".join([
        '''    <g stroke="#334155" stroke-width="1">
      <rect x="10" y="38" width="130" height="26" rx="5" fill="#0F172A"/>
      <rect x="22" y="76" width="118" height="26" rx="5" fill="#0F172A"/>
    </g>
    <g fill="#E2E8F0" font-family="'Consolas','Menlo',monospace" font-size="10">
      <text x="20" y="55">memory <tspan fill="#34D399">hit</tspan></text>
      <text x="32" y="93">TOML file <tspan fill="#94A3B8">miss ↓</tspan></text>
    </g>
    <g fill="none" stroke="#34D399" stroke-width="1.4">
      <path d="M 75 64 L 75 76"/>
      <path d="M 75 102 L 75 114"/>
    </g>''',
        mono(["StoreFetchedRange", '<tspan fill="#94A3B8" font-size="9">DateRangeCoverage</tspan>'], y0=128, dy=15, size=10)]),
    "fetch", "store", "Providers",
    "\n".join([
        '''    <g stroke="#334155" stroke-width="1">
      <rect x="12" y="36" width="118" height="20" rx="4" fill="#0F172A"/>
      <rect x="12" y="62" width="118" height="20" rx="4" fill="#0F172A"/>
      <rect x="12" y="88" width="118" height="20" rx="4" fill="#0F172A"/>
    </g>
    <g fill="#E2E8F0" font-family="'Consolas','Menlo',monospace" font-size="9.5">
      <text x="20" y="50">ECB <tspan fill="#94A3B8">priority 1</tspan></text>
      <text x="20" y="76">BOE <tspan fill="#94A3B8">priority 2</tspan></text>
      <text x="20" y="102">RBA <tspan fill="#94A3B8">per-pair</tspan></text>
    </g>''',
        mono(['<tspan fill="#60A5FA">Aggregating</tspan>', '<tspan fill="#60A5FA">RateProvider</tspan>'],
             x=12, y0=128, dy=14, size=9.5)]),
    "AddCachedRateProvider · AddAggregatedRateProvider", gid="fxcache")

add("hero-fx-caching-distributed", "Bodu.Financial.ExchangeRates.Caching.Distributed",
    "Bodu.Financial.ExchangeRates.Caching.Distributed — IDistributedCache backend for exchange-rate caching",
    "#F87171", "IDistributedCache",
    "\n".join([
        '''    <g stroke="#F87171" stroke-width="1.4" fill="#0F172A">
      <g transform="translate(20 44)">
        <ellipse cx="18" cy="5" rx="18" ry="5"/>
        <path d="M 0 5 L 0 27 A 18 5 0 0 0 36 27 L 36 5" fill="#0F172A"/>
      </g>
      <g transform="translate(94 44)">
        <ellipse cx="18" cy="5" rx="18" ry="5"/>
        <path d="M 0 5 L 0 27 A 18 5 0 0 0 36 27 L 36 5" fill="#0F172A"/>
      </g>
      <g transform="translate(57 92)">
        <ellipse cx="18" cy="5" rx="18" ry="5"/>
        <path d="M 0 5 L 0 27 A 18 5 0 0 0 36 27 L 36 5" fill="#0F172A"/>
      </g>
    </g>
    <g stroke="#94A3B8" stroke-width="1.2" stroke-dasharray="3 3" fill="none">
      <line x1="58" y1="60" x2="92" y2="60"/>
      <line x1="42" y1="74" x2="70" y2="94"/>
      <line x1="108" y1="74" x2="80" y2="94"/>
    </g>''',
        note("Redis · SQL Server · any backend", x=12, y=148)]),
    "get", "set", "Rate cache",
    mono(['<tspan fill="#F87171">AddDistributedRateCache</tspan>', '<tspan fill="#F87171">AddRedisRateCache</tspan>', None,
          "StoreFetchedRange", "DateRangeCoverage", None,
          '<tspan fill="#94A3B8" font-size="9">shared across instances</tspan>'], y0=44, dy=16, size=9.5),
    "one durable rate cache for the whole fleet", gid="fxdist")

add("hero-fx-caching-sqlite", "Bodu.Financial.ExchangeRates.Caching.Sqlite",
    "Bodu.Financial.ExchangeRates.Caching.Sqlite — durable SQLite backend for exchange-rate caching",
    "#60A5FA", "rates.db",
    "\n".join([
        '''    <g stroke="#60A5FA" stroke-width="1.6" fill="#0F172A">
      <g transform="translate(47 40)">
        <ellipse cx="28" cy="8" rx="28" ry="8"/>
        <path d="M 0 8 L 0 50 A 28 8 0 0 0 56 50 L 56 8" fill="#0F172A"/>
        <ellipse cx="28" cy="29" rx="28" ry="8" fill="none" stroke-dasharray="3 3" stroke-width="1"/>
      </g>
    </g>''',
        mono(['rates <tspan fill="#94A3B8">(pair,date,mid)</tspan>',
              'coverage <tspan fill="#94A3B8">(pair,range)</tspan>'], x=14, y0=122, dy=17, size=9.5)]),
    "lookup", "persist", "Rate cache",
    mono(['<tspan fill="#60A5FA">AddSqliteRateCache(…)</tspan>', None, "CachingRate",
          'Provider <tspan fill="#94A3B8">read-through</tspan>', None,
          '<tspan fill="#94A3B8" font-size="9">survives restarts —</tspan>',
          '<tspan fill="#94A3B8" font-size="9">fetch once per range</tspan>'], y0=44, dy=16, size=9.5),
    "durable single-file cache · zero server", gid="fxsql")

# --- IO / formats --------------------------------------------------------------------------------------------

add("hero-io-compound", "Bodu.IO.Compound",
    "Bodu.IO.Compound — reader and writer for the OLE2 Compound File Binary container",
    "#60A5FA", "CompoundFile",
    mono(['<tspan fill="#60A5FA">Root Storage</tspan>',
          '<tspan fill="#94A3B8">├─</tspan> Workbook',
          '<tspan fill="#94A3B8">├─</tspan> SummaryInfo…',
          '<tspan fill="#94A3B8">├─</tspan> DocumentSummary…',
          '<tspan fill="#94A3B8">└─</tspan> ObjectPool', None,
          '<tspan fill="#94A3B8" font-size="9">.xls · .doc · .msg envelopes</tspan>'], y0=44, dy=17, size=10),
    "Open", "Create", "FAT sectors",
    "\n".join([
        '''    <g stroke="#334155" stroke-width="1">
      <rect x="14" y="36" width="24" height="20" rx="3" fill="#3B82F6"/>
      <rect x="44" y="36" width="24" height="20" rx="3" fill="#3B82F6"/>
      <rect x="74" y="36" width="24" height="20" rx="3" fill="#0F172A"/>
      <rect x="104" y="36" width="24" height="20" rx="3" fill="#3B82F6"/>
      <rect x="14" y="62" width="24" height="20" rx="3" fill="#0F172A"/>
      <rect x="44" y="62" width="24" height="20" rx="3" fill="#3B82F6"/>
      <rect x="74" y="62" width="24" height="20" rx="3" fill="#60A5FA"/>
      <rect x="104" y="62" width="24" height="20" rx="3" fill="#0F172A"/>
      <rect x="14" y="88" width="24" height="20" rx="3" fill="#3B82F6"/>
      <rect x="44" y="88" width="24" height="20" rx="3" fill="#0F172A"/>
      <rect x="74" y="88" width="24" height="20" rx="3" fill="#3B82F6"/>
      <rect x="104" y="88" width="24" height="20" rx="3" fill="#60A5FA"/>
    </g>''',
        mono(["CompoundStream", '<tspan fill="#94A3B8" font-size="9">mini-FAT · DIFAT · v3/v4</tspan>'],
             x=14, y0=128, dy=16, size=10)]),
    "OLE2 / CFB structured storage · staged builder", gid="iocmp")

add("hero-io-pst", "Bodu.IO.Pst",
    "Bodu.IO.Pst — read-only node-database reader for the Outlook PST container",
    "#A78BFA", "MS-PST NDB",
    mono(['<tspan fill="#94A3B8">!BDN</tspan> header · CRC',
          '<tspan fill="#94A3B8">NBT</tspan> node B-tree',
          '<tspan fill="#94A3B8">BBT</tspan> block B-tree',
          'permute · cyclic decode', 'XBLOCK data trees', None,
          '<tspan fill="#94A3B8" font-size="9">Unicode format (wVer 23)</tspan>'], y0=44, dy=17, size=10),
    "walk", "decode", "PstFile",
    mono(["EnumerateNodes()", "GetNode(nid)", "ReadAllBytes()",
          '<tspan fill="#60A5FA">subnode trees</tspan>',
          '<tspan fill="#94A3B8" font-size="9">Compatible / Strict / Minimal</tspan>',
          '<tspan fill="#94A3B8" font-size="9">no MAPI semantics · no writing</tspan>'], y0=44, dy=17, size=10),
    "node database · checksums verified · raw payloads", gid="iopst")

add("hero-excel", "Bodu.Formats.Excel.Binary",
    "Bodu.Formats.Excel.Binary — BIFF8 records decoded into worksheet cells",
    "#34D399", "BIFF8 records",
    mono(['<tspan fill="#94A3B8">FD 00</tspan> LABELSST', '<tspan fill="#94A3B8">7E 02</tspan> RK',
          '<tspan fill="#94A3B8">BD 00</tspan> MULRK', '<tspan fill="#94A3B8">06 00</tspan> FORMULA', None,
          '<tspan fill="#94A3B8" font-size="9">forward-only cursor over</tspan>',
          '<tspan fill="#94A3B8" font-size="9">the Workbook stream</tspan>'], y0=44, dy=17, size=10),
    "decode", "stream", "worksheet",
    "\n".join([
        '''    <g font-family="'Consolas','Menlo',monospace" font-size="9.5">
      <g fill="#94A3B8">
        <text x="38" y="46">A</text><text x="72" y="46">B</text><text x="108" y="46">C</text>
        <text x="14" y="64">1</text><text x="14" y="82">2</text><text x="14" y="100">3</text>
      </g>
      <g stroke="#334155" stroke-width="1" fill="none">
        <rect x="26" y="52" width="104" height="54" rx="3"/>
        <line x1="26" y1="70" x2="130" y2="70"/>
        <line x1="26" y1="88" x2="130" y2="88"/>
        <line x1="60" y1="52" x2="60" y2="106"/>
        <line x1="96" y1="52" x2="96" y2="106"/>
      </g>
      <text x="32" y="64" fill="#A78BFA">"USD"</text><text x="64" y="64" fill="#2DD4BF">0.68</text><text x="100" y="64" fill="#FBBF24">TRUE</text>
      <text x="32" y="82" fill="#2DD4BF">44929</text><text x="64" y="82" fill="#2DD4BF">112.7</text><text x="100" y="82" fill="#F87171">#N/A</text>
      <text x="32" y="100" fill="#A78BFA">"JPY"</text><text x="64" y="100" fill="#2DD4BF">0.70</text><text x="100" y="100" fill="#2DD4BF">8.5%</text>
    </g>''',
        mono(['<tspan fill="#94A3B8" font-size="9">cached formula results</tspan>'], x=14, y0=128, dy=14)]),
    "read-only BIFF8 · shared strings · used range", gid="xls")

add("hero-outlook", "Bodu.Formats.Outlook",
    "Bodu.Formats.Outlook — the shared MAPI value model for the Outlook format readers",
    "#60A5FA", "MapiPropertyTag",
    mono(['<tspan fill="#94A3B8">0x0037</tspan> 001F Subject', '<tspan fill="#94A3B8">0x0C1A</tspan> 001F SenderName',
          '<tspan fill="#94A3B8">0x3701</tspan> 0102 AttachData', '<tspan fill="#94A3B8">0x8000</tspan>+ named range', None,
          '<tspan fill="#94A3B8" font-size="9">id · type · multi-valued flag</tspan>'], y0=44, dy=17, size=10),
    "decode", "address", "Value model",
    mono(["MapiProperty", "MapiPropertyCollection", "MapiNamedProperty", "MapiPropertyIds",
          '<tspan fill="#2DD4BF">recipient · attachment</tspan>',
          '<tspan fill="#94A3B8" font-size="9">container-free · shared by</tspan>',
          '<tspan fill="#94A3B8" font-size="9">the .msg and .pst readers</tspan>'], y0=44, dy=17, size=10),
    "property tags · typed values · named identities", gid="olk")

add("hero-outlook-msg", "Bodu.Formats.Outlook.Msg",
    "Bodu.Formats.Outlook.Msg — the read-only .msg reader over the OLE2 container",
    "#2DD4BF", "MS-OXMSG streams",
    mono(["__properties_version1.0", "__substg1.0_0037001F", "__recip_version1.0_#…", "__attach_version1.0_#…",
          "__nameid_version1.0", None,
          '<tspan fill="#94A3B8" font-size="9">over Bodu.IO.Compound</tspan>'], y0=44, dy=17, size=9.5),
    "open", "decode", "OutlookMessage",
    mono(["Properties · Recipients", "Attachments · nested msgs", "named-property lookup",
          '<tspan fill="#60A5FA">BodyText · Html · Rtf</tspan>',
          '<tspan fill="#94A3B8" font-size="9">MS-OXRTFCP decompression</tspan>',
          '<tspan fill="#94A3B8" font-size="9">Compatible / Strict levels</tspan>'], y0=44, dy=17, size=10),
    "MAPI properties · recipients · attachments · bodies", gid="olkmsg")


def main():
    for name, s in sorted(B.items()):
        lheader = s["lheader"]
        rheader = s["rheader"]
        svg = TEMPLATE.format(
            aria=s["aria"], title=s["title"], gid=s["gid"], accent=s["accent"],
            ldark=DARK[s["accent"]], lheader=lheader,
            lhsize="10" if len(lheader) > 14 else "11",
            rhsize="10" if len(rheader) > 14 else "11",
            lbody=s["lbody"], up=s["up"], down=s["down"], rheader=rheader,
            rbody=s["rbody"], caption=s["caption"])
        with open(os.path.join(OUT, name + ".svg"), "w") as f:
            f.write(svg)
    print(f"wrote {len(B)} hero banners to {os.path.abspath(OUT)}")


if __name__ == "__main__":
    main()
