// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
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
    /// Verifies that each cataloged input a fix established as malformed is rejected by <c>Utf8DelimitedReader</c>
    /// with <see cref="DelimitedFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsRead_ShouldThrowDelimitedFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        Assert.AreEqual(nameof(DelimitedFormatException), fix.Expected, $"{fix}: a read is rejected with DelimitedFormatException.");

        string rendering = string.Empty;
        _ = Assert.ThrowsExactly<DelimitedFormatException>(
            () =>
            {
                rendering = Render(ReadDocument(input, options.ToReaderOptions()), options.View);
            },
            thrown => thrown is null ? $"{fix}: read {Escape(rendering)}." : $"{fix}: threw {Describe(thrown)}.");
    }

    /// <summary>
    /// Verifies that each cataloged input a fix established as malformed, read by <c>Utf8DelimitedReader</c> in blocks
    /// split every way and with a new reader after every token, raises exactly the error a single read raises, message,
    /// line and offset included, after the same tokens.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsReadInBlocks_ShouldThrowAsTheSinglePassDoes(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        DelimitedReaderOptions options = RowOptions.Parse(fix.Options).ToReaderOptions();
        byte[] input = CorpusEscapes.Decode(fix.Input);

        Assert.StartsWith(nameof(DelimitedFormatException) + "|", DelimitedReaderTranscript.ReadWhole(input, options)[^1], fix.ToString());

        DelimitedReaderTranscript.AssertEverySplitMatches(input, options, fix.ToString());
    }
}
