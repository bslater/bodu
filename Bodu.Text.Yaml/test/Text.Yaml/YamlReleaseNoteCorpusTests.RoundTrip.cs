// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlReleaseNoteCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

public sealed partial class YamlReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that writing back the documents each catalogued input reads as, and reading the written text again,
    /// gives the same values, and that the written text is exactly the text the row expects when it gives one.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RoundTripRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenFixScenarioIsWrittenBack_ShouldReadBackTheSameValues(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        string first;
        byte[] written;
        IReadOnlyList<YamlDocument> documents = ReadDocuments(input, options);
        try
        {
            first = Render(documents);
            written = WriteDocuments(documents);
        }
        finally
        {
            DisposeAll(documents);
        }

        string second;
        try
        {
            second = ReadAndRender(written, options);
        }
        catch (YamlFormatException ex)
        {
            second = "nothing: " + Describe(ex);
        }

        string message = $"{fix}: read {first}, wrote {CorpusEscapes.Encode(written)}, read back {second}.";
        if (expected.Length > 0)
            Assert.AreEqual(CorpusEscapes.Encode(expected), CorpusEscapes.Encode(written), message);

        Assert.AreEqual(first, second, message);
    }
}
