"""Tests for bld/docs/prepare_api_inputs.py and the framework rules in bld/docs/docs_common.py."""

from __future__ import annotations

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import docs_common as dc  # noqa: E402
import prepare_api_inputs as p  # noqa: E402

TFMS = ["net8.0", "net10.0"]


def build(package_id: str = "Bodu.Alpha", version: str = "1.0.0", override: str = "", assembly: str = "Bodu.Alpha") -> dict[str, str]:
    """Builds one Assembly row of inputs.tsv."""
    return {"package_id": package_id, "package_version": version, "version_override": override, "assembly": assembly}


def project(package_id: str = "Bodu.Alpha", published: bool = True) -> p.DocumentedProject:
    return p.DocumentedProject(package_id, f"/repo/{package_id}/src/{package_id}.csproj", published)


class FrameworkTests(unittest.TestCase):
    """Framework names and precedence are derived from the TFM, never listed by hand."""

    def test_parse_framework_when_net10_should_derive_reader_names(self) -> None:
        """Verifies that net10.0 reads as .NET 10 with the net-10.0 view token."""
        fw = dc.parse_framework("net10.0")
        self.assertEqual((fw.product, fw.version, fw.label, fw.view), (".NET", "10", ".NET 10", "net-10.0"))

    def test_parse_framework_when_minor_version_should_keep_it(self) -> None:
        """Verifies that a framework with a minor version keeps it in its label."""
        self.assertEqual(dc.parse_framework("net10.1").label, ".NET 10.1")

    def test_parse_framework_when_not_net_moniker_should_raise(self) -> None:
        """Verifies that a netstandard or malformed moniker is rejected rather than mislabelled."""
        for tfm in ("netstandard2.0", "net48", "net8.0-windows"):
            with self.subTest(tfm=tfm), self.assertRaises(dc.PipelineError):
                dc.parse_framework(tfm)

    def test_frameworks_newest_first_when_future_framework_added_should_order_by_version(self) -> None:
        """Verifies that precedence follows the version number, not the string, so net12.0 outranks net8.0."""
        self.assertEqual([fw.tfm for fw in dc.frameworks_newest_first(["net8.0", "net12.0", "net10.0"])], ["net12.0", "net10.0", "net8.0"])


class PackageValidationTests(unittest.TestCase):
    """validate_packages() resolves one package id and version per assembly, or reports why it cannot."""

    def test_validate_when_builds_agree_should_resolve_the_package(self) -> None:
        """Verifies that consistent builds resolve to the assembly's package, id and version."""
        packages, problems = p.validate_packages({"Bodu.Alpha": {t: build() for t in TFMS}}, [project()], TFMS)
        self.assertEqual(problems, [])
        self.assertEqual(packages, {"Bodu.Alpha": {"id": "Bodu.Alpha", "version": "1.0.0", "published": True}})

    def test_validate_when_override_applies_should_use_the_resolved_prerelease_version(self) -> None:
        """Verifies that a project-specific prerelease version is reported exactly, not replaced by a shared one."""
        rows = {t: build(version="1.6.0-preview.3", override="1.6.0") for t in TFMS}
        packages, problems = p.validate_packages({"Bodu.Alpha": rows}, [project()], TFMS)
        self.assertEqual(problems, [])
        self.assertEqual(packages["Bodu.Alpha"]["version"], "1.6.0-preview.3")

    def test_validate_when_versions_differ_between_frameworks_should_fail(self) -> None:
        """Verifies that one package resolving to two versions across frameworks is an inconsistent build."""
        rows = {"net8.0": build(version="1.0.0"), "net10.0": build(version="1.0.1")}
        _, problems = p.validate_packages({"Bodu.Alpha": rows}, [project()], TFMS)
        self.assertTrue(any("differs between frameworks" in problem for problem in problems), problems)

    def test_validate_when_version_is_empty_should_fail(self) -> None:
        """Verifies that an empty resolved version is rejected."""
        _, problems = p.validate_packages({"Bodu.Alpha": {t: build(version="") for t in TFMS}}, [project()], TFMS)
        self.assertTrue(any("empty PackageVersion" in problem for problem in problems), problems)

    def test_validate_when_override_is_not_reflected_should_fail(self) -> None:
        """Verifies that a version override the resolved version ignores is caught, not silently replaced."""
        rows = {t: build(version="1.0.0", override="1.1.0") for t in TFMS}
        _, problems = p.validate_packages({"Bodu.Alpha": rows}, [project()], TFMS)
        self.assertTrue(any("BoduPackageVersionOverride" in problem for problem in problems), problems)

    def test_validate_when_package_id_differs_from_manifest_should_fail(self) -> None:
        """Verifies that an assembly whose package id is not the manifest's id is rejected."""
        _, problems = p.validate_packages({"Bodu.Alpha": {t: build(package_id="Bodu.Other") for t in TFMS}}, [project()], TFMS)
        self.assertTrue(any("not exactly the id" in problem for problem in problems), problems)

    def test_validate_when_framework_build_is_missing_should_fail(self) -> None:
        """Verifies that a project the build produced for only one framework is reported."""
        _, problems = p.validate_packages({"Bodu.Alpha": {"net8.0": build()}}, [project()], TFMS)
        self.assertTrue(any("no assembly for net10.0" in problem for problem in problems), problems)

    def test_validate_when_package_is_withheld_should_mark_it_unpublished(self) -> None:
        """Verifies that a package the manifest withholds from nuget.org is marked unpublished."""
        packages, _ = p.validate_packages({"Bodu.Alpha": {t: build() for t in TFMS}}, [project(published=False)], TFMS)
        self.assertFalse(packages["Bodu.Alpha"]["published"])


class FusionVersionTests(unittest.TestCase):
    """fusion_version() orders reference assemblies by version when two projects resolve different ones."""

    def test_fusion_version_when_versions_differ_should_order_numerically(self) -> None:
        """Verifies that 10.0.0.0 outranks 9.0.0.0 (numeric, not lexical, comparison)."""
        self.assertGreater(p.fusion_version("System.Text.Json, Version=10.0.0.0, Culture=neutral"), p.fusion_version("System.Text.Json, Version=9.0.0.0, Culture=neutral"))

    def test_fusion_version_when_no_version_should_return_empty(self) -> None:
        """Verifies that a fusion name without a version sorts lowest."""
        self.assertEqual(p.fusion_version("Something"), ())


if __name__ == "__main__":
    unittest.main()
