// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReleaseNoteCorpusTests.WriteReject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Ini;

public sealed partial class IniReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing each catalogued set of triples a fix established as unwritable throws
    /// <see cref="ArgumentException" />, the exception the row names, rather than writing text that reads back as
    /// something else.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(WriteRejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void WriteReject_WhenFixScenarioIsWritten_ShouldThrowArgumentException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        List<(string Section, string Key, string Value)> triples = ReadWriteNotation(fix.Input);
        Assert.AreEqual(nameof(ArgumentException), fix.Expected, $"{fix}: a refused write throws ArgumentException.");
        byte[] written = [];

        Assert.ThrowsExactly<ArgumentException>(
            () =>
            {
                written = WriteTriples(triples, options);
            },
            thrown => thrown is null ? $"{fix}: no exception; wrote {CorpusEscapes.Encode(written)}." : fix.ToString());
    }
}
