#!/usr/bin/env python3
"""Documentation guard rails for the DocFX site under docs/.

Eight checks, each independently runnable, all run by ``all``:

  orphans      every hand-authored page under docs/ is reachable from a TOC
  namespaces   every public namespace in the generated API metadata has an
               apidoc/ overview (uid: <Namespace>) so its page is not a bare
               type list
  identifiers  every backticked PascalCase identifier in the conceptual pages
               exists somewhere in the source tree (catches renamed or
               imagined members in prose)
  status       every package-matrix.md row agrees with the API-stability tier
               its package's README.md declares, and every other Stable /
               Preview / Experimental claim about a package agrees with
               docs/docs/package-matrix.md
  packages     every shipping package has a row in
               bld/docs-checks/package-docs-map.txt, and every page a row names
               exists
  publication  every packable package is either published or withheld in
               bld/release-manifest.txt, every published one has an install
               command, and no withheld one claims to be installable
  targets      every sentence or "Target frameworks" table column, in a
               package README or a page under docs/, that names the .NET
               frameworks a package targets names exactly $(BoduNetTargets)
               from bld/TargetFrameworks.props; and the opening paragraph of
               a package README, the repository README or a namespace
               overview names every one of those .NET versions, or the
               oldest as a minimum, when it names any
  sessions     every *.runsettings file at the repository root states its
               session limit in the comment above <TestSessionTimeout>, as
               "N minutes per assembly", and the value is that many minutes

Allow-lists live in bld/docs-checks/*.txt (one entry per line, ``#`` comments).
The namespace allow-list is *debt*: entries are namespaces that still lack an
overview and should be removed as overviews are written, never added to.

Usage:
  python3 bld/check-docs.py all            # after `bash bld/docs/build-api-docs.sh all`
  python3 bld/check-docs.py orphans identifiers status targets sessions   # no build needed
"""

from __future__ import annotations

import glob
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(ROOT, "docs")
CHECKS = os.path.join(ROOT, "bld", "docs-checks")

# Folders under docs/ that are not hand-authored consumer pages.
SKIP_DIRS = ("_site", "api", "apidoc", "templates", "reviews", "forensic-review", "obj", "bin")

STATUS_WORDS = ("Stable", "Preview", "Experimental")


def read(path: str) -> str:
    with open(path, encoding="utf-8", errors="ignore") as f:
        return f.read()


def load_list(name: str) -> set[str]:
    path = os.path.join(CHECKS, name)
    if not os.path.exists(path):
        return set()
    out: set[str] = set()
    for line in read(path).splitlines():
        line = line.split("#", 1)[0].strip()
        if line:
            out.add(line)
    return out


def docs_pages(include_apidoc: bool = False) -> list[str]:
    pages: list[str] = []
    for path in glob.glob(os.path.join(DOCS, "**", "*.md"), recursive=True):
        rel = os.path.relpath(path, DOCS)
        top = rel.split(os.sep, 1)[0]
        if top in SKIP_DIRS and not (include_apidoc and top == "apidoc"):
            continue
        pages.append(path)
    return sorted(pages)


# ---------------------------------------------------------------- orphans


def check_orphans() -> list[str]:
    referenced: set[str] = set()
    for toc in glob.glob(os.path.join(DOCS, "**", "toc.yml"), recursive=True):
        rel = os.path.relpath(toc, DOCS)
        if rel.split(os.sep, 1)[0] in SKIP_DIRS:
            continue
        base = os.path.dirname(toc)
        for href in re.findall(r"^\s*-?\s*href:\s*(\S+)", read(toc), re.M):
            href = href.strip("'\"")
            if href.startswith(("http://", "https://", "xref:")) or href.endswith("/"):
                continue
            referenced.add(os.path.normpath(os.path.join(base, href)))
    referenced.add(os.path.join(DOCS, "index.md"))  # the site home is the root page

    allow = load_list("orphan-allowlist.txt")
    problems = []
    for page in docs_pages():
        rel = os.path.relpath(page, DOCS).replace(os.sep, "/")
        if page in referenced or rel in allow:
            continue
        problems.append(f"{rel}: not referenced by any toc.yml (add it to a TOC or to bld/docs-checks/orphan-allowlist.txt)")
    return problems


# ------------------------------------------------------------- namespaces


def check_namespaces() -> list[str]:
    api_toc = os.path.join(DOCS, "api", "toc.yml")
    if not os.path.exists(api_toc):
        return [f"{os.path.relpath(api_toc, ROOT)} does not exist; run `bash bld/docs/build-api-docs.sh assemblies metadata merge` first"]

    # Namespace entries of the generated API TOC, at every depth: the TOC nests namespaces
    # (namespaceLayout: nested). Only namespaces with a page of their own are checked; the nested
    # layout also adds page-less parent nodes (Bodu.IO above Bodu.IO.Hashing) that have no page to
    # give an overview to. Namespaces outside the Bodu.* root (BCL-convention homes such as
    # Microsoft.Extensions.DependencyInjection) hold only registration extensions and are exempt by
    # design.
    namespaces = {
        uid
        for uid in re.findall(r"^\s*- uid: (\S+)\n\s+name: .*\n\s+type: Namespace", read(api_toc), re.M)
        if (uid.startswith("Bodu.") or uid == "Bodu") and os.path.exists(os.path.join(DOCS, "api", f"{uid}.yml"))
    }

    overwritten: set[str] = set()
    for md in glob.glob(os.path.join(DOCS, "apidoc", "*.md")):
        m = re.search(r"^uid:\s*([A-Za-z0-9_.`]+)", read(md), re.M)
        if m:
            overwritten.add(m.group(1))

    debt = load_list("namespace-overview-allowlist.txt")
    problems = []
    for ns in sorted(namespaces):
        if ns in overwritten or ns in debt:
            continue
        problems.append(f"namespace {ns} has no docs/apidoc overview (add apidoc/{ns}.md with `uid: {ns}`)")
    for ns in sorted(debt - namespaces):
        problems.append(f"bld/docs-checks/namespace-overview-allowlist.txt lists {ns}, which is not a generated Bodu namespace; remove it")
    for ns in sorted(debt & overwritten):
        problems.append(f"bld/docs-checks/namespace-overview-allowlist.txt lists {ns}, which now has an overview; remove it")
    return problems


# ------------------------------------------------------------ identifiers

# Names that are legitimately mentioned in prose but declared outside this repo.
BCL_NAMES = set(
    """
    ArgumentException ArgumentNullException ArgumentOutOfRangeException InvalidOperationException
    NotSupportedException FormatException IOException ObjectDisposedException OverflowException
    KeyNotFoundException OperationCanceledException CryptographicException JsonException
    OperationStatus CryptographicOperations SequenceEqual MidpointRounding NumberStyles
    ReadOnlySpan ReadOnlyMemory ReadOnlySequence IBufferWriter ArrayPool MemoryPool ArrayBufferWriter
    IEnumerable IAsyncEnumerable IReadOnlyList IReadOnlyCollection IReadOnlyDictionary IReadOnlySet
    ICollection IList IDictionary ISet HashSet SortedSet SortedDictionary SortedList LinkedList
    BlockingCollection ConcurrentQueue ConcurrentDictionary ConcurrentBag IProducerConsumerCollection
    KeyValuePair StringComparer StringComparison StringBuilder CultureInfo DateTimeOffset TimeSpan
    DateOnly TimeOnly TimeZoneInfo DateTimeKind DayOfWeek TimeProvider CancellationToken ValueTask
    ISpanFormattable IUtf8SpanFormattable ISpanParsable IUtf8SpanParsable IParsable IFormattable
    IEquatable IComparable IComparer IEqualityComparer IDisposable IAsyncDisposable
    INumber ISignedNumber IBinaryInteger IFloatingPoint IFloatingPointIeee754 IAdditionOperators
    BigInteger Int128 UInt128 CreateChecked CreateSaturating CreateTruncating MaxMagnitude MinMagnitude
    JsonSerializer JsonSerializerOptions JsonConverter JsonConverterFactory JsonNamingPolicy
    JsonIgnoreCondition JsonPropertyName JsonElement JsonDocument JsonNode Utf8JsonReader Utf8JsonWriter
    IServiceCollection IServiceProvider ServiceCollection IConfiguration IConfigurationBuilder
    IConfigurationSource IConfigurationProvider IConfigurationSection ConfigurationBuilder
    FileConfigurationSource FileConfigurationProvider IFileProvider PhysicalFileProvider AddJsonFile
    AddJsonStream AddIniFile AddEnvironmentVariables AddCommandLine SetBasePath ReloadOnChange
    ChangeToken IChangeToken IOptions IOptionsMonitor IOptionsSnapshot OptionsBuilder
    IHttpClientFactory HttpClient HttpMessageHandler HttpRequestMessage HttpResponseMessage
    ILogger ILoggerFactory LogLevel EventId IDistributedCache IMemoryCache MeterListener
    EnableMeasurementEvents SetMeasurementEventCallback ActivitySource Stopwatch
    SymmetricAlgorithm HashAlgorithm KeyedHashAlgorithm ICryptoTransform CryptoStream CryptoStreamMode
    AsymmetricAlgorithm IncrementalHash RandomNumberGenerator PaddingMode CipherMode AesGcm AesCcm
    ChaCha20Poly1305 HMACSHA256 HMACSHA512 SHA256 SHA512 SHA3_256 Shake128 Shake256 PemEncoding
    NonCryptographicHashAlgorithm XxHash32 XxHash64 XxHash3 XxHash128 Crc32 Crc64
    MemoryStream FileStream StreamReader StreamWriter TextReader TextWriter BinaryReader BinaryWriter
    Encoding UTF8Encoding UnicodeEncoding UTF32Encoding GetString GetBytes NewGuid
    XDocument XElement XAttribute XName XNamespace XmlReader XmlWriter XmlNameTable IXmlNamespaceResolver
    XPathSelectElements XmlSerializer Regex RegexOptions NonBacktracking
    Vector128 Vector256 Vector512 Avx2 Avx512F Sse2 Ssse3 Pclmulqdq AdvSimd MemoryMarshal BinaryPrimitives
    Interlocked Volatile ReaderWriterLockSlim SemaphoreSlim ManualResetEvent ManualResetEventSlim
    ThreadPool Parallel FakeTimeProvider
    TestMethod TestClass DataRow DynamicData TestCategory DataTestMethod ProjectReference PackageReference
    OrderBy OrderByDescending ThenBy GroupBy SelectMany FirstOrDefault LastOrDefault ToDictionary ToList
    ToArray ToHashSet TryGetValue GetValueOrDefault ContainsKey GetEnumerator MoveNext TryParse TryFormat
    ToString GetHashCode Equals CompareTo Dispose DisposeAsync ConfigureAwait
    InvariantCulture CurrentCulture Ordinal OrdinalIgnoreCase
    LinkedHashMap ChainMap ListMultimap SetMultimap TreeMap TreeSet
    WebApplication WebApplicationBuilder HostApplicationBuilder CreateDefaultBuilder CreateApplicationBuilder
    ConfigureAppConfiguration ConfigureServices IHostBuilder
    GetViewBetween Directory HostBuilder
    SqliteConnection ConnectionMultiplexer StackExchange.Redis SQLitePCLRaw Thread Program
    RSACryptoServiceProvider SHA3_512 MessagePackWriter JsonWriter Utf8Json
    Base32hex
    """.split()
)

IDENT_RE = re.compile(r"`([A-Z][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)*)(?:<[^`]*>)?(?:\([^`]*\))?`")


def source_identifier_bag() -> set[str]:
    """Every identifier-shaped token declared or used in the repository's C# (library,
    test, and sample sources), XML resource packs, MSBuild files, and project names."""
    bag: set[str] = set()
    patterns = [
        os.path.join(ROOT, "*", "src", "**", "*.cs"),
        os.path.join(ROOT, "*", "*", "src", "**", "*.cs"),
        os.path.join(ROOT, "*", "test", "**", "*.cs"),
        os.path.join(ROOT, "*", "*", "test", "**", "*.cs"),
        os.path.join(ROOT, "samples", "**", "*.cs"),
        os.path.join(ROOT, "*", "src", "**", "*.xml"),  # resource ids and rule names
        os.path.join(ROOT, "*", "*", "src", "**", "*.xml"),
        os.path.join(ROOT, "*", "src", "**", "*.targets"),
        os.path.join(ROOT, "*", "src", "**", "*.props"),
        os.path.join(ROOT, "*", "*", "src", "**", "*.targets"),
        os.path.join(ROOT, "*", "*", "src", "**", "*.props"),
        os.path.join(ROOT, "*", "build", "**", "*.targets"),  # MSBuild integration packages
        os.path.join(ROOT, "*", "build", "**", "*.props"),
    ]
    for pat in patterns:
        for path in glob.glob(pat, recursive=True):
            if os.sep + "obj" + os.sep in path or os.sep + "bin" + os.sep in path or "Bodu.CodeStyle" in path:
                continue
            bag.update(re.findall(r"[A-Za-z_][A-Za-z0-9_]+", read(path)))
    # Project / package ids and every dotted prefix of them (Bodu.IO, Bodu.IO.Pst, ...).
    for csproj in glob.glob(os.path.join(ROOT, "**", "*.csproj"), recursive=True):
        if "Bodu.CodeStyle" in csproj or os.sep + "archive" + os.sep in csproj:
            continue
        name = os.path.basename(csproj)[: -len(".csproj")]
        parts = name.split(".")
        for i in range(1, len(parts) + 1):
            bag.add(".".join(parts[:i]))
            bag.add(parts[i - 1])
    return bag


def strip_code(text: str) -> str:
    """Blank out fenced and indented code blocks, preserving line numbers."""
    def blank(m: re.Match) -> str:
        return "\n" * m.group(0).count("\n")
    text = re.sub(r"```.*?```", blank, text, flags=re.S)
    text = re.sub(r"^[ \t]{4,}\S.*$", "", text, flags=re.M)  # indented code blocks
    return text


def is_candidate(token: str) -> bool:
    """Only multi-hump identifiers are checked; single capitalised words, all-caps
    acronyms/constants, version-like tokens (V3), file names, and members of a type
    parameter (T.Zero) are prose."""
    if token.isupper() or re.fullmatch(r"[A-Z][0-9]+", token) or token.endswith(".cs"):
        return False
    if "." in token:
        first = token.split(".", 1)[0]
        return not (len(first) == 1 or first.isupper())
    humps = len(re.findall(r"[A-Z]", token))
    return humps >= 2 or bool(re.search(r"[0-9_]", token))


def check_identifiers() -> list[str]:
    bag = source_identifier_bag()
    allow = load_list("identifier-allowlist.txt") | BCL_NAMES
    problems = []
    for page in docs_pages(include_apidoc=True) + [os.path.join(DOCS, "index.md")]:
        rel = os.path.relpath(page, DOCS).replace(os.sep, "/")
        text = strip_code(read(page))
        for line_no, line in enumerate(text.splitlines(), 1):
            for token in IDENT_RE.findall(line):
                if token in allow or not is_candidate(token):
                    continue
                parts = token.split(".")
                if parts[0] in ("System", "Microsoft") or parts[0] in allow:
                    continue  # a member of an external type
                # A dotted token passes when it is a known package id or every
                # segment is a known identifier.
                if token in bag or all(p in bag or p in allow for p in parts):
                    continue
                problems.append(f"{rel}:{line_no}: `{token}` is not declared anywhere under */src (renamed? imagined? add to bld/docs-checks/identifier-allowlist.txt if it is an illustrative name)")
    return problems


# ----------------------------------------------------------------- status


def check_status() -> list[str]:
    matrix_path = os.path.join(DOCS, "docs", "package-matrix.md")
    matrix: dict[str, str] = {}
    for line in read(matrix_path).splitlines():
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.strip("|").split("|")]
        pkg = None
        status = None
        for c in cells:
            m = re.fullmatch(r"`(Bodu\.[A-Za-z0-9_.]+)`", c)
            if m and pkg is None:
                pkg = m.group(1)
            if c in STATUS_WORDS and status is None:
                status = c
        if pkg and status:
            matrix[pkg] = status
    if not matrix:
        return ["package-matrix.md: no package/status rows found; the parser needs updating"]

    problems = matrix_tier_problems(matrix)
    for page in docs_pages() + [os.path.join(DOCS, "index.md")]:
        if os.path.abspath(page) == os.path.abspath(matrix_path):
            continue
        rel = os.path.relpath(page, DOCS).replace(os.sep, "/")
        for line_no, line in enumerate(read(page).splitlines(), 1):
            claims = set(re.findall(r"\*\*(Stable|Preview|Experimental)\*\*", line))
            if line.startswith("|"):
                cells = [c.strip() for c in line.strip("|").split("|")]
                claims |= {c for c in cells if c in STATUS_WORDS}
            if not claims:
                continue
            if line.startswith("|"):
                # In a table the package column is the first backticked id; other ids
                # on the row are dependencies or prose.
                pkgs = re.findall(r"`(Bodu\.[A-Za-z0-9_.]+)`", line)[:1]
            else:
                pkgs = re.findall(r"`(Bodu\.[A-Za-z0-9_.]+)`", line)
            for pkg in pkgs:
                if pkg in matrix and matrix[pkg] not in claims:
                    problems.append(f"{rel}:{line_no}: `{pkg}` marked {'/'.join(sorted(claims))}, but package-matrix.md says {matrix[pkg]}")
    return problems


# The tier banner a package README opens with, in the grammar bld/check-release-manifest.sh reads
# (and holds to the version stream the package ships on), so the matrix and the release agree on
# one answer.
README_TIER = re.compile(r"API stability\s*-+\s*\*{0,2}(Stable|Preview|Experimental)")


def matrix_tier_problems(matrix: dict[str, str]) -> list[str]:
    """Holds each package-matrix.md row to the tier its package's README.md declares.

    The README is what nuget.org shows and what the release checks reconcile with the version a
    package ships at, so the matrix follows it. A row for a package that is not packable, or whose
    README has no banner, is skipped: the release-manifest check owns "every package has a tier".
    """
    packages = packable_package_ids()
    problems = []
    for pkg, status in sorted(matrix.items()):
        project = packages.get(pkg)
        if project is None:
            continue
        readme = os.path.join(os.path.dirname(os.path.dirname(project)), "README.md")
        if not os.path.exists(readme):
            continue
        match = README_TIER.search(read(readme)[:2000])
        if match and match.group(1) != status:
            problems.append(
                f"package-matrix.md: `{pkg}` marked {status}, but "
                f"{os.path.relpath(readme, ROOT).replace(os.sep, '/')} says {match.group(1)}")
    return problems


# --------------------------------------------------------------------- targets


# A target framework moniker written as inline code (`net10.0`, `netstandard2.0`), and a list of them.
_TFM_CODE = r"`net(?:standard)?\d+(?:\.\d+)?`"
_TFM_LIST = _TFM_CODE + r"(?:\s*(?:,\s*and|,|and)\s*" + _TFM_CODE + r")*"

# A claim about the frameworks a package targets - "Targets `net8.0` and `net10.0`.", "all packages target ...",
# "**Target frameworks.** ..." - and the list of monikers it names, which may wrap onto the next line.
FRAMEWORK_CLAIM = re.compile(r"\b[Tt]arget(?:s|\s+frameworks?)?\b[.:*\s]*(" + _TFM_LIST + ")")

# The header cell of a table column that lists each row's target frameworks.
FRAMEWORK_COLUMN = re.compile(r"^target frameworks?$", re.IGNORECASE)

# A modern .NET moniker. A claim naming only netstandard (a build-time task package) says nothing about
# $(BoduNetTargets), and a netstandard moniker beside the modern ones is a downlevel addition rather than one of them.
_MODERN_TFM = re.compile(r"`(net\d+\.\d+)`")


def net_targets() -> list[str]:
    """Returns ``$(BoduNetTargets)`` from ``bld/TargetFrameworks.props`` in declaration order, or an empty list."""
    match = re.search(r"<BoduNetTargets>([^<]+)</BoduNetTargets>", read(os.path.join(ROOT, "bld", "TargetFrameworks.props")))
    return [tfm.strip() for tfm in match.group(1).split(";") if tfm.strip()] if match else []


def framework_claim_problems(rel: str, text: str, expected: list[str]) -> list[str]:
    """Reports every claim in ``text`` whose .NET target frameworks are not exactly ``expected``.

    A claim is a sentence that names the frameworks after "target", "targets" or "Target frameworks", or a cell
    of a table column headed "Target frameworks". Code blocks are skipped, so a project file shown in a sample
    is not read as a claim about the package.
    """
    text = strip_code(text)
    claims: list[tuple[int, list[str]]] = []
    for match in FRAMEWORK_CLAIM.finditer(text):
        claims.append((text.count("\n", 0, match.start(1)) + 1, _MODERN_TFM.findall(match.group(1))))

    column: int | None = None
    for line_no, line in enumerate(text.splitlines(), 1):
        if not line.startswith("|"):
            column = None
            continue
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        header = next((i for i, cell in enumerate(cells) if FRAMEWORK_COLUMN.match(cell)), None)
        if header is not None:
            column = header
        elif column is not None and column < len(cells):
            claims.append((line_no, _MODERN_TFM.findall(cells[column])))

    problems = []
    for line_no, named in claims:
        if named and sorted(named) != sorted(expected):
            problems.append(
                f"{rel}:{line_no}: names {', '.join(named)} as the target frameworks, but $(BoduNetTargets) in "
                f"bld/TargetFrameworks.props is {', '.join(expected)}")
    return problems


# A .NET version named in prose: ".NET 8", ".NET 10". ".NET Framework", ".NET Standard" and ".NET Core" are other
# products, and do not match.
_NET_VERSION = re.compile(r"\.NET (\d+)(?:\.\d+)?\b")

# A .NET version stated as a minimum: ".NET 8 and later", ".NET 8 or later", ".NET 8+".
_NET_MINIMUM = re.compile(r"\.NET (\d+)(?:\.\d+)?(?:\+| (?:and|or) (?:later|newer))")


def opening_paragraph(text: str) -> tuple[int, list[str]] | None:
    """Returns the first line number and the lines of a page's opening paragraph, or ``None`` when it has none.

    The opening paragraph is the first block of prose. YAML front matter, headings, block quotes (the API-stability
    banner), images and badges, HTML, tables and lists can come before it, and are passed over.
    """
    lines = strip_code(text).splitlines()
    i = 0
    if lines and lines[0].strip() == "---":
        end = next((j for j in range(1, len(lines)) if lines[j].strip() == "---"), None)
        if end is not None:
            i = end + 1

    while i < len(lines):
        if not lines[i].strip():
            i += 1
            continue
        start = i
        while i < len(lines) and lines[i].strip():
            i += 1
        first = lines[start].lstrip()
        if not (first.startswith(("#", ">", "!", "[!", "<", "|", "- ", "* ", "+ ")) or re.match(r"\d+\. ", first)):
            return start + 1, lines[start:i]
    return None


def framework_opening_problems(rel: str, text: str, expected: list[str]) -> list[str]:
    """Reports a page whose opening paragraph names .NET versions other than every one of ``expected``.

    A package README's or a namespace overview's opening paragraph introduces the package, so the .NET versions it
    names read as the ones the package supports. It must name every version ``expected`` holds (".NET 8 and
    .NET 10"), or the oldest alone, as a minimum (".NET 8 and later"). ``framework_claim_problems`` reads the
    monikers a page writes as code (`net8.0`); this reads the product names.
    """
    opening = opening_paragraph(text)
    if opening is None:
        return []

    first_line, lines = opening
    paragraph = " ".join(line.strip() for line in lines)
    named = sorted({int(version) for version in _NET_VERSION.findall(paragraph)})
    if not named:
        return []

    versions = sorted(int(tfm[3:].split(".")[0]) for tfm in expected if re.fullmatch(r"net\d+\.\d+", tfm))
    minimums = {int(version) for version in _NET_MINIMUM.findall(paragraph)}
    if named == versions or (named == versions[:1] and minimums == set(named)):
        return []

    line_no = first_line + next((index for index, line in enumerate(lines) if _NET_VERSION.search(line)), 0)
    supported = " and ".join(f".NET {version}" for version in versions)
    return [
        f"{rel}:{line_no}: the opening paragraph names {', '.join(f'.NET {version}' for version in named)}, but "
        f"$(BoduNetTargets) in bld/TargetFrameworks.props is {', '.join(expected)}: name {supported}, or "
        f".NET {versions[0]} and later"]


def check_targets() -> list[str]:
    """Holds every statement of the frameworks a package targets to ``$(BoduNetTargets)``.

    Every project sets ``<TargetFrameworks>$(BoduNetTargets)</TargetFrameworks>``, so a package README (the page
    nuget.org shows), the repository README and the documentation site name those frameworks wherever they name
    any. Without this check the sentences go stale silently when the list changes, as they did when ``net10.0``
    joined ``net8.0``: first the monikers, then the product names that open a package's README and overview.
    """
    expected = net_targets()
    if not expected:
        return ["bld/TargetFrameworks.props: no <BoduNetTargets> found; the parser needs updating"]

    readmes = {os.path.join(os.path.dirname(os.path.dirname(project)), "README.md") for project in packable_package_ids().values()}
    readmes.add(os.path.join(ROOT, "README.md"))
    readmes = sorted(path for path in readmes if os.path.exists(path))
    overviews = sorted(glob.glob(os.path.join(DOCS, "apidoc", "*.md")))
    problems = []
    for page in readmes + docs_pages(include_apidoc=True):
        problems += framework_claim_problems(os.path.relpath(page, ROOT).replace(os.sep, "/"), read(page), expected)
    for page in readmes + overviews:
        problems += framework_opening_problems(os.path.relpath(page, ROOT).replace(os.sep, "/"), read(page), expected)
    return problems


# ------------------------------------------------------- packages (definition of done)


def packable_package_ids() -> dict[str, str]:
    """Maps each packable package id to the project file that ships it.

    The discovery rule matches the CI "Validate package inventory" step: every ``**/src/*.csproj``
    outside the archive and CodeStyle trees that does not opt out with ``<IsPackable>false</IsPackable>``,
    keyed by ``<PackageId>`` when the project overrides it (the regional calendar data packs do).
    """
    ids: dict[str, str] = {}
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in ("archive", "Bodu.CodeStyle", "obj", "bin", ".git")]
        if os.path.basename(dirpath) != "src":
            continue
        for name in filenames:
            if not name.endswith(".csproj"):
                continue
            path = os.path.join(dirpath, name)
            text = read(path)
            if re.search(r"<IsPackable>\s*false", text, re.IGNORECASE):
                continue
            match = re.search(r"<PackageId>([^<]+)</PackageId>", text)
            ids[match.group(1) if match else name[: -len(".csproj")]] = path
    return ids


def check_packages() -> list[str]:
    """Holds every shipping package to the documentation set a new package is expected to arrive with.

    Each package needs a row in ``bld/docs-checks/package-docs-map.txt`` naming its landing page, its
    guides, and its samples page. Family pages are shared by design, and a reviewed ``-`` records a
    deliberate absence; what the check forbids is a package with no row at all, a row pointing at a page
    that does not exist, and a row left behind by a package that no longer ships.
    """
    map_path = os.path.join(CHECKS, "package-docs-map.txt")
    if not os.path.exists(map_path):
        return [f"{os.path.relpath(map_path, ROOT)}: missing; the package definition-of-done map is required"]

    declared: dict[str, tuple[str, str, str]] = {}
    problems = []
    for line_no, raw in enumerate(read(map_path).splitlines(), 1):
        line = raw.split("#", 1)[0].strip()
        if not line:
            continue
        parts = line.split()
        if len(parts) != 4:
            problems.append(f"package-docs-map.txt:{line_no}: expected 4 columns, found {len(parts)}")
            continue
        declared[parts[0]] = (parts[1], parts[2], parts[3])

    packages = packable_package_ids()

    for pkg in sorted(set(packages) - set(declared)):
        problems.append(
            f"package-docs-map.txt: `{pkg}` is packable but has no row; add its landing page, guides, "
            f"and samples page (or a reviewed '-')")
    for pkg in sorted(set(declared) - set(packages)):
        problems.append(f"package-docs-map.txt: `{pkg}` has a row but is no longer a packable package; remove it")

    for pkg in sorted(set(declared) & set(packages)):
        landing, guides, samples = declared[pkg]
        if landing != "-" and not (
                os.path.exists(os.path.join(DOCS, "docs", landing, "index.md"))
                or os.path.exists(os.path.join(DOCS, "docs", f"{landing}.md"))):
            problems.append(f"package-docs-map.txt: `{pkg}` landing page 'docs/docs/{landing}' does not exist")
        if guides != "-" and not (
                os.path.isdir(os.path.join(DOCS, "guides", guides))
                or os.path.exists(os.path.join(DOCS, "guides", f"{guides}.md"))):
            problems.append(f"package-docs-map.txt: `{pkg}` guides 'docs/guides/{guides}' does not exist")
        if samples != "-" and not os.path.exists(os.path.join(DOCS, "samples", f"{samples}.md")):
            problems.append(f"package-docs-map.txt: `{pkg}` samples page 'docs/samples/{samples}.md' does not exist")

    return problems


# ------------------------------------------------- publication (manifest vs docs)


def release_manifest() -> tuple[dict[str, str], dict[str, str]]:
    """Reads ``bld/release-manifest.txt`` as the two sets of packages it records.

    Returns ``(published, withheld)``: published maps a package id to the version it first
    shipped at, withheld maps a package id to the reason it is kept off nuget.org.

    The grammar is the manifest's own, and is shared with the ``Select shipping packages`` step
    in ``.github/workflows/release.yml`` - a data line is ``<PackageId> <first-shipped-version>``,
    while a comment line that is *only* a package id opens a withheld entry whose reason is the
    wrapped comment lines beneath it. Parsing the same file both workflows already trust keeps
    "is this package published?" a single answer rather than a second list to maintain.
    """
    published: dict[str, str] = {}
    withheld: dict[str, str] = {}
    collecting: str | None = None

    for raw in read(os.path.join(ROOT, "bld", "release-manifest.txt")).splitlines():
        line = raw.rstrip()
        if line.lstrip().startswith("#"):
            stripped = line.lstrip()[1:].strip()
            if re.fullmatch(r"Bodu\.[A-Za-z0-9.]+", stripped):
                collecting = stripped
                withheld[collecting] = ""
            elif collecting and stripped:
                withheld[collecting] = (withheld[collecting] + " " + stripped).strip()
            continue

        collecting = None
        parts = line.split()
        if len(parts) >= 2 and parts[0].startswith("Bodu."):
            published[parts[0]] = parts[1]

    return published, withheld


# A withheld package is not on nuget.org, so the only install command that can work names a feed
# built from a local pack. Requiring that marker - rather than banning the command outright - keeps
# the one honest instruction (install the CLI from a clone) sayable, and rejects the plain form that
# silently fails for the reader.
LOCAL_SOURCE = re.compile(r"--add-source|--source\s+[.\w/\\]")


def install_commands(pkg: str, is_tool: bool) -> re.Pattern[str]:
    """Builds the pattern that a page uses to tell a reader to install ``pkg``."""
    # The trailing guard is "not another identifier character" rather than whitespace, so an id
    # quoted mid-sentence (`dotnet tool install --global Bodu.X`) counts the same as one on its own
    # line in a fenced block, while Bodu.X.Y is not mistaken for Bodu.X.
    escaped = re.escape(pkg)
    if is_tool:
        return re.compile(rf"dotnet tool install (?:--global |-g )?{escaped}(?![A-Za-z0-9_.])")
    return re.compile(rf"dotnet add package {escaped}(?![A-Za-z0-9_.])")


def check_publication() -> list[str]:
    """Keeps the documented install story in step with what nuget.org actually carries.

    Three failures are worth catching, and only the first was caught before:

    * a packable package the manifest does not account for at all - neither shipped nor
      recorded as deliberately withheld, so nobody decided either way;
    * a published package with no install command anywhere under ``docs/``, which leaves a
      reader with no way in;
    * a **withheld** package whose docs hand out an install command regardless. That is the one
      that reached the live site: ``dotnet add package Bodu.Financial.ExchangeRates.Oanda``
      names a package that has never been pushed, so the command simply fails.

    Withheld packages are documented - they are real code with real API pages - they just may not
    claim to be installable from nuget.org.
    """
    published, withheld = release_manifest()
    if not published:
        return ["release-manifest.txt: no published entries found; the parser needs updating"]

    packages = packable_package_ids()
    problems = []

    for pkg in sorted(set(packages) - set(published) - set(withheld)):
        problems.append(
            f"release-manifest.txt: `{pkg}` is packable but the manifest neither ships it nor records "
            f"it as withheld. Add it with its first-shipped version, or add a comment entry saying why "
            f"it is held back")
    for pkg in sorted(set(published) & set(withheld)):
        problems.append(f"release-manifest.txt: `{pkg}` is listed as shipping and also recorded as withheld")
    for pkg in sorted(set(published) - set(packages)):
        problems.append(f"release-manifest.txt: `{pkg}` is listed as shipping but is not a packable project")

    # Every packable package is listed in the matrix, published or not: the matrix is the page a
    # reader is sent to, so a package missing from it is invisible however well it is documented.
    matrix_text = read(os.path.join(DOCS, "docs", "package-matrix.md"))
    for pkg in sorted(packages):
        if f"`{pkg}`" not in matrix_text:
            problems.append(f"package-matrix.md: `{pkg}` is packable but is not listed")

    pages = docs_pages(include_apidoc=True)
    texts = {os.path.relpath(p, DOCS).replace(os.sep, "/"): read(p) for p in pages}

    for pkg in sorted(set(published) & set(packages)):
        is_tool = bool(re.search(r"<PackAsTool>\s*true", read(packages[pkg]), re.IGNORECASE))
        pattern = install_commands(pkg, is_tool)
        if not any(pattern.search(t) for t in texts.values()):
            verb = "dotnet tool install" if is_tool else "dotnet add package"
            problems.append(f"docs/: published package `{pkg}` has no '{verb} {pkg}' command anywhere under docs/")

    for pkg in sorted(set(withheld) & set(packages)):
        is_tool = bool(re.search(r"<PackAsTool>\s*true", read(packages[pkg]), re.IGNORECASE))
        pattern = install_commands(pkg, is_tool)
        for rel, text in sorted(texts.items()):
            for line_no, line in enumerate(text.splitlines(), 1):
                if pattern.search(line) and not LOCAL_SOURCE.search(line):
                    problems.append(
                        f"{rel}:{line_no}: `{pkg}` is withheld from nuget.org, so this command cannot work as "
                        f"written. Either drop it, or point it at a local feed with --add-source")

    # Phantom packages: an install command naming something this solution does not produce.
    for rel, text in sorted(texts.items()):
        for line_no, line in enumerate(text.splitlines(), 1):
            for m in re.finditer(r"dotnet (?:add package|tool install(?: --global| -g)?) (Bodu[A-Za-z0-9_.]*)", line):
                if m.group(1) not in packages:
                    problems.append(
                        f"{rel}:{line_no}: installs `{m.group(1)}`, which is not a packable project under "
                        f"**/src/. Stale or renamed package?")

    return problems


# ------------------------------------------------------- sessions (runsettings guards)


# The limit a runsettings file states for its own sessions: "10 minutes per assembly". A limit named in passing,
# such as stress.runsettings' "the 10-minute per-session guard used by the other tiers", is not written this way.
_SESSION_LIMIT = re.compile(r"(\d+) minutes per assembly")

# The comment immediately above the element, and the element's value in milliseconds.
_SESSION_TIMEOUT = re.compile(
    r"(?:<!--((?:(?!<!--|-->).)*)-->\s*)?<TestSessionTimeout>\s*(\d+)\s*</TestSessionTimeout>", re.DOTALL)


def session_timeout_problems(rel: str, text: str) -> list[str]:
    """Reports a runsettings file whose ``TestSessionTimeout`` is not the limit stated in the comment above it.

    The comment states the limit in minutes per assembly, and the documentation repeats it, while the value is
    written in milliseconds, where one zero too many goes unseen. A file that sets no limit is not read.
    """
    match = _SESSION_TIMEOUT.search(text)
    if match is None:
        return []

    line_no = text.count("\n", 0, match.start(2)) + 1
    value = int(match.group(2))
    limits = _SESSION_LIMIT.findall(match.group(1) or "")
    if len(limits) != 1:
        return [
            f"{rel}:{line_no}: the comment above <TestSessionTimeout> must state its limit once, as "
            f"\"N minutes per assembly\""]

    minutes = int(limits[0])
    if value != minutes * 60_000:
        return [
            f"{rel}:{line_no}: <TestSessionTimeout> is {value} ms ({value / 60_000:g} minutes), but the comment "
            f"above it states {minutes} minutes per assembly ({minutes * 60_000} ms)"]
    return []


def check_sessions() -> list[str]:
    """Holds each runsettings file's session limit to the limit its comment states.

    CI's Test step, the coverage workflow and the documented test tiers each run under one of the runsettings files
    at the repository root. A limit far longer than intended lets a hung test run until the job's own timeout,
    which names no test.
    """
    problems = []
    for name in sorted(n for n in os.listdir(ROOT) if n.endswith(".runsettings")):
        problems += session_timeout_problems(name, read(os.path.join(ROOT, name)))
    return problems


# ------------------------------------------------------------------- main


def main(argv: list[str]) -> int:
    wanted = [a for a in argv if not a.startswith("-")] or ["all"]
    if "all" in wanted:
        wanted = ["orphans", "namespaces", "identifiers", "status", "packages", "publication", "targets", "sessions"]
    runners = {
        "orphans": check_orphans,
        "namespaces": check_namespaces,
        "identifiers": check_identifiers,
        "status": check_status,
        "packages": check_packages,
        "publication": check_publication,
        "targets": check_targets,
        "sessions": check_sessions,
    }
    failed = 0
    for name in wanted:
        if name not in runners:
            print(f"unknown check '{name}'", file=sys.stderr)
            return 2
        problems = runners[name]()
        if problems:
            failed += 1
            print(f"[{name}] {len(problems)} problem(s):")
            for p in problems:
                print(f"  {p}")
        else:
            print(f"[{name}] ok")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
