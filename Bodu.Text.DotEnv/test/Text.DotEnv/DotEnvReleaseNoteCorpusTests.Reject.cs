// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.DotEnv;

public sealed partial class DotEnvReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued input a fix established as malformed is rejected by <c>Utf8DotEnvReader</c> with
    /// <see cref="DotEnvFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsRead_ShouldThrowDotEnvFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        Assert.AreEqual(nameof(DotEnvFormatException), fix.Expected, $"{fix}: a read is rejected with DotEnvFormatException.");

        string? rendering = null;
        _ = Assert.ThrowsExactly<DotEnvFormatException>(
            () =>
            {
                rendering = Render(ReadEntries(input, options));
            },
            exception => exception is null
                ? $"{fix}: read without an error, as {EncodeText(rendering ?? string.Empty)}."
                : $"{fix}: threw {exception.GetType().Name}: {exception.Message}");
    }
}
