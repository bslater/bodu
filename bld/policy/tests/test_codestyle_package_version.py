"""Tests that the Bodu.CodeStyle XML-documentation analyzer packs at the version the main solution restores.

The main solution restores Bodu.CodeStyle.XmlDocumentation from the bodu-local feed (local-packages/) at the version
Directory.Packages.props pins, and every CI job that builds it first packs the analyzer from source into that feed.
The pack only reaches the build when it writes the pinned version; otherwise the build restores whatever nupkg was
last committed. The package's version is evaluated by MSBuild as CI packs it: from Bodu.CodeStyle/, on the SDK its
global.json pins.
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import unittest
import xml.etree.ElementTree as ET
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
CODESTYLE = os.path.join(ROOT, "Bodu.CodeStyle")
PROJECT = os.path.join(CODESTYLE, "Bodu.CodeStyle.XmlDocumentation", "Bodu.CodeStyle.XmlDocumentation.csproj")
LOCAL_PACKAGES = os.path.join(ROOT, "local-packages")
PACKAGE_ID = "Bodu.CodeStyle.XmlDocumentation"


def central_pin() -> str:
    """Returns the version Directory.Packages.props pins for the analyzer package."""
    tree = ET.parse(os.path.join(ROOT, "Directory.Packages.props"))
    for element in tree.iter("PackageVersion"):
        if element.get("Include") == PACKAGE_ID:
            return element.get("Version", "")
    raise AssertionError(f"Directory.Packages.props has no PackageVersion for {PACKAGE_ID}")


def evaluate(*names: str) -> dict[str, str]:
    """Evaluates the packaging project's properties from Bodu.CodeStyle/, as CI's pack steps run."""
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        raise AssertionError("dotnet is not on PATH; install the SDK Bodu.CodeStyle/global.json pins")

    env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1")
    args = [dotnet, "msbuild", PROJECT, "-nologo", *(f"-getProperty:{name}" for name in names)]
    result = subprocess.run(args, cwd=CODESTYLE, env=env, capture_output=True, text=True)
    if result.returncode != 0:
        raise AssertionError(f"dotnet msbuild failed with exit code {result.returncode}:\n{result.stdout}{result.stderr}")

    return json.loads(result.stdout)["Properties"]


def committed_packages() -> list[str]:
    """Returns the analyzer nupkg file names committed to local-packages/."""
    prefix = PACKAGE_ID.lower() + "."
    return sorted(name for name in os.listdir(LOCAL_PACKAGES) if name.lower().startswith(prefix) and name.endswith(".nupkg"))


def nuspec_version(path: str) -> str:
    """Returns the version a nupkg's nuspec declares."""
    with zipfile.ZipFile(path) as package:
        nuspec = next(name for name in package.namelist() if name.endswith(".nuspec") and "/" not in name)
        root = ET.fromstring(package.read(nuspec))
    for element in root.iter():
        if element.tag.endswith("}version") or element.tag == "version":
            return (element.text or "").strip()
    raise AssertionError(f"{path} declares no version")


class CodeStylePackageVersionTests(unittest.TestCase):
    """The analyzer package's version, held to the pin the main solution restores."""

    def test_package_version_when_packed_as_ci_packs_it_should_equal_the_central_pin(self) -> None:
        properties = evaluate("PackageId", "PackageVersion")

        self.assertEqual(PACKAGE_ID, properties["PackageId"])
        self.assertEqual(central_pin(), properties["PackageVersion"])

    def test_committed_package_when_read_should_be_the_pinned_version_alone(self) -> None:
        pin = central_pin()

        self.assertEqual([f"{PACKAGE_ID}.{pin}.nupkg"], committed_packages())
        self.assertEqual(pin, nuspec_version(os.path.join(LOCAL_PACKAGES, f"{PACKAGE_ID}.{pin}.nupkg")))


if __name__ == "__main__":
    unittest.main()
