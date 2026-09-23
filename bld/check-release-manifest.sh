#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------------------------
# check-release-manifest.sh
#
# Whole-tree gate on bld/release-manifest.txt and the release preconditions it implies. The manifest
# decides what reaches nuget.org and, through its first-shipped-version column, which packages get an
# ApiCompat baseline — so a malformed or incomplete entry is not a style problem, it is a bad release.
# A nuget.org version cannot be withdrawn afterwards, only delisted, which is why these are checked
# before a tag rather than discovered by a red release run after one.
#
# WHOLE-TREE, not diff-scoped (unlike check-policy.sh): the manifest is small, currently clean, and
# every line of it is load-bearing, so there is nothing to grandfather.
#
# Checks:
#   1. Format     — every entry is "<PackageId> <first-shipped-version>", version as MAJOR.MINOR.PATCH.
#   2. Unique     — no package id appears twice (a duplicate would publish twice and confuse the
#                   baseline lookup, which takes the first match).
#   3. Project    — every id resolves to a packable project (<Id>.csproj under a src/ directory).
#                   Note the id is NOT always the directory name: the regional calendar data packs
#                   live under Bodu.Globalization.Calendar.Data/, so this searches rather than assumes.
#   4. NotAhead   — no package claims a first-shipped version later than BoduBaseVersion. Such an entry
#                   would silently never receive a baseline, because the comparison in
#                   Directory.Build.targets would never be satisfied.
#   5. Readme     — every manifest package has a project-root README.md (bld/RELEASING.md
#                   precondition 4); it is packed as the NuGet readme and is a consumer's first page.
#   6. Tier       — that README carries an API-stability tier banner. A published package with no tier
#                   leaves a consumer unable to tell whether its surface is committed.
#   7. Icon       — bld/icons/<PackageId>.png exists. The docfx workflow checks the other direction
#                   (every .svg has a rasterized sibling); this checks that every SHIPPING package has
#                   an icon, which is the direction a release cares about.
#
# Withheld packages (named in the manifest's comment block) are deliberately absent and are not
# checked — they do not ship, so they owe consumers nothing.
#
# Usage:  bld/check-release-manifest.sh
# Exit code: 0 when the manifest is clean, 1 when one or more violations are found.
# ---------------------------------------------------------------------------------------------------------------
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
manifest="$repo_root/bld/release-manifest.txt"
versioning="$repo_root/bld/Versioning.props"

violations=0

# Emits a GitHub Actions error annotation plus a human-readable line.
fail() {
    local message="$1"
    printf '::error file=bld/release-manifest.txt::%s\n' "$message"
    printf '  VIOLATION: %s\n' "$message"
    violations=$((violations + 1))
}

if [ ! -f "$manifest" ]; then
    printf '::error::bld/release-manifest.txt not found\n'
    exit 1
fi

base_version="$(sed -n 's:.*<BoduBaseVersion>\(.*\)</BoduBaseVersion>.*:\1:p' "$versioning" | head -1)"
if [ -z "$base_version" ]; then
    printf '::error file=bld/Versioning.props::BoduBaseVersion could not be read\n'
    exit 1
fi

printf 'Release manifest check\n'
printf '======================\n'
printf 'BoduBaseVersion: %s\n' "$base_version"

# Compares two MAJOR.MINOR.PATCH versions; prints 1 when $1 > $2, else 0.
version_gt() {
    local a="$1" b="$2"
    [ "$a" = "$b" ] && { printf '0'; return; }
    if [ "$(printf '%s\n%s\n' "$a" "$b" | sort -V | head -1)" = "$a" ]; then printf '0'; else printf '1'; fi
}

entries=0
seen_ids=" "

while IFS= read -r raw; do
    line="${raw%%#*}"
    line="$(printf '%s' "$line" | tr -d '\r')"
    [ -z "$(printf '%s' "$line" | tr -d '[:space:]')" ] && continue

    entries=$((entries + 1))
    read -r id version extra <<<"$line"

    # 1. Format.
    if [ -n "${extra:-}" ]; then
        fail "$id: unexpected trailing text '$extra' (expected '<PackageId> <first-shipped-version>')"
        continue
    fi
    if [ -z "${version:-}" ]; then
        fail "$id: missing first-shipped version (expected '<PackageId> <first-shipped-version>')"
        continue
    fi
    if ! printf '%s' "$version" | grep -qE '^[0-9]+\.[0-9]+\.[0-9]+$'; then
        fail "$id: first-shipped version '$version' is not MAJOR.MINOR.PATCH"
        continue
    fi

    # 2. Unique.
    case "$seen_ids" in
        *" $id "*) fail "$id: listed more than once" ;;
        *) seen_ids="$seen_ids$id " ;;
    esac

    # 3. Project — resolved by search, because the id is not always the directory name.
    project="$(find "$repo_root" -name "$id.csproj" -path '*/src/*' -not -path '*/obj/*' -not -path '*/bin/*' 2>/dev/null | head -1)"
    if [ -z "$project" ]; then
        fail "$id: no <Id>.csproj found under any src/ directory — is the id spelled correctly?"
        continue
    fi
    project_root="$(dirname "$(dirname "$project")")"

    # 4. NotAhead.
    if [ "$(version_gt "$version" "$base_version")" = "1" ]; then
        fail "$id: first-shipped version $version is ahead of BoduBaseVersion $base_version — it would never receive a package-validation baseline"
    fi

    # 5/6. README and its tier banner.
    readme="$project_root/README.md"
    if [ ! -f "$readme" ]; then
        fail "$id: no README.md at $(realpath --relative-to="$repo_root" "$project_root") (bld/RELEASING.md precondition 4)"
    elif ! head -c 2000 "$readme" | grep -qE 'API stability[[:space:]]*[—-]+[[:space:]]*\*{0,2}(Stable|Preview|Experimental)'; then
        fail "$id: README.md carries no API-stability tier banner (Stable / Preview / Experimental)"
    fi

    # 7. Icon.
    if [ ! -f "$repo_root/bld/icons/$id.png" ]; then
        fail "$id: no bld/icons/$id.png (packed as the NuGet icon)"
    fi
done < "$manifest"

printf -- '----------------------\n'
printf 'Manifest entries checked: %d\n' "$entries"

if [ "$entries" -eq 0 ]; then
    printf '::error file=bld/release-manifest.txt::The manifest has no entries — a release would publish nothing\n'
    exit 1
fi

if [ "$violations" -gt 0 ]; then
    printf 'FAILED: %d violation(s).\n' "$violations"
    exit 1
fi

printf 'OK: no violations.\n'
