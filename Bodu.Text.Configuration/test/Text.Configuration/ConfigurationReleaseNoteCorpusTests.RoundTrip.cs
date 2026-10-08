// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReleaseNoteCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Configuration;

public sealed partial class ConfigurationReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that saving each catalogued document with its profile's write options writes the expected text, when
    /// the row gives it, and text that parses and resolves to the same view as the input.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RoundTripRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenFixScenarioIsSavedAndReadAgain_ShouldResolveTheSameView(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        var options = RowOptions.Parse(fix.Options);
        string text = CorpusEscapes.DecodeText(fix.Input);
        string expected = fix.Expected.Length == 0 ? string.Empty : CorpusEscapes.DecodeText(fix.Expected);

        var document = ConfigurationDocument.Parse(text, ConfigurationParseOptions.For(options.Profile));
        string first = Render(document.Resolve(options.Target, ConfigurationResolveOptions.For(options.Profile)));
        string written = Write(document, options);
        string second = RenderOrDescribe(written, options);
        string writtenEscaped = CorpusEscapes.Encode(Encoding.UTF8.GetBytes(written));

        if (expected.Length > 0)
            Assert.AreEqual(expected, written, $"{fix}: wrote {writtenEscaped}.");

        Assert.AreEqual(first, second, $"{fix}: wrote {writtenEscaped}.");
    }
}
