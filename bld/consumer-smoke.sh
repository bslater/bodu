#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------------------------
# consumer-smoke.sh
#
# Consumes the shipping packages the way a consumer does, and fails the way a consumer would. Every
# other check of a package runs inside the repository: the build resolves each library through a
# ProjectReference, the pack validates each package against its published baseline, and nothing ever
# restores the packed .nupkg into a project of its own. A packaging mistake that only a consumer sees -
# a dependency missing from the nuspec, an XML documentation file left out, an analyzer or build asset
# that ships wrongly, a framework the package claims but does not carry - therefore passes every
# repository build (#1362). This script is the consumer boundary.
#
# It creates, OUTSIDE the repository tree so that no Directory.Build.props, NuGet.config or analyzer of
# the repository applies, a scratch directory holding:
#   * a nuget.config whose only sources are the packages under test and nuget.org for their
#     dependencies, with every Bodu.* id mapped to the packages under test alone, so a version nuget.org
#     already publishes can never stand in for the one being checked;
#   * an empty NUGET_PACKAGES directory and an empty NuGet HTTP cache, so nothing restored or
#     downloaded earlier on this machine is reused; and
#   * per target framework, a console project with no ProjectReference that references every package
#     bld/release-manifest.txt lists as published, pinned to the version under test, plus a Program.cs
#     that touches one public type from a package in each domain and prints one line per package.
# Each project is restored, built with warnings as errors, and run; the first failure stops the script
# with a message naming the framework and the phase.
#
# Two modes select where the packages come from:
#   --feed    a directory of .nupkg files, as `dotnet pack bodu.slnx -c Release -p:BoduShipping=true
#             --output <dir>` writes it (what .github/workflows/build-test.yml's shipping-pack job
#             passes). Each package's version is read from its file name, so the two version streams
#             (bld/Versioning.props) need no stating.
#   --source  a NuGet v3 service index, for the post-publish check against nuget.org
#             (bld/RELEASING.md). The Stable stream version comes from --version; a package whose
#             csproj sets <BoduPackageVersionOverride> is taken at that override - $(BoduPreviewVersion)
#             from --preview-version, a literal as written - exactly as the pack resolves it.
#
# Usage:  bld/consumer-smoke.sh [options]
#   --feed <dir>               Directory of .nupkg files to consume. Required unless --source is given.
#   --source <url>             NuGet v3 service index to consume instead of a local feed
#                              (https://api.nuget.org/v3/index.json for the post-publish check).
#   --version <x.y.z>          With --source: the Stable stream version the packages were published at.
#   --preview-version <x.y.z>  With --source: the preview stream version. Default: BoduPreviewVersion
#                              in bld/Versioning.props.
#   --frameworks <list>        Target frameworks to consume on, separated by ';', ',' or spaces.
#                              Default: BoduNetTargets in bld/TargetFrameworks.props.
#   --output <dir>             Scratch directory for the configuration, cache and projects. It must be
#                              new or empty and outside the repository tree, and it is left in place
#                              for inspection. Default: a fresh directory under the system temp path.
#   -h | --help                Show this help.
#
# Needs the dotnet SDK on PATH, the runtime of every framework consumed (a framework whose runtime is
# missing cannot run, and a roll-forward onto another would check the wrong BCL) and network access to
# nuget.org for the dependencies.
#
# Exit code: 0 when every framework restored, built and ran; 1 on the first failure; 2 on a usage error.
# ---------------------------------------------------------------------------------------------------------------
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
manifest="$repo_root/bld/release-manifest.txt"
versioning="$repo_root/bld/Versioning.props"
target_frameworks_props="$repo_root/bld/TargetFrameworks.props"
nuget_org="https://api.nuget.org/v3/index.json"

feed=""
source_url=""
version=""
preview_version=""
frameworks=""
output_dir=""

usage() {
    sed -n '2,53p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
}

while [ $# -gt 0 ]; do
    case "$1" in
        --feed)            feed="${2:?--feed requires a directory}"; shift 2 ;;
        --source)          source_url="${2:?--source requires a NuGet v3 service index URL}"; shift 2 ;;
        --version)         version="${2:?--version requires a MAJOR.MINOR.PATCH version}"; shift 2 ;;
        --preview-version) preview_version="${2:?--preview-version requires a MAJOR.MINOR.PATCH version}"; shift 2 ;;
        --frameworks)      frameworks="${2:?--frameworks requires a list of target frameworks}"; shift 2 ;;
        --output)          output_dir="${2:?--output requires a directory}"; shift 2 ;;
        -h|--help)         usage; exit 0 ;;
        *) echo "error: unknown option '$1' (try --help)" >&2; exit 2 ;;
    esac
done

# ---------------------------------------------------------------------------------------------------------------
# Argument validation
# ---------------------------------------------------------------------------------------------------------------
usage_error() {
    echo "error: $1 (try --help)" >&2
    exit 2
}

is_version() {
    printf '%s' "$1" | grep -qE '^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$'
}

if ! command -v dotnet >/dev/null 2>&1; then
    usage_error "dotnet is not on PATH"
fi

if [ -n "$feed" ] && [ -n "$source_url" ]; then
    usage_error "--feed and --source are alternatives; pass one of them"
fi
if [ -z "$feed" ] && [ -z "$source_url" ]; then
    usage_error "pass --feed <dir of .nupkg> or --source <NuGet v3 URL>"
fi

if [ -n "$feed" ]; then
    if [ ! -d "$feed" ]; then
        usage_error "--feed '$feed' is not a directory"
    fi
    feed="$(cd "$feed" && pwd)"
    if [ -n "$version" ] || [ -n "$preview_version" ]; then
        usage_error "--version and --preview-version apply to --source; with --feed each package's version is read from its .nupkg file name"
    fi
else
    if [ -z "$version" ]; then
        usage_error "--source requires --version <x.y.z>, the Stable stream version the packages were published at"
    fi
    is_version "$version" || usage_error "--version '$version' is not MAJOR.MINOR.PATCH"
    if [ -z "$preview_version" ]; then
        preview_version="$(sed -n 's:.*<BoduPreviewVersion>\(.*\)</BoduPreviewVersion>.*:\1:p' "$versioning" | head -1)"
        if [ -z "$preview_version" ]; then
            echo "error: could not read BoduPreviewVersion from bld/Versioning.props; pass --preview-version" >&2
            exit 2
        fi
    fi
    is_version "$preview_version" || usage_error "--preview-version '$preview_version' is not MAJOR.MINOR.PATCH"
fi

# The frameworks BoduNetTargets lists, so widening the matrix there widens this check with it.
if [ -z "$frameworks" ]; then
    frameworks="$(sed -n 's:.*<BoduNetTargets>\(.*\)</BoduNetTargets>.*:\1:p' "$target_frameworks_props" | head -1)"
    if [ -z "$frameworks" ]; then
        echo "error: could not read BoduNetTargets from bld/TargetFrameworks.props; pass --frameworks" >&2
        exit 2
    fi
fi
framework_list=()
framework_names=""
for tfm in $(printf '%s' "$frameworks" | tr ';,' '  '); do
    framework_list+=("$tfm")
    framework_names="${framework_names:+$framework_names, }$tfm"
done
if [ "${#framework_list[@]}" -eq 0 ]; then
    usage_error "--frameworks names no target framework"
fi

# Makes a path absolute whether or not it exists yet: its deepest existing ancestor is canonicalized
# and the rest appended, so the repository check below runs before anything is created.
absolute_path() {
    local path="$1" rest=""

    while [ ! -d "$path" ]; do
        rest="$(basename "$path")${rest:+/$rest}"
        path="$(dirname "$path")"
    done
    path="$(cd "$path" && pwd)"
    printf '%s' "${path%/}${rest:+/$rest}"
}

# The scratch directory: outside the repository, so the repository's build and NuGet configuration
# cannot reach the consumer projects, and new or empty, so the cache really starts empty.
if [ -z "$output_dir" ]; then
    output_dir="$(mktemp -d -t bodu-consumer-smoke.XXXXXX)" || { echo "error: could not create a scratch directory" >&2; exit 1; }
else
    output_dir="$(absolute_path "$output_dir")"
fi
case "$output_dir/" in
    "$repo_root"/*) usage_error "--output '$output_dir' is inside the repository; the consumer must live outside it so that the repository's Directory.Build.props and NuGet.config do not apply" ;;
esac
mkdir -p "$output_dir" || { echo "error: could not create --output '$output_dir'" >&2; exit 1; }
if [ -n "$(ls -A "$output_dir")" ]; then
    usage_error "--output '$output_dir' is not empty; the consumer must start from a clean directory"
fi

# ---------------------------------------------------------------------------------------------------------------
# The packages: every data line of bld/release-manifest.txt, in its order. The comment block that
# records the deliberately withheld packages is skipped with every other comment, so a withheld package
# is never consumed, and a package the manifest does not list is a build artifact only.
# ---------------------------------------------------------------------------------------------------------------
if [ ! -f "$manifest" ]; then
    echo "error: bld/release-manifest.txt not found" >&2
    exit 1
fi

package_ids=()
while IFS= read -r raw; do
    line="${raw%%#*}"
    line="$(printf '%s' "$line" | tr -d '\r')"
    [ -z "$(printf '%s' "$line" | tr -d '[:space:]')" ] && continue
    read -r id _ <<<"$line"
    package_ids+=("$id")
done < "$manifest"

if [ "${#package_ids[@]}" -eq 0 ]; then
    echo "error: bld/release-manifest.txt lists no package" >&2
    exit 1
fi

# The version each package is consumed at.
#
# From a feed, the version is the one in the package's file name: the pack wrote it, so it is the
# version under test whatever stream the package ships on. A manifest package with no .nupkg in the feed
# is a pack that did not produce it, and two versions of one package are a feed that was packed into
# twice; both fail here, before any restore could pick one quietly.
#
# From a source, the version is resolved as the pack resolves it: BoduBaseVersion (here --version) unless
# the package's csproj sets <BoduPackageVersionOverride>, to $(BoduPreviewVersion) (here --preview-version)
# or to a literal out-of-band version. The csproj is found by search, as bld/check-release-manifest.sh
# finds it, because the regional calendar packs live under Bodu.Globalization.Calendar.Data/.
resolve_feed_version() {
    local id="$1" file name candidate found=""

    for file in "$feed/$id".*.nupkg; do
        [ -e "$file" ] || continue
        name="$(basename "$file" .nupkg)"
        candidate="${name#"$id".}"
        is_version "$candidate" || continue
        if [ -n "$found" ]; then
            echo "error: $feed holds $id at both $found and $candidate; pack into an empty directory" >&2
            return 1
        fi
        found="$candidate"
    done

    if [ -z "$found" ]; then
        echo "error: $feed holds no $id.<version>.nupkg, although bld/release-manifest.txt lists the package as published; the pack did not produce it" >&2
        return 1
    fi
    printf '%s' "$found"
}

src_projects=""
resolve_source_version() {
    local id="$1" project override

    if [ -z "$src_projects" ]; then
        src_projects="$(find "$repo_root" \( -name obj -o -name bin -o -name .git -o -name node_modules \) -prune -o -name '*.csproj' -path '*/src/*' -print 2>/dev/null)"
    fi
    project="$(printf '%s\n' "$src_projects" | grep -F "/$id.csproj" | head -1)"
    if [ -z "$project" ]; then
        echo "error: no $id.csproj found under any src/ directory, so its version stream cannot be resolved" >&2
        return 1
    fi

    override="$(sed -n 's:.*<BoduPackageVersionOverride>\(.*\)</BoduPackageVersionOverride>.*:\1:p' "$project" | head -1)"
    case "$override" in
        '')                      printf '%s' "$version" ;;
        '$(BoduPreviewVersion)') printf '%s' "$preview_version" ;;
        *)                       printf '%s' "$override" ;;
    esac
}

package_versions=()
for id in "${package_ids[@]}"; do
    if [ -n "$feed" ]; then
        resolved="$(resolve_feed_version "$id")" || exit 1
    else
        resolved="$(resolve_source_version "$id")" || exit 1
    fi
    package_versions+=("$resolved")
done

if [ -n "$feed" ]; then
    packages_from="$feed"
else
    packages_from="$source_url (Stable $version, preview $preview_version)"
fi

printf 'Consumer smoke\n'
printf '==============\n'
printf 'Packages:   %s from bld/release-manifest.txt\n' "${#package_ids[@]}"
printf 'From:       %s\n' "$packages_from"
printf 'Frameworks: %s\n' "$framework_names"
printf 'Scratch:    %s\n' "$output_dir"
printf '\n'
for i in "${!package_ids[@]}"; do
    printf '  %-55s %s\n' "${package_ids[$i]}" "${package_versions[$i]}"
done
printf '\n'

# ---------------------------------------------------------------------------------------------------------------
# The scratch tree
# ---------------------------------------------------------------------------------------------------------------
xml_escape() {
    printf '%s' "$1" | sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g; s/"/\&quot;/g'
}

# The configuration: the packages under test and nuget.org, nothing inherited from a configuration higher
# up (the <clear /> elements), and every Bodu.* id mapped to the packages under test alone, so that a
# version nuget.org already publishes cannot be restored in place of the one being checked. When the
# source IS nuget.org, one entry carries both patterns.
write_nuget_config() {
    local packages_key="bodu-packages" packages_entry=""

    if [ -n "$feed" ]; then
        packages_entry="    <add key=\"bodu-packages\" value=\"$(xml_escape "$feed")\" />"
    elif [ "${source_url%/}" != "${nuget_org%/}" ]; then
        packages_entry="    <add key=\"bodu-packages\" value=\"$(xml_escape "$source_url")\" protocolVersion=\"3\" />"
    else
        packages_key="nuget.org"
    fi

    {
        printf '<?xml version="1.0" encoding="utf-8"?>\n'
        printf '<!-- Written by bld/consumer-smoke.sh: the packages under test, and nuget.org for their dependencies. -->\n'
        printf '<configuration>\n'
        printf '  <packageSources>\n'
        printf '    <clear />\n'
        [ -n "$packages_entry" ] && printf '%s\n' "$packages_entry"
        printf '    <add key="nuget.org" value="%s" protocolVersion="3" />\n' "$nuget_org"
        printf '  </packageSources>\n'
        printf '  <packageSourceMapping>\n'
        printf '    <clear />\n'
        if [ "$packages_key" != "nuget.org" ]; then
            printf '    <packageSource key="%s">\n' "$packages_key"
            printf '      <package pattern="Bodu.*" />\n'
            printf '    </packageSource>\n'
            printf '    <packageSource key="nuget.org">\n'
            printf '      <package pattern="*" />\n'
            printf '    </packageSource>\n'
        else
            printf '    <packageSource key="nuget.org">\n'
            printf '      <package pattern="Bodu.*" />\n'
            printf '      <package pattern="*" />\n'
            printf '    </packageSource>\n'
        fi
        printf '  </packageSourceMapping>\n'
        printf '</configuration>\n'
    } > "$output_dir/nuget.config"
}

# One console project per framework, with no ProjectReference and every manifest package pinned to the
# exact version under test ([x.y.z]), so a floor can never float up to another version.
write_project() {
    local tfm="$1" dir="$2" i

    {
        printf '<Project Sdk="Microsoft.NET.Sdk">\n'
        printf '\n'
        printf '  <!-- Written by bld/consumer-smoke.sh: a consumer of the shipping packages with no project reference. -->\n'
        printf '  <PropertyGroup>\n'
        printf '    <OutputType>Exe</OutputType>\n'
        printf '    <TargetFramework>%s</TargetFramework>\n' "$tfm"
        printf '    <Nullable>enable</Nullable>\n'
        printf '    <ImplicitUsings>enable</ImplicitUsings>\n'
        printf '    <RootNamespace>Bodu.ConsumerSmoke</RootNamespace>\n'
        printf '  </PropertyGroup>\n'
        printf '\n'
        printf '  <ItemGroup>\n'
        for i in "${!package_ids[@]}"; do
            printf '    <PackageReference Include="%s" Version="[%s]" />\n' "${package_ids[$i]}" "${package_versions[$i]}"
        done
        printf '  </ItemGroup>\n'
        printf '\n'
        printf '</Project>\n'
    } > "$dir/ConsumerSmoke.csproj"
}

# The program: one public type from a package in each domain, each use printed on a line of its own,
# so the log of a run says which packages were loaded and what they answered.
write_program() {
    local dir="$1"

    cat > "$dir/Program.cs" <<'PROGRAM'
// Written by bld/consumer-smoke.sh. Touches one public type from a package in each domain, so a package
// whose assembly, dependencies or framework assets do not reach a consumer fails here rather than at a
// consumer's first restore. Every value printed comes from the package named on its line.
using System.Globalization;
using System.Text.Json;
using Bodu;
using Bodu.Collections.Generic;
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization.Json;
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Recurrence;
using Bodu.IO.Hashing.CheckDigits;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;
using Bodu.Security.Cryptography;
using Bodu.Text.Toml;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

Report("Bodu.Core", $"DayOfWeekSet.Weekdays = {DayOfWeekSet.Weekdays}");

var buffer = new CircularBuffer<int>(4);
buffer.Enqueue(1);
buffer.Enqueue(2);
Report("Bodu.Collections", $"CircularBuffer<int>.Count = {buffer.Count}");

var fraction = new Fraction<int>(3, 4) + new Fraction<int>(1, 4);
Report("Bodu.Numerics", $"3/4 + 1/4 = {fraction}");

var numericsJson = new JsonSerializerOptions().AddNumericsJsonConverters();
Report("Bodu.Numerics.Serialization.Json", $"Fraction<int> as JSON = {JsonSerializer.Serialize(fraction, numericsJson)}");

Report("Bodu.IO.Hashing", $"Luhn.IsValid(\"79927398713\") = {Luhn.IsValid("79927398713")}");

using var blake2b = new Blake2b();
var digest = Convert.ToHexString(blake2b.ComputeHash("abc"u8.ToArray()));
Report("Bodu.Security.Cryptography", $"Blake2b(\"abc\") = {digest[..16]}... ({digest.Length / 2} bytes)");

Report("Bodu.Text.Toml", $"TOML = {TomlSerializer.Serialize(new SmokeDocument("bodu", 1)).ReplaceLineEndings(" ").Trim()}");

var money = new Money(12.50m, CurrencyCode.AUD);
Report("Bodu.Financial", $"Money = {money.Amount} {money.Code}");

var financialJson = new JsonSerializerOptions().AddFinancialJsonConverters();
Report("Bodu.Financial.Serialization.Json", $"Money as JSON = {JsonSerializer.Serialize(money, financialJson)}");

var next = CronExpression.Parse("0 9 * * 1-5").GetNextOccurrence(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
Report("Bodu.Globalization.Recurrence", $"next weekday 09:00 after 2026-01-01 = {next?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "none"}");

NotableDateService service = AmericasCalendarData.CreateService("US");
var notableDates = service.Resolve(new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)), "US");
Report("Bodu.Globalization.Calendar", $"US July 2026 resolves {notableDates.Count} notable date(s): {string.Join(", ", notableDates.Select(d => d.NotableDateId))}");
Report("Bodu.Globalization.Calendar.Americas", $"{AmericasCalendarData.SupportedCountries.Count} supported countries");

return 0;

static void Report(string package, string detail) =>
    Console.WriteLine($"{package}: {detail}");

/// <summary>A document the TOML line serializes.</summary>
/// <param name="Name">A name.</param>
/// <param name="Version">A version.</param>
internal sealed record SmokeDocument(string Name, int Version);
PROGRAM
}

write_nuget_config
for tfm in "${framework_list[@]}"; do
    mkdir -p "$output_dir/$tfm"
    write_project "$tfm" "$output_dir/$tfm"
    write_program "$output_dir/$tfm"
done

# The clean cache: directories that did not exist a moment ago, so every package, Bodu's and its
# dependencies alike, is fetched from the configured sources and nothing restored or downloaded earlier
# on this machine is reused.
export NUGET_PACKAGES="$output_dir/packages"
export NUGET_HTTP_CACHE_PATH="$output_dir/http-cache"
mkdir -p "$NUGET_PACKAGES" "$NUGET_HTTP_CACHE_PATH"
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# ---------------------------------------------------------------------------------------------------------------
# Restore, build, run - per framework, stopping at the first failure
# ---------------------------------------------------------------------------------------------------------------
fail() {
    printf '\nerror: %s\n' "$1" >&2
    printf 'The scratch tree is left at %s for inspection.\n' "$output_dir" >&2
    exit 1
}

for tfm in "${framework_list[@]}"; do
    project="$output_dir/$tfm/ConsumerSmoke.csproj"

    printf '==> %s: restore from %s\n' "$tfm" "$packages_from"
    dotnet restore "$project" --configfile "$output_dir/nuget.config" \
        || fail "restoring the $tfm consumer failed: a package bld/release-manifest.txt lists is missing from $packages_from, or a dependency a nuspec declares cannot be resolved (see the NuGet output above)"

    printf '\n==> %s: build with warnings as errors\n' "$tfm"
    dotnet build "$project" --no-restore --configuration Release -warnaserror \
        || fail "building the $tfm consumer failed: a package's assembly, documentation file, analyzer or build asset is not what a consumer can compile against with warnings as errors (see the build output above)"

    printf '\n==> %s: run\n' "$tfm"
    dotnet run --project "$project" --no-build --configuration Release \
        || fail "the $tfm consumer did not run to completion: a package's runtime assets are wrong for $tfm, or the $tfm runtime is not installed (dotnet --list-runtimes)"
    printf '\n'
done

# ---------------------------------------------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------------------------------------------
version_summary="$(printf '%s\n' "${package_versions[@]}" | sort | uniq -c | awk '{ printf "%s%s at %s", (NR > 1 ? ", " : ""), $1, $2 }')"

printf 'Consumer smoke summary\n'
printf '======================\n'
printf '  Packages:    %s from bld/release-manifest.txt (%s)\n' "${#package_ids[@]}" "$version_summary"
printf '  Frameworks:  %s\n' "$framework_names"
printf '  From:        %s\n' "$packages_from"
printf '  Cache:       %s (started empty)\n' "$NUGET_PACKAGES"
printf '  Scratch:     %s\n' "$output_dir"
printf '  Result:      PASS\n'
exit 0
