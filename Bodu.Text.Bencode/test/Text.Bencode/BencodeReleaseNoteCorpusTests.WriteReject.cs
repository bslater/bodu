// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReleaseNoteCorpusTests.WriteReject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that replaying each catalogued write transcript a fix established as unwritable throws
    /// <see cref="BencodeFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(WriteRejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void WriteReject_WhenFixScenarioIsWritten_ShouldThrowBencodeFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        List<WriteOperation> operations = ReadWriteNotation(fix.Input);
        Assert.AreEqual(nameof(BencodeFormatException), fix.Expected, $"{fix}: a refused write throws BencodeFormatException.");

        Assert.ThrowsExactly<BencodeFormatException>(
            () =>
            {
                _ = Write(operations, options);
            },
            fix.ToString());
    }
}
