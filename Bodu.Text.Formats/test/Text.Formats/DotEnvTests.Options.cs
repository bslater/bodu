// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvTests.Options.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

public sealed partial class DotEnvTests
{

    // ---------------------------------------- DuplicateKeyBehavior ----------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DotEnvDuplicateKeyBehavior.LastWins" /> causes the last value to win when the same
    /// key appears more than once.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyLastWins_ShouldReturnLastValue()
    {
        DotEnvParseOptions options = new() { DuplicateKeyBehavior = DotEnvDuplicateKeyBehavior.LastWins };

        DotEnvDocument doc = DotEnv.Parse("KEY=first\nKEY=second", options);

        Assert.AreEqual("second", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDuplicateKeyBehavior.LastWins" /> produces exactly one entry for a
    /// duplicated key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyLastWins_ShouldProduceSingleEntry()
    {
        DotEnvParseOptions options = new() { DuplicateKeyBehavior = DotEnvDuplicateKeyBehavior.LastWins };

        DotEnvDocument doc = DotEnv.Parse("KEY=first\nKEY=second", options);

        Assert.AreEqual(1, doc.Entries.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDuplicateKeyBehavior.FirstWins" /> causes the first value to be retained
    /// when the same key appears more than once.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyFirstWins_ShouldReturnFirstValue()
    {
        DotEnvParseOptions options = new() { DuplicateKeyBehavior = DotEnvDuplicateKeyBehavior.FirstWins };

        DotEnvDocument doc = DotEnv.Parse("KEY=first\nKEY=second", options);

        Assert.AreEqual("first", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDuplicateKeyBehavior.FirstWins" /> produces exactly one entry for a
    /// duplicated key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyFirstWins_ShouldProduceSingleEntry()
    {
        DotEnvParseOptions options = new() { DuplicateKeyBehavior = DotEnvDuplicateKeyBehavior.FirstWins };

        DotEnvDocument doc = DotEnv.Parse("KEY=first\nKEY=second", options);

        Assert.AreEqual(1, doc.Entries.Count);
    }

    // ------------------------------------------ AllowExportPrefix -----------------------------------------------

    /// <summary>
    /// Verifies that when <see cref="DotEnvParseOptions.AllowExportPrefix" /> is <see langword="true" /> (the
    /// default), a line starting with <c>export </c> is accepted and the key is parsed correctly.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowExportPrefixEnabled_ShouldStripExportAndParseKey()
    {
        DotEnvDocument doc = DotEnv.Parse("export HOST=localhost");

        Assert.AreEqual("localhost", doc["HOST"]);
    }

    /// <summary>
    /// Verifies that when <see cref="DotEnvParseOptions.AllowExportPrefix" /> is <see langword="false" />, a line
    /// starting with <c>export </c> is parsed as a regular key/value — treating <c>export</c> as the key name if
    /// followed immediately by <c>=</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowExportPrefixDisabled_ShouldTreatExportAsKey()
    {
        DotEnvParseOptions options = new() { AllowExportPrefix = false };

        DotEnvDocument doc = DotEnv.Parse("export=myvalue", options);

        Assert.AreEqual("myvalue", doc["export"]);
    }

    // ------------------------------------------ AllowInlineComments ---------------------------------------------

    /// <summary>
    /// Verifies that when <see cref="DotEnvParseOptions.AllowInlineComments" /> is <see langword="true" /> (the
    /// default), a <c>#</c> preceded by whitespace truncates an unquoted value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowInlineCommentsEnabled_ShouldTruncateValueAtHashPrecededBySpace()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=value # comment");

        Assert.AreEqual("value", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that when <see cref="DotEnvParseOptions.AllowInlineComments" /> is <see langword="false" />, a
    /// <c>#</c> preceded by whitespace is treated as part of the value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowInlineCommentsDisabled_ShouldIncludeHashInValue()
    {
        DotEnvParseOptions options = new() { AllowInlineComments = false };

        DotEnvDocument doc = DotEnv.Parse("KEY=value # notacomment", options);

        Assert.AreEqual("value # notacomment", doc["KEY"]);
    }

}
