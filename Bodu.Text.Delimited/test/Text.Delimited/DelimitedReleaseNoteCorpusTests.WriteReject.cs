// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.WriteReject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing each catalogued set of records a fix established as unwritable throws
    /// <see cref="ArgumentNullException" />, the exception the row names, which <c>Utf8DelimitedWriter</c> documents for
    /// a <see langword="null" /> name or value.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(WriteRejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void WriteReject_WhenFixScenarioIsWritten_ShouldThrowArgumentNullException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        List<FramedRecord> records = ReadWriteNotation(CorpusEscapes.Decode(fix.Input), options.NoHeader);
        Assert.AreEqual(nameof(ArgumentNullException), fix.Expected, $"{fix}: a refused write throws ArgumentNullException.");

        byte[] written = [];
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () =>
            {
                written = WriteRecords(records, options.ToWriterOptions());
            },
            thrown => thrown is null ? $"{fix}: wrote {Escape(written)}." : $"{fix}: threw {Describe(thrown)}.");
    }
}
