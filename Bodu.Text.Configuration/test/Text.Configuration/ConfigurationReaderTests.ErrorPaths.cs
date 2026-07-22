// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReaderTests.ErrorPaths.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Text.Configuration;

/// <summary>
/// Pins the configuration reader's limit, malformed-key, and duplicate-resolution paths.
/// </summary>
[TestClass]
public sealed class ConfigurationReaderErrorPathTests
{
    private static ConfigurationParseException ParseAndCapture(string source, ConfigurationParseOptions options) =>
        Assert.ThrowsExactly<ConfigurationParseException>(() => _ = ConfigurationDocument.Parse(source, options));

    /// <summary>
    /// Verifies that a line exceeding <see cref="ConfigurationParseOptions.MaxLineLength" /> emits the
    /// <see cref="ConfigurationDiagnosticCode.LineTooLong" /> diagnostic.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLineExceedsMaxLength_ShouldEmitLineTooLong()
    {
        ConfigurationParseOptions options = new() { MaxLineLength = 5, DiagnosticMode = ConfigurationDiagnosticMode.Throw };

        ConfigurationParseException ex = ParseAndCapture("abcdefghij = 1\n", options);

        Assert.AreEqual(ConfigurationDiagnosticCode.LineTooLong, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that a property line with an empty key emits <see cref="ConfigurationDiagnosticCode.EmptyKey" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyIsEmpty_ShouldEmitEmptyKey()
    {
        ConfigurationParseOptions options = new() { DiagnosticMode = ConfigurationDiagnosticMode.Throw };

        ConfigurationParseException ex = ParseAndCapture("[*]\n = value\n", options);

        Assert.AreEqual(ConfigurationDiagnosticCode.EmptyKey, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that a key exceeding <see cref="ConfigurationParseOptions.MaxKeyLength" /> emits
    /// <see cref="ConfigurationDiagnosticCode.KeyTooLong" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyExceedsMaxLength_ShouldEmitKeyTooLong()
    {
        ConfigurationParseOptions options = new() { MaxKeyLength = 3, DiagnosticMode = ConfigurationDiagnosticMode.Throw };

        ConfigurationParseException ex = ParseAndCapture("[*]\nabcd = 1\n", options);

        Assert.AreEqual(ConfigurationDiagnosticCode.KeyTooLong, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that a key containing a control character emits
    /// <see cref="ConfigurationDiagnosticCode.InvalidKeyCharacter" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyContainsControlCharacter_ShouldEmitInvalidKeyCharacter()
    {
        ConfigurationParseOptions options = new() { DiagnosticMode = ConfigurationDiagnosticMode.Throw };
        string source = "[*]\na" + (char)1 + "b = 1\n";

        ConfigurationParseException ex = ParseAndCapture(source, options);

        Assert.AreEqual(ConfigurationDiagnosticCode.InvalidKeyCharacter, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that a duplicate key under <see cref="DuplicateKeyPolicy.Disallowed" /> emits
    /// <see cref="ConfigurationDiagnosticCode.DuplicateKey" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyDisallowed_ShouldEmitDuplicateKey()
    {
        ConfigurationParseOptions options = new()
        {
            DuplicateKeyMode = DuplicateKeyPolicy.Disallowed,
            DiagnosticMode = ConfigurationDiagnosticMode.Throw,
        };

        ConfigurationParseException ex = ParseAndCapture("[*]\na = 1\na = 2\n", options);

        Assert.AreEqual(ConfigurationDiagnosticCode.DuplicateKey, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that a duplicate key under <see cref="DuplicateKeyPolicy.FirstWins" /> keeps the first value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyFirstWins_ShouldKeepFirstValue()
    {
        ConfigurationParseOptions options = new() { DuplicateKeyMode = DuplicateKeyPolicy.FirstWins };

        var document = ConfigurationDocument.Parse("[*]\na = 1\na = 2\n", options);

        Assert.AreEqual("1", document.Resolve("any.cs").GetString("a"));
    }

    /// <summary>
    /// Verifies that a duplicate key under <see cref="DuplicateKeyPolicy.LastWins" /> replaces the value, preserving
    /// the replacement's inline and leading comment trivia.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyLastWinsWithComments_ShouldKeepLastValue()
    {
        ConfigurationParseOptions options = new() { DuplicateKeyMode = DuplicateKeyPolicy.LastWins };

        var document = ConfigurationDocument.Parse("[*]\na = 1\n# lead\na = 2 ; inline\n", options);

        Assert.AreEqual("2", document.Resolve("any.cs").GetString("a"));
    }

    /// <summary>
    /// Verifies that a repeated section under <see cref="IniDuplicateSectionBehavior.MergeAll" /> merges its entries
    /// into the first occurrence.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionMergeAll_ShouldMergeEntries()
    {
        ConfigurationParseOptions options = new() { DuplicateSectionMode = IniDuplicateSectionBehavior.MergeAll };

        var document = ConfigurationDocument.Parse("[*.cs]\na = 1\n[other]\nx = 9\n[*.cs]\nb = 2\n", options);
        ConfigurationView view = document.Resolve("file.cs");

        Assert.AreEqual(("1", "2"), (view.GetString("a"), view.GetString("b")));
    }

    /// <summary>
    /// Verifies that an adjacent repeated section under <see cref="IniDuplicateSectionBehavior.MergeAdjacent" /> merges
    /// its entries into the preceding occurrence.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionMergeAdjacent_ShouldMergeEntries()
    {
        ConfigurationParseOptions options = new() { DuplicateSectionMode = IniDuplicateSectionBehavior.MergeAdjacent };

        var document = ConfigurationDocument.Parse("[*.cs]\na = 1\n[*.cs]\nb = 2\n", options);
        ConfigurationView view = document.Resolve("file.cs");

        Assert.AreEqual(("1", "2"), (view.GetString("a"), view.GetString("b")));
    }
}
