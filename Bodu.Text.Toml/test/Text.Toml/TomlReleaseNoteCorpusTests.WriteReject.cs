// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReleaseNoteCorpusTests.WriteReject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Toml;

public sealed partial class TomlReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing the value each catalogued tagged JSON describes, where a fix established that the value
    /// cannot be written, throws <see cref="TomlSerializationException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [Timeout(RowTimeout)]
    [DynamicData(
        nameof(WriteRejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void WriteReject_WhenFixScenarioIsWritten_ShouldThrowTomlSerializationException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        Assert.AreEqual(nameof(TomlSerializationException), fix.Expected, $"{fix}: a refused write throws TomlSerializationException.");

        byte[]? written = null;
        Assert.ThrowsExactly<TomlSerializationException>(
            () =>
            {
                written = WriteTaggedJson(input, options);
            },
            exception => exception is null ? $"{fix} wrote {CorpusEscapes.Encode(written)}." : $"{fix} threw {Describe(exception)}");
    }
}
