// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing each catalogued set of records with <c>Utf8DelimitedWriter</c> emits exactly the expected
    /// bytes.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(WriteRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Write_WhenFixScenarioIsWritten_ShouldEmitTheExpectedBytes(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        List<FramedRecord> records = ReadWriteNotation(CorpusEscapes.Decode(fix.Input), options.NoHeader);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        byte[] written = WriteRecords(records, options.ToWriterOptions());

        Assert.AreEqual(Escape(expected), Escape(written), fix.ToString());
    }
}
