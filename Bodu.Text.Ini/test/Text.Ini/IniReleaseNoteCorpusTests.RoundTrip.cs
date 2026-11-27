// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReleaseNoteCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;
using Bodu.Text.Ini.Reader;

namespace Bodu.Text.Ini;

public sealed partial class IniReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing each cataloged input back with the writer writes the expected text, when the row gives it,
    /// and text that reads back to the same rendering as the input.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RoundTripRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenFixScenarioIsWrittenBack_ShouldReadBackTheSameRendering(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        string first = Render(input, options);
        byte[] written = WriteBack(input, options);
        string second = Render(written, options);

        if (expected.Length > 0)
            CollectionAssert.AreEqual(expected, written, $"{fix}: wrote {CorpusEscapes.Encode(written)}.");

        Assert.AreEqual(first, second, $"{fix}: wrote {CorpusEscapes.Encode(written)}.");
    }

    /// <summary>
    /// Verifies that each cataloged input a fix established as one to write back, read by <c>Utf8IniReader</c> in
    /// blocks split every way and with a new reader after every token, reports exactly what a single read reports.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RoundTripRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenFixScenarioIsReadInBlocks_ShouldMatchTheSinglePassTranscript(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        IniReaderOptions options = RowOptions.Parse(fix.Options).ToReaderOptions();
        byte[] input = CorpusEscapes.Decode(fix.Input);

        IniReaderTranscript.AssertEverySplitMatches(input, options, fix.ToString());
    }
}
