#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------------------------
# coverage-scope.sh
#
# Decides what the Coverage workflow (.github/workflows/coverage.yml) collects. A scheduled or dispatched run
# collects every test project across SHARD_COUNT shards; a pull request's run collects only the test projects
# whose production sources the diff touched, or nothing, or defers the whole collection to the scheduled full
# run. The rule lived in the workflow's "Decide collection scope" step until bld/policy/tests/
# test_coverage_scope.py, which the Policy Gate runs, needed to hold it to its cases.
#
# On a pull request the changed paths arrive on standard input, one per line, as `git diff --name-only`
# prints them. They are read in full before anything is tested, and every test runs over a here-string of
# that copy rather than over a pipe: `echo "$changed" | grep -q` exits at its first match, and on a diff longer
# than a pipe buffer echo is still writing then, dies of SIGPIPE, and under `pipefail` turns the test false,
# so a run that should have skipped went on to collect every project the diff named (#1337).
#
# The decision, in order:
#   1. A change to shared build configuration (Directory.Build.props, Directory.Build.targets,
#      Directory.Packages.props, anything under bld/, coverage.runsettings) or to Bodu.Core/src fans out to
#      effectively every project, so a "diff-scoped" run would be a full run wearing a disguise. Defer to the
#      scheduled baseline instead of pretending to be quick: mode=skip, with the reason on standard error.
#   2. Otherwise each changed <Project>/src/** path maps to the <Project>/test/*.Test.csproj files beside it,
#      resolved against the current directory, so run the script from the repository root. Bodu.CodeStyle is
#      left out, as the full run (which collects bodu.slnx) leaves it out: it is a separate solution that
#      codestyle-build-test.yml builds and tests on its own SDK, and its test projects target net8.0 alone,
#      not the framework collect-coverage.sh builds (#1336). mode=diff names the test projects found, once
#      each, sorted, space-separated; mode=skip when none was.
#
# Usage:  git diff --name-only "origin/<base>...HEAD" | bld/coverage-scope.sh [options]
#   --event <name>   The GitHub event that started the run. Default: pull_request. Any other event is a
#                    full run, which reads nothing from standard input.
#   --shards <n>     The shard count of a full run. Default: $SHARD_COUNT. Required for a full run.
#   -h | --help      Show this help.
#
# Output: exactly three lines on standard output, in the form the workflow appends to $GITHUB_OUTPUT:
#   mode=full|diff|skip
#   projects=<space-separated test project paths, empty unless mode=diff>
#   shards=<1,2,...,n for a full run; 1 for a diff run; empty when skipped>
# Everything else the script says goes to standard error.
#
# Exit code: 0 when a decision was made, 2 for a usage error.
# ---------------------------------------------------------------------------------------------------------------
set -euo pipefail

event="pull_request"
shard_count="${SHARD_COUNT:-}"

usage() {
    # The header: from the opening banner line to the closing one.
    sed -n '2,/^# ----/p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
}

while [ $# -gt 0 ]; do
    case "$1" in
        --event)   event="${2:?--event requires a GitHub event name}"; shift 2 ;;
        --shards)  shard_count="${2:?--shards requires a count}"; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "error: unknown option '$1' (try --help)" >&2; exit 2 ;;
    esac
done

# A full run on schedule or dispatch: every shard, no project list.
if [ "$event" != "pull_request" ]; then
    if ! [[ "$shard_count" =~ ^[1-9][0-9]*$ ]]; then
        echo "error: a full run needs its shard count; set SHARD_COUNT or pass --shards <n>" >&2
        exit 2
    fi

    echo "mode=full"
    echo "projects="
    echo "shards=$(seq -s, 1 "$shard_count")"
    exit 0
fi

if [ -t 0 ]; then
    echo "error: pipe the changed paths, one per line, on standard input (try --help)" >&2
    exit 2
fi

# The whole list, read once; see the header for why every test below runs over a here-string of it.
changed=$(cat)

if grep -qE '^(Directory\.Build\.(props|targets)|Directory\.Packages\.props|bld/|coverage\.runsettings|Bodu\.Core/src/)' <<< "$changed"; then
    echo "Diff touches shared build configuration or Bodu.Core; deferring to the scheduled full run." >&2
    echo "mode=skip"
    echo "projects="
    echo "shards="
    exit 0
fi

# Map each changed <Project>/src/** path to that project's test project (see the header for the
# Bodu.CodeStyle exclusion). The glob stays unexpanded when a project has no test project, and the
# file test then skips it.
projects=""
for dir in $(grep -E '/src/' <<< "$changed" | sed -E '/^Bodu\.CodeStyle\//d; s|/src/.*||' | sort -u); do
    for candidate in "$dir"/test/*.Test.csproj; do
        [ -f "$candidate" ] && projects="$projects $candidate"
    done
done

# sed rather than `grep -v`: grep exits 1 when it filters every line out, and under `pipefail` that
# would end the script before the "nothing to collect" branch below can run.
projects=$(echo "$projects" | tr ' ' '\n' | sed '/^$/d' | sort -u | tr '\n' ' ' | sed 's/ $//')

if [ -z "$projects" ]; then
    echo "No production source changed; nothing to collect." >&2
    echo "mode=skip"
else
    echo "Collecting: $projects" >&2
    echo "mode=diff"
fi

echo "projects=$projects"
echo "shards=1"
