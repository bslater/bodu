// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReleaseNoteCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that copying each catalogued input token by token from the reader to the writer writes the expected
    /// bytes, when the row gives them, and bytes that read back to the same transcript as the input.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RoundTripRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenFixScenarioIsCopied_ShouldReadBackTheSameTranscript(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        byte[] expected = fix.Expected.Length == 0 ? [] : CorpusEscapes.Decode(fix.Expected);

        string first = Render(input, options);
        byte[] written = CopyTokens(input, options);
        string second = Render(written, options);

        if (expected.Length > 0)
            CollectionAssert.AreEqual(expected, written, $"{fix}: wrote {CorpusEscapes.Encode(written)}.");

        Assert.AreEqual(first, second, $"{fix}: wrote {CorpusEscapes.Encode(written)}.");
    }
}
