// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationProfileGoldenTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Golden behaviour tests that pin the Bodu, EditorConfigCompatible, Strict, and Relaxed profiles against a
/// single shared fixture. The fixture is designed so each profile produces a *different* observable outcome
/// for at least one switch — preamble layering, inline comments, duplicate keys, <c>unset</c> handling, or
/// section-header trailing content. If a profile's preset drifts, exactly one of these tests fails and the
/// drifted axis is named.
/// </summary>
[TestClass]
public class ConfigurationProfileGoldenTests
{
    private const string PreambleAndSectionFixture = """
service.name = Bodu

[*.cs]
format.indent.size = 4

[generated/**]
format.indent.size = unset
""";

    /// <summary>
    /// Verifies that <see cref="ConfigurationProfile.Bodu" /> applies the preamble for non-matching target
    /// paths and treats <c>unset</c> as a literal value.
    /// </summary>
    [TestMethod]
    public void Bodu_WhenResolveWithGeneratedTarget_ShouldExposeUnsetLiteralAndPreamble()
    {
        var doc = ConfigurationDocument.Parse(PreambleAndSectionFixture, ConfigurationParseOptions.Bodu);
        ConfigurationView view = doc.Resolve("generated/Foo.cs", ConfigurationResolveOptions.Bodu);

        Assert.AreEqual("Bodu", view.GetString("service:name"));

        // Bodu treats unset as a literal — the [*.cs] section sets size=4 and [generated/**] sets
        // it to "unset" (literal string).
        Assert.AreEqual("unset", view.GetString("format:indent:size"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationProfile.EditorConfigCompatible" /> ignores preamble pairs for
    /// section-anchored resolution and treats <c>unset</c> as removing the effective value.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenResolveWithGeneratedTarget_ShouldHonourUnsetAndDropPreamble()
    {
        var doc = ConfigurationDocument.Parse(PreambleAndSectionFixture, ConfigurationParseOptions.EditorConfigCompatible);
        ConfigurationView view = doc.Resolve("generated/Foo.cs", ConfigurationResolveOptions.EditorConfigCompatible);

        // EditorConfigCompatible disables preamble layering for resolution.
        Assert.IsNull(view["service:name"]);

        // EditorConfigCompatible treats unset as RemoveEffectiveValue — the indent setting is removed.
        Assert.IsNull(view["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationProfile.Strict" /> rejects a duplicate section header that the
    /// other profiles accept.
    /// </summary>
    [TestMethod]
    public void Strict_WhenDuplicateSectionPresent_ShouldThrowDuplicateSection()
    {
        const string fixture = """
[*.cs]
format.indent.size = 4

[*.cs]
format.indent.size = 2
""";
        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationDocument.Parse(fixture, ConfigurationParseOptions.Strict);
        });

        Assert.AreEqual(ConfigurationDiagnosticCode.DuplicateSection, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationProfile.Strict" /> rejects a duplicate key that the
    /// Bodu/Relaxed/EditorConfigCompatible profiles accept (last-wins).
    /// </summary>
    [TestMethod]
    public void Strict_WhenDuplicateKeyPresent_ShouldThrowDuplicateKey()
    {
        const string fixture = """
[*]
format.indent.size = 4
format.indent.size = 2
""";
        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationDocument.Parse(fixture, ConfigurationParseOptions.Strict);
        });

        Assert.AreEqual(ConfigurationDiagnosticCode.DuplicateKey, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationProfile.Relaxed" /> collects diagnostics rather than throwing
    /// so that recoverable errors do not abort the parse.
    /// </summary>
    [TestMethod]
    public void Relaxed_WhenMalformedLinePresent_ShouldCollectAndContinue()
    {
        const string fixture = """
[*]
broken_no_equals
good = value
""";
        ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(fixture, ConfigurationParseOptions.Relaxed);

        Assert.IsGreaterThanOrEqualTo(1, result.Diagnostics.Length);
        Assert.AreEqual(ConfigurationDiagnosticCode.MissingEquals, result.Diagnostics[0].Code);

        // The good entry survives the malformed sibling.
        Assert.AreEqual("value", result.Document.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationProfile.EditorConfigCompatible" /> rejects an inline comment
    /// that the Bodu profile would strip — under EditorConfig 0.17.2, <c>#</c>/<c>;</c> are literal value
    /// characters.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenValueContainsHash_ShouldKeepHashAsLiteral()
    {
        const string fixture = """
[*]
key = value # not-a-comment
""";
        var doc = ConfigurationDocument.Parse(fixture, ConfigurationParseOptions.EditorConfigCompatible);

        Assert.AreEqual("value # not-a-comment", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationProfile.Bodu" /> strips an inline comment after whitespace.
    /// </summary>
    [TestMethod]
    public void Bodu_WhenValueContainsHashAfterSpace_ShouldStripInlineComment()
    {
        const string fixture = """
[*]
key = value # comment
""";
        var doc = ConfigurationDocument.Parse(fixture, ConfigurationParseOptions.Bodu);

        Assert.AreEqual("value", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationProfile.Bodu" /> accepts trailing words after a section
    /// header (Lenient mode), where <see cref="ConfigurationProfile.EditorConfigCompatible" /> would emit
    /// <see cref="ConfigurationDiagnosticCode.TrailingContentAfterSectionHeader" />.
    /// </summary>
    [TestMethod]
    public void Bodu_WhenSectionHeaderHasTrailingText_ShouldAcceptSilently()
    {
        const string fixture = """
[*.cs] trailing
key = value
""";
        var doc = ConfigurationDocument.Parse(fixture, ConfigurationParseOptions.Bodu);

        Assert.HasCount(1, doc.Sections);
        Assert.AreEqual("value", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationProfile.EditorConfigCompatible" /> emits the trailing-content
    /// diagnostic for the same input that <see cref="ConfigurationProfile.Bodu" /> accepts silently.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenSectionHeaderHasTrailingText_ShouldThrowTrailingContent()
    {
        const string fixture = """
[*.cs] trailing
key = value
""";
        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationDocument.Parse(fixture, ConfigurationParseOptions.EditorConfigCompatible);
        });

        Assert.AreEqual(ConfigurationDiagnosticCode.TrailingContentAfterSectionHeader, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that the preamble layering on/off switch is the only axis that differs between Bodu and
    /// EditorConfigCompatible when resolving with a non-matching target — pins the documented matrix.
    /// </summary>
    [TestMethod]
    public void Profiles_WhenNonMatchingTarget_ShouldDifferOnlyOnPreambleLayering()
    {
        const string fixture = "service.name = Bodu\n";

        var bodu = ConfigurationDocument.Parse(fixture, ConfigurationParseOptions.Bodu);
        var editorConfig = ConfigurationDocument.Parse(fixture, ConfigurationParseOptions.EditorConfigCompatible);

        ConfigurationView boduView = bodu.Resolve("README.md", ConfigurationResolveOptions.Bodu);
        ConfigurationView editorConfigView = editorConfig.Resolve("README.md", ConfigurationResolveOptions.EditorConfigCompatible);

        Assert.AreEqual("Bodu", boduView.GetString("service:name"));
        Assert.IsNull(editorConfigView["service:name"]);
    }

    /// <summary>
    /// Verifies the differences between Strict and Bodu resolve options. Strict keeps preamble layering on
    /// (matching Bodu) but tightens both the missing-path-root mode and the <c>unset</c> handling.
    /// </summary>
    [TestMethod]
    public void Strict_WhenResolveOptionsConstructed_ShouldDifferFromBoduOnStrictnessAxesOnly()
    {
        var strict = ConfigurationResolveOptions.For(ConfigurationProfile.Strict);
        var bodu = ConfigurationResolveOptions.For(ConfigurationProfile.Bodu);

        // Shared: preamble layering is enabled in both.
        Assert.AreEqual(bodu.ApplyPreambleProperties, strict.ApplyPreambleProperties);

        // Different: Strict requires a path root, Bodu treats its absence as the empty root.
        Assert.AreEqual(ConfigurationMissingPathRootMode.Throw, strict.MissingPathRootMode);
        Assert.AreEqual(ConfigurationMissingPathRootMode.UseEmptyRoot, bodu.MissingPathRootMode);

        // Different: Strict treats unset as RemoveEffectiveValue (per EditorConfig's stricter semantics),
        // Bodu treats it as a literal value.
        Assert.AreEqual(ConfigurationUnsetValueMode.RemoveEffectiveValue, strict.UnsetValueMode);
        Assert.AreEqual(ConfigurationUnsetValueMode.TreatAsLiteral, bodu.UnsetValueMode);
    }

    /// <summary>
    /// Verifies that the Relaxed profile uses the same resolve options as Bodu — it differs only on
    /// parse-side diagnostic routing (Collect vs Throw).
    /// </summary>
    [TestMethod]
    public void Relaxed_WhenResolveOptionsConstructed_ShouldMatchBoduResolveBehaviour()
    {
        var relaxed = ConfigurationResolveOptions.For(ConfigurationProfile.Relaxed);
        var bodu = ConfigurationResolveOptions.For(ConfigurationProfile.Bodu);

        Assert.AreEqual(bodu.ApplyPreambleProperties, relaxed.ApplyPreambleProperties);
        Assert.AreEqual(bodu.MissingPathRootMode, relaxed.MissingPathRootMode);
        Assert.AreEqual(bodu.UnsetValueMode, relaxed.UnsetValueMode);
    }
}
