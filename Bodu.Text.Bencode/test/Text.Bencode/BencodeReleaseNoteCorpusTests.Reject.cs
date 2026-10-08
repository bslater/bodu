// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued input a fix established as malformed is rejected by the selected surface with
    /// <see cref="BencodeFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsRead_ShouldThrowBencodeFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        Assert.AreEqual(nameof(BencodeFormatException), fix.Expected, $"{fix}: a read is rejected with BencodeFormatException.");

        Assert.ThrowsExactly<BencodeFormatException>(
            () =>
            {
                _ = Render(input, options);
            },
            fix.ToString());
    }
}
