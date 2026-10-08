// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReleaseNoteCorpusTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that replaying each catalogued write transcript on <c>Utf8BencodeWriter</c> closes every container and
    /// writes exactly the expected bytes.
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
        List<WriteOperation> operations = ReadWriteNotation(fix.Input);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        (byte[] written, int depth) = Write(operations, options);

        Assert.AreEqual(0, depth, $"{fix}: the transcript left a container open.");
        CollectionAssert.AreEqual(expected, written, $"{fix}: wrote {CorpusEscapes.Encode(written)}.");
    }
}
