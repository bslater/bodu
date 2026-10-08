// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReleaseNoteCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Toml;

public sealed partial class TomlReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing each catalogued input back through <c>TomlDocument</c> and <c>Utf8TomlWriter</c> writes the
    /// expected text, when the row gives it, and text that reads back to the same values as the input.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [Timeout(RowTimeout)]
    [DynamicData(
        nameof(RoundTripRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenFixScenarioIsRewritten_ShouldReadBackTheSameValues(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        string first = Render(ReadModel(input, options));
        byte[] written = Rewrite(input, options);
        string second = Render(ReadModel(written, options));

        if (expected.Length > 0)
            CollectionAssert.AreEqual(expected, written, $"{fix} wrote {CorpusEscapes.Encode(written)}.");

        Assert.AreEqual(first, second, $"{fix} wrote {CorpusEscapes.Encode(written)}.");
    }
}
