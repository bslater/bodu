// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReleaseNoteCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.DotEnv;

public sealed partial class DotEnvReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing back the entries read from each catalogued input emits bytes that read back to the same
    /// entries, and the expected bytes when the row gives them.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
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
}
