// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReleaseNoteCorpusTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Toml;

public sealed partial class TomlReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing the value each catalogued tagged JSON describes through <c>Utf8TomlWriter</c> writes
    /// exactly the expected text.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [Timeout(RowTimeout)]
    [DynamicData(
        nameof(WriteRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Write_WhenFixScenarioIsWritten_ShouldEmitTheExpectedText(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        byte[] written = WriteTaggedJson(input, options);

        CollectionAssert.AreEqual(expected, written, $"{fix} wrote {CorpusEscapes.Encode(written)}.");
    }
}
