// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;
using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing back the records read from each cataloged input, with the row's dialect, writes bytes
    /// that read back to the same records, and the expected bytes when the row gives them.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RoundTripRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenFixScenarioIsWrittenBack_ShouldReadBackTheSameRecords(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        FramedDocument first = ReadDocument(input, options.ToReaderOptions());
        byte[] written = WriteRecords(first.Records, options.ToWriterOptions());
        FramedDocument second = ReadDocument(written, options.ToReaderOptions());

        string firstRendering = Escape(Render(first, options.View));
        string secondRendering = Escape(Render(second, options.View));
        Assert.AreEqual(firstRendering, secondRendering, $"{fix}: read {firstRendering}, wrote {Escape(written)}, then read {secondRendering}.");

        if (expected.Length > 0)
            Assert.AreEqual(Escape(expected), Escape(written), $"{fix}: the written bytes differ from the expected ones.");
    }

    /// <summary>
    /// Verifies that each cataloged input a fix established as one to write back, read by <c>Utf8DelimitedReader</c> in
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

        DelimitedReaderOptions options = RowOptions.Parse(fix.Options).ToReaderOptions();
        byte[] input = CorpusEscapes.Decode(fix.Input);

        DelimitedReaderTranscript.AssertEverySplitMatches(input, options, fix.ToString());
    }
}
