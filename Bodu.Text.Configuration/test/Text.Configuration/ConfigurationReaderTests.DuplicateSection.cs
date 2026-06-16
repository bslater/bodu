// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReaderTests.DuplicateSection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Pins the duplicate-section diagnostic contract. Duplicate section headers under
/// <see cref="IniDuplicateSectionBehavior.Disallowed" /> must surface as
/// <see cref="ConfigurationDiagnosticCode.DuplicateSection" /> rather than the unrelated
/// <see cref="ConfigurationDiagnosticCode.UnterminatedSectionHeader" /> code.
/// </summary>
[TestClass]
public class ConfigurationReaderDuplicateSectionTests
{
    private const string DuplicateSectionFixture = """
[*.cs]
format.indent.size = 4

[*.cs]
format.indent.style = space
""";

    /// <summary>
    /// Verifies that when duplicate section headers are disallowed and the parser throws, the wrapped
    /// diagnostic carries <see cref="ConfigurationDiagnosticCode.DuplicateSection" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionDisallowedAndThrow_ShouldUseDuplicateSectionCode()
    {
        ConfigurationParseOptions options = new()
        {
            DuplicateSectionMode = IniDuplicateSectionBehavior.Disallowed,
            DiagnosticMode = ConfigurationDiagnosticMode.Throw,
        };

        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationDocument.Parse(DuplicateSectionFixture, options);
        });

        Assert.IsNotNull(ex.Diagnostic);
        Assert.AreEqual(ConfigurationDiagnosticCode.DuplicateSection, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that when duplicate section headers are disallowed and diagnostics are collected, the parse
    /// result reports <see cref="ConfigurationDiagnosticCode.DuplicateSection" /> at least once and continues
    /// parsing past the duplicate.
    /// </summary>
    [TestMethod]
    public void ParseWithDiagnostics_WhenDuplicateSectionDisallowedAndCollect_ShouldReportDuplicateSectionAndContinue()
    {
        ConfigurationParseOptions options = new()
        {
            DuplicateSectionMode = IniDuplicateSectionBehavior.Disallowed,
            DiagnosticMode = ConfigurationDiagnosticMode.Collect,
        };

        ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(DuplicateSectionFixture, options);

        bool hasDuplicateSection = false;
        foreach (ConfigurationDiagnostic d in result.Diagnostics)
        {
            if (d.Code == ConfigurationDiagnosticCode.DuplicateSection)
            {
                hasDuplicateSection = true;
                break;
            }
        }

        Assert.IsTrue(hasDuplicateSection, "Expected at least one DuplicateSection diagnostic.");

        // The duplicate-section diagnostic must never masquerade as UnterminatedSectionHeader.
        foreach (ConfigurationDiagnostic d in result.Diagnostics)
            Assert.AreNotEqual(ConfigurationDiagnosticCode.UnterminatedSectionHeader, d.Code);
    }

    /// <summary>
    /// Verifies that an unterminated section header still emits
    /// <see cref="ConfigurationDiagnosticCode.UnterminatedSectionHeader" /> — the introduction of
    /// <see cref="ConfigurationDiagnosticCode.DuplicateSection" /> must not regress the original code.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSectionHeaderIsUnterminated_ShouldStillUseUnterminatedSectionHeaderCode()
    {
        ConfigurationParseOptions options = new()
        {
            DiagnosticMode = ConfigurationDiagnosticMode.Throw,
        };

        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationDocument.Parse("[unterminated\nkey = value\n", options);
        });

        Assert.IsNotNull(ex.Diagnostic);
        Assert.AreEqual(ConfigurationDiagnosticCode.UnterminatedSectionHeader, ex.Diagnostic!.Code);
    }
}
