// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReleaseNoteCorpusTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;
using Bodu.Text.DotEnv.Reader;

namespace Bodu.Text.DotEnv;

public sealed partial class DotEnvReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each cataloged input a fix established as readable is read by <c>Utf8DotEnvReader</c> into the
    /// expected entries.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ParseRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenFixScenarioIsRead_ShouldRenderTheExpectedEntries(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);

        string actual = Render(ReadEntries(input, options));

        Assert.AreEqual(Canonicalize(fix.Expected), EncodeText(actual), fix.ToString());
    }

    /// <summary>
    /// Verifies that each cataloged input a fix established as readable, read by <c>Utf8DotEnvReader</c> in blocks
    /// split every way and with a new reader after every token, reports exactly the tokens and lines a single read
    /// reports.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ParseRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenFixScenarioIsReadInBlocks_ShouldMatchTheSinglePassTranscript(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        DotEnvReaderOptions options = RowOptions.Parse(fix.Options).ToReaderOptions();
        byte[] input = CorpusEscapes.Decode(fix.Input);

        DotEnvReaderTranscript.AssertEverySplitMatches(input, options, fix.ToString());
    }
}
