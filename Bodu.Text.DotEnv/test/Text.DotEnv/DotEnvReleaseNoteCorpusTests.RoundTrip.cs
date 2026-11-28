// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReleaseNoteCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
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
    /// Verifies that writing back the entries read from each cataloged input emits bytes that read back to the same
    /// entries, and the expected bytes when the row gives them.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RoundTripRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenFixScenarioIsWrittenBack_ShouldReadBackTheSameEntries(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);

        List<Entry> entries = ReadEntries(input, options);
        byte[] written = Write(entries, options);
        List<Entry> readBack = ReadEntries(written, options);

        Assert.AreEqual(
            EncodeText(Render(entries)),
            EncodeText(Render(readBack)),
            $"{fix}: wrote {CorpusEscapes.Encode(written)}.");

        if (fix.Expected.Length > 0)
            Assert.AreEqual(Canonicalize(fix.Expected), CorpusEscapes.Encode(written), fix.ToString());
    }

    /// <summary>
    /// Verifies that each cataloged input a fix established as one to write back, read by <c>Utf8DotEnvReader</c> in
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

        DotEnvReaderOptions options = RowOptions.Parse(fix.Options).ToReaderOptions();
        byte[] input = CorpusEscapes.Decode(fix.Input);

        DotEnvReaderTranscript.AssertEverySplitMatches(input, options, fix.ToString());
    }
}
