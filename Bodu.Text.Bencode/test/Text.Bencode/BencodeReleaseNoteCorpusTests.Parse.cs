// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReleaseNoteCorpusTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued input a fix established as readable is read by the selected surface into the
    /// expected token transcript.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ParseRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenFixScenarioIsRead_ShouldRenderTheExpectedTranscript(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);

        string actual = Render(input, options);

        Assert.AreEqual(NormalizeTranscript(fix.Expected), NormalizeTranscript(actual), fix.ToString());
    }
}
