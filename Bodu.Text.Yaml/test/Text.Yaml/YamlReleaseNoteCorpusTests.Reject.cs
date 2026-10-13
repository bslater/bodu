// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Yaml;

public sealed partial class YamlReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each cataloged input a fix established as malformed is rejected with
    /// <see cref="YamlFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsRead_ShouldThrowYamlFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        Assert.AreEqual(nameof(YamlFormatException), fix.Expected, $"{fix}: a read is rejected with YamlFormatException.");

        string accepted = string.Empty;
        Assert.ThrowsExactly<YamlFormatException>(
            () =>
            {
                accepted = ReadAndRender(input, options);
            },
            exception => exception is null ? $"{fix}: accepted, reading {accepted}." : $"{fix}: threw {Describe(exception)}.");
    }
}
