// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
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
    /// Verifies that each cataloged input a fix established as malformed is rejected by the view the row selects with
    /// <see cref="IniFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsRead_ShouldThrowIniFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        Assert.AreEqual(nameof(IniFormatException), fix.Expected, $"{fix}: a read is rejected with IniFormatException.");
        string rendering = string.Empty;

        Assert.ThrowsExactly<IniFormatException>(
            () =>
            {
                rendering = Render(input, options);
            },
            thrown => thrown is null ? $"{fix}: no exception; rendered {rendering}." : fix.ToString());
    }

    /// <summary>
    /// Verifies that each cataloged input a fix established as malformed, read by <c>Utf8IniReader</c> in blocks split
    /// every way and with a new reader after every token, reports exactly what a single read reports: the tokens before
    /// the reader's error and the error, message, line and offset included, or, for a row that a duplicate policy of
    /// the document view rejects, every token.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsReadInBlocks_ShouldMatchTheSinglePassTranscript(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        IniReaderOptions options = RowOptions.Parse(fix.Options).ToReaderOptions();
        byte[] input = CorpusEscapes.Decode(fix.Input);

        IniReaderTranscript.AssertEverySplitMatches(input, options, fix.ToString());
    }
}
