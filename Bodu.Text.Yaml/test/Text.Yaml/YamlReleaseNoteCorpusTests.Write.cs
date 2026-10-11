// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlReleaseNoteCorpusTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Yaml;

public sealed partial class YamlReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing the JSON value each catalogued write scenario holds through <c>Utf8YamlWriter</c> emits
    /// exactly the YAML text the row expects.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(WriteRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Write_WhenFixScenarioIsWritten_ShouldEmitTheExpectedText(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        byte[] input = CorpusEscapes.Decode(fix.Input);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        byte[] written = WriteJsonValue(input);

        Assert.AreEqual(CorpusEscapes.Encode(expected), CorpusEscapes.Encode(written), fix.ToString());
    }
}
