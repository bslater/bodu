// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReaderTests.CollectMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Pins the reader's recovery paths: under collect-diagnostics the parser records a diagnostic and continues past the
/// offending line rather than throwing.
/// </summary>
[TestClass]
public sealed class ConfigurationReaderCollectModeTests
{
    private static bool HasCode(ConfigurationParseResult result, ConfigurationDiagnosticCode code)
    {
        foreach (ConfigurationDiagnostic diagnostic in result.Diagnostics)
        {
            if (diagnostic.Code == code)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Verifies that an over-long line is recorded and skipped under collect-diagnostics.
    /// </summary>
    [TestMethod]
    public void ParseWithDiagnostics_WhenLineTooLong_ShouldRecordAndContinue()
    {
        ConfigurationParseOptions options = new() { MaxLineLength = 5, DiagnosticMode = ConfigurationDiagnosticMode.Collect };

        ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics("abcdefghij = 1\n[*]\nk = 1\n", options);

        Assert.IsTrue(HasCode(result, ConfigurationDiagnosticCode.LineTooLong));
    }

    /// <summary>
    /// Verifies that an empty key is recorded and skipped under collect-diagnostics.
    /// </summary>
    [TestMethod]
    public void ParseWithDiagnostics_WhenKeyEmpty_ShouldRecordAndContinue()
    {
        ConfigurationParseOptions options = new() { DiagnosticMode = ConfigurationDiagnosticMode.Collect };

        ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics("[*]\n = value\n", options);

        Assert.IsTrue(HasCode(result, ConfigurationDiagnosticCode.EmptyKey));
    }

    /// <summary>
    /// Verifies that an over-long key is recorded and skipped under collect-diagnostics.
    /// </summary>
    [TestMethod]
    public void ParseWithDiagnostics_WhenKeyTooLong_ShouldRecordAndContinue()
    {
        ConfigurationParseOptions options = new() { MaxKeyLength = 3, DiagnosticMode = ConfigurationDiagnosticMode.Collect };

        ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics("[*]\nabcd = 1\n", options);

        Assert.IsTrue(HasCode(result, ConfigurationDiagnosticCode.KeyTooLong));
    }

    /// <summary>
    /// Verifies that a control character in a key is recorded and skipped under collect-diagnostics.
    /// </summary>
    [TestMethod]
    public void ParseWithDiagnostics_WhenKeyHasControlCharacter_ShouldRecordAndContinue()
    {
        ConfigurationParseOptions options = new() { DiagnosticMode = ConfigurationDiagnosticMode.Collect };
        string source = "[*]\na" + (char)1 + "b = 1\n";

        ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(source, options);

        Assert.IsTrue(HasCode(result, ConfigurationDiagnosticCode.InvalidKeyCharacter));
    }

    /// <summary>
    /// Verifies that a disallowed duplicate key is recorded and skipped under collect-diagnostics.
    /// </summary>
    [TestMethod]
    public void ParseWithDiagnostics_WhenDuplicateKeyDisallowed_ShouldRecordAndContinue()
    {
        ConfigurationParseOptions options = new()
        {
            DuplicateKeyMode = DuplicateKeyPolicy.Disallowed,
            DiagnosticMode = ConfigurationDiagnosticMode.Collect,
        };

        ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics("[*]\na = 1\na = 2\n", options);

        Assert.IsTrue(HasCode(result, ConfigurationDiagnosticCode.DuplicateKey));
    }
}
