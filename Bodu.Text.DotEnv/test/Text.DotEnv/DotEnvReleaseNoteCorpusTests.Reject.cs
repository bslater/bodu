// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
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
    /// Verifies that each cataloged input a fix established as malformed is rejected by <c>Utf8DotEnvReader</c> with
    /// <see cref="DotEnvFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsRead_ShouldThrowDotEnvFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        Assert.AreEqual(nameof(DotEnvFormatException), fix.Expected, $"{fix}: a read is rejected with DotEnvFormatException.");

        string? rendering = null;
        _ = Assert.ThrowsExactly<DotEnvFormatException>(
            () =>
            {
                rendering = Render(ReadEntries(input, options));
            },
            exception => exception is null
                ? $"{fix}: read without an error, as {EncodeText(rendering ?? string.Empty)}."
                : $"{fix}: threw {exception.GetType().Name}: {exception.Message}");
    }

    /// <summary>
    /// Verifies that each cataloged input a fix established as malformed, read by <c>Utf8DotEnvReader</c> in blocks
    /// split every way and with a new reader after every token, raises exactly the error a single read raises, message,
    /// line, column and offset included, after the same tokens.
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

        DotEnvReaderOptions options = RowOptions.Parse(fix.Options).ToReaderOptions();
        byte[] input = CorpusEscapes.Decode(fix.Input);

        Assert.StartsWith(nameof(DotEnvFormatException) + "|", DotEnvReaderTranscript.ReadWhole(input, options)[^1], fix.ToString());

        DotEnvReaderTranscript.AssertEverySplitMatches(input, options, fix.ToString());
    }
}
