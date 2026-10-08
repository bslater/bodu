// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlReleaseNoteCorpusTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

using Bodu.Test.Corpus;
using Bodu.Test.Kat;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

public sealed partial class YamlReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued input a fix established as readable reads as the documents the row expects, one
    /// JSON value per document, with integers told apart from floats.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ParseRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenFixScenarioIsRead_ShouldMatchTheExpectedDocuments(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        List<JsonElement> expected = ReadExpectedDocuments(fix.Expected);

        IReadOnlyList<YamlDocument> documents = ReadDocuments(input, options);
        try
        {
            string message = $"{fix}: expected {fix.Expected}, read {Render(documents)}.";

            Assert.AreEqual(expected.Count, documents.Count, message);
            for (int i = 0; i < expected.Count; i++)
                Assert.IsTrue(CorpusCompare.Matches(expected[i], documents[i].RootElement, distinguishIntegers: true), message);
        }
        finally
        {
            DisposeAll(documents);
        }
    }
}
