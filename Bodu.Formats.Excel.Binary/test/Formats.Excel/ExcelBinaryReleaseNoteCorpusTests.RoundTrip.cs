// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing each cataloged record stream back through <c>BiffWriter</c> writes a stream whose record
    /// listing equals the input's, and the expected bytes when the row gives them.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [Timeout(RowTimeout)]
    [DynamicData(
        nameof(RoundTripRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenFixScenarioIsRewritten_ShouldListTheSameRecords(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        GeneratedInput input = InputGenerator.Generate(fix.Input, options.LayoutVersion);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        (string first, byte[] written) = Rewrite(input.Stream, options);
        string second = RenderRecords(written, options, includeLengths: false);

        if (expected.Length > 0)
            CollectionAssert.AreEqual(expected, written, $"{fix} wrote {CorpusEscapes.Encode(written)}.");

        Assert.AreEqual(first, second, $"{fix} wrote {CorpusEscapes.Encode(written)}.");
    }
}
