#!/bin/bash
# Installs the .NET SDK 8.0 on session start so the agent can run `dotnet
# build` and `dotnet test` against Bodu.sln. Only runs in the remote Claude
# Code on the web environment; on a developer's local machine the SDK is
# expected to be installed already.
#
# The script is idempotent: when `dotnet` is already on PATH it exits
# immediately, so re-invocation (resume, clear, compact) is essentially free.
set -euo pipefail

# Only act in the remote environment; local sessions are left untouched.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
    exit 0
fi

# Fast path: SDK already installed.
if command -v dotnet >/dev/null 2>&1; then
    exit 0
fi

echo "[session-start] dotnet not found; installing dotnet-sdk-8.0 via apt..."

export DEBIAN_FRONTEND=noninteractive

# Refresh apt only if no recent lists are present, to keep re-runs cheap.
# Tolerate third-party PPA failures (deadsnakes, ondrej, etc.) — the main
# Ubuntu archive is sufficient for dotnet-sdk-8.0.
if [ -z "$(find /var/lib/apt/lists -maxdepth 1 -type f -mmin -1440 2>/dev/null)" ]; then
    apt-get update -qq || echo "[session-start] apt-get update reported errors; continuing with existing cache."
fi

if ! apt-get install -y --no-install-recommends dotnet-sdk-8.0; then
    echo "[session-start] dotnet-sdk-8.0 install failed. If this is a transient cache issue, retry the session." >&2
    exit 1
fi

# Suppress first-run telemetry/welcome work so subsequent `dotnet` commands
# don't pay that cost or write to stdout in unexpected places.
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    {
        echo "export DOTNET_NOLOGO=1"
        echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
        echo "export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
    } >> "$CLAUDE_ENV_FILE"
fi

echo "[session-start] dotnet $(dotnet --version) installed."
