// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued input a fix established as malformed is rejected by <c>Utf8DelimitedReader</c>
    /// with <see cref="DelimitedFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
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
}
