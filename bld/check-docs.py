#!/usr/bin/env python3
"""Documentation guard rails for the DocFX site under docs/.

Four checks, each independently runnable, all run by ``all``:

  orphans      every hand-authored page under docs/ is reachable from a TOC
  namespaces   every public namespace in the generated API metadata has an
               apidoc/ overview (uid: <Namespace>) so its page is not a bare
               type list
  identifiers  every backticked PascalCase identifier in the conceptual pages
               exists somewhere in the source tree (catches renamed or
               imagined members in prose)
  status       every Stable / Preview / Experimental claim about a package
               agrees with docs/docs/package-matrix.md, the source of truth

Allow-lists live in bld/docs-checks/*.txt (one entry per line, ``#`` comments).
The namespace allow-list is *debt*: entries are namespaces that still lack an
overview and should be removed as overviews are written, never added to.

Usage:
  python3 bld/check-docs.py all            # after `docfx docs/docfx.json`
  python3 bld/check-docs.py orphans identifiers status   # no build needed
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
        return [f"{os.path.relpath(api_toc, ROOT)} does not exist; run `docfx docs/docfx.json` (metadata) first"]

    # Top-level namespace entries of the generated API TOC. Namespaces outside the
    # Bodu.* root (BCL-convention homes such as Microsoft.Extensions.DependencyInjection)
    # hold only registration extensions and are exempt by design.
    namespaces = {
        uid
        for uid in re.findall(r"^- uid: (\S+)\n  name: .*\n  type: Namespace", read(api_toc), re.M)
        if uid.startswith("Bodu.") or uid == "Bodu"
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

    problems = []
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
    in ``.github/workflows/release.yml`` — a data line is ``<PackageId> <first-shipped-version>``,
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
# built from a local pack. Requiring that marker — rather than banning the command outright — keeps
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

    * a packable package the manifest does not account for at all — neither shipped nor
      recorded as deliberately withheld, so nobody decided either way;
    * a published package with no install command anywhere under ``docs/``, which leaves a
      reader with no way in;
    * a **withheld** package whose docs hand out an install command regardless. That is the one
      that reached the live site: ``dotnet add package Bodu.Financial.ExchangeRates.Oanda``
      names a package that has never been pushed, so the command simply fails.

    Withheld packages are documented — they are real code with real API pages — they just may not
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


# ------------------------------------------------------------------- main


def main(argv: list[str]) -> int:
    wanted = [a for a in argv if not a.startswith("-")] or ["all"]
    if "all" in wanted:
        wanted = ["orphans", "namespaces", "identifiers", "status", "packages", "publication"]
    runners = {
        "orphans": check_orphans,
        "namespaces": check_namespaces,
        "identifiers": check_identifiers,
        "status": check_status,
        "packages": check_packages,
        "publication": check_publication,
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
