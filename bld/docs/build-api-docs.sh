#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------
# build-api-docs.sh
#
# Builds the DocFX site with its framework-aware API reference: the same stages, in the same
# order, locally and in .github/workflows/docfx-build-publish.yml.
#
#   build-api-docs.sh <stage>...
#
#   projects    choose the documented projects: every package in bld/release-manifest.txt, less
#               bld/docs/api-exclusions.txt (bld/docs/prepare_api_inputs.py projects)
#   assemblies  build those projects in Release for every framework in $(BoduNetTargets), check
#               each produced its .dll, .xml, and .pdb, resolve each package's id and version, and
#               stage everything DocFX reads under docs/obj/api/input/<tfm> (runs `projects` first)
#   metadata    run `docfx metadata` once per framework into docs/obj/api/metadata/<tfm>
#   merge       fold the frameworks into one API reference in docs/api, annotated with each API's
#               frameworks and package, failing on an unreviewed framework divergence
#   build       run `docfx build` over the conceptual docs and the merged API reference
#   validate    check the merged metadata and the rendered site (bld/docs/validate_api_site.py)
#   test        run the pipeline's own unit tests (bld/docs/tests)
#   all         assemblies, metadata, merge, build, validate
#   serve       serve docs/_site locally
#
# Requirements: the .NET SDK global.json pins and the .NET 8 runtime (the net8.0 references
# resolve against it), and Python 3.10+ with the packages in bld/docs/requirements.txt. Set
# PYTHON to use an interpreter other than python3, for example one in a virtual environment.
# DocFX is the local tool pinned in docs/.config/dotnet-tools.json.
# ---------------------------------------------------------------------------------------------
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
docs_dir="$repo_root/docs"
python="${PYTHON:-python3}"

# The documentation assemblies are compiled with BuildingForDocfx=true, which drops the C# 14
# extension blocks for their classic extension-method fallback (Directory.Build.targets explains
# why DocFX cannot render the blocks). Everything else about the build is the shipped Release build.
docs_build_properties="BuildingForDocfx=true"

stage_projects() {
    "$python" "$repo_root/bld/docs/prepare_api_inputs.py" projects
}

stage_assemblies() {
    stage_projects

    # Restore and build must agree on the configuration (see "Restore and build must agree" in
    # CLAUDE.md); the documentation build is Release.
    dotnet restore "$repo_root/bodu.slnx" -p:Configuration=Release -nologo -v:q
    dotnet msbuild "$repo_root/bld/docs/Bodu.Docs.Api.proj" -m -nologo -v:m \
        "-p:BoduDocsBuildProperties=$docs_build_properties"

    "$python" "$repo_root/bld/docs/prepare_api_inputs.py" stage
}

docfx() {
    (cd "$docs_dir" && dotnet tool restore >/dev/null && dotnet docfx "$@")
}

stage_metadata() {
    rm -rf "$docs_dir/obj/api/metadata"

    # DocFX turns each Source Link URL into a github.com/<repo>/blob/<ref>/ link, taking <ref> from
    # DOCFX_SOURCE_BRANCH_NAME or else GITHUB_REF_NAME. Under GitHub Actions the latter is a branch
    # ("master") or a pull request's merge ref, neither of which pins the source a page documents,
    # so the commit is named explicitly.
    DOCFX_SOURCE_BRANCH_NAME="$(git -C "$repo_root" rev-parse HEAD)" \
        docfx metadata obj/api/docfx.metadata.json --warningsAsErrors
}

stage_merge() {
    "$python" "$repo_root/bld/docs/merge_framework_metadata.py"
}

stage_build() {
    docfx build docfx.json --warningsAsErrors
}

stage_validate() {
    "$python" "$repo_root/bld/docs/validate_api_site.py"
}

stage_test() {
    "$python" -m unittest discover -s "$repo_root/bld/docs/tests"
}

stage_serve() {
    docfx serve _site
}

if [ "$#" -eq 0 ]; then
    sed -n '2,/^# ----/p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
    exit 2
fi

for stage in "$@"; do
    case "$stage" in
        projects|assemblies|metadata|merge|build|validate|test|serve)
            echo "::group::docs: $stage"
            "stage_$stage"
            echo "::endgroup::"
            ;;
        all)
            for s in assemblies metadata merge build validate; do
                echo "::group::docs: $s"
                "stage_$s"
                echo "::endgroup::"
            done
            ;;
        *)
            echo "Unknown stage '$stage'. Run with no arguments for usage." >&2
            exit 2
            ;;
    esac
done
