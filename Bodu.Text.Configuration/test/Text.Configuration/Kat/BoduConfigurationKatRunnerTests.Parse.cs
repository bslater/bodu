// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKatRunnerTests.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;
using Bodu.Text.Formats;

namespace Bodu.Text.Configuration.Kat;

public partial class BoduConfigurationKatRunnerTests
{
    /// <summary>
    /// Drives every <see cref="BoduConfigurationKatKind.Parse" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(BoduConfigurationKnownAnswerData.ParserData),
        typeof(BoduConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Parse_Kat(BoduConfigurationKat kat)
    {
        BoduConfigurationParseOptions options = BuildParseOptions(kat);

        if (kat.Outcome is BoduConfigurationKatOutcome.Fail)
        {
            ExecuteParseFail(kat, options);
            return;
        }

        ExecuteParsePass(kat, options);
    }

    private static void ExecuteParsePass(BoduConfigurationKat kat, BoduConfigurationParseOptions options)
    {
        BoduConfigurationParseResult result = BoduConfigurationDocument.ParseWithDiagnostics(kat.Source!, options);
        IniDocument doc = result.Document;

        ExpectedDocument expected = kat.ExpectedDocument
            ?? throw new InvalidOperationException($"{kat.Id} is missing ExpectedDocument.");

        Assert.HasCount(expected.Preamble.Count, doc.GlobalSection.Entries, $"{kat.Id}: preamble entry count");
        for (var i = 0; i < expected.Preamble.Count; i++)
            AssertEntry(kat, expected.Preamble[i], doc.GlobalSection.Entries[i]);

        Assert.HasCount(expected.Sections.Count, doc.Sections, $"{kat.Id}: section count");
        for (var s = 0; s < expected.Sections.Count; s++)
        {
            ExpectedSection es = expected.Sections[s];
            IniSection actual = doc.Sections[s];

            Assert.AreEqual(es.Pattern, actual.Name, $"{kat.Id}: section[{s}].Name");
            Assert.HasCount(es.LeadingComments.Count, actual.LeadingComments, $"{kat.Id}: section[{s}].LeadingComments.Count");
            for (var c = 0; c < es.LeadingComments.Count; c++)
                Assert.AreEqual(es.LeadingComments[c], actual.LeadingComments[c].Text, $"{kat.Id}: section[{s}].LeadingComments[{c}]");

            Assert.HasCount(es.Properties.Count, actual.Entries, $"{kat.Id}: section[{s}].Entries.Count");
            for (var p = 0; p < es.Properties.Count; p++)
                AssertEntry(kat, es.Properties[p], actual.Entries[p]);
        }

        if (kat.ExpectedDiagnosticCount.HasValue)
            Assert.HasCount(kat.ExpectedDiagnosticCount.Value, result.Diagnostics, $"{kat.Id}: diagnostics count");
    }

    private static void ExecuteParseFail(BoduConfigurationKat kat, BoduConfigurationParseOptions options)
    {
        if (kat.ExpectedException is null)
            Assert.Fail($"{kat.Id} is a fail KAT but has no ExpectedException.");

        Exception ex = AssertThrowsExactlyByName(kat.ExpectedException!, () =>
        {
            _ = BoduConfigurationDocument.Parse(kat.Source!, options);
        });

        if (kat.ExpectedDiagnosticCode is null)
            return;

        if (ex is BoduConfigurationParseException parseException && parseException.Diagnostic is not null)
        {
            Assert.AreEqual(
                kat.ExpectedDiagnosticCode,
                parseException.Diagnostic.Code.ToString(),
                $"{kat.Id}: diagnostic code mismatch.");
        }
        else
        {
            Assert.Fail($"{kat.Id}: expected a BoduConfigurationParseException with diagnostic code '{kat.ExpectedDiagnosticCode}'.");
        }
    }

    private static void AssertEntry(BoduConfigurationKat kat, ExpectedProperty expected, IniEntry actual)
    {
        Assert.AreEqual(expected.Key, actual.Key, $"{kat.Id}: entry raw key");
        Assert.AreEqual(expected.Value, actual.Value, $"{kat.Id}: entry value");
        Assert.AreEqual(expected.ConfigurationKey, actual.ConfigurationKey(), $"{kat.Id}: entry configuration key");

        Assert.HasCount(expected.LeadingComments.Count, actual.LeadingComments, $"{kat.Id}: leading comment count");
        for (var i = 0; i < expected.LeadingComments.Count; i++)
            Assert.AreEqual(expected.LeadingComments[i], actual.LeadingComments[i].Text, $"{kat.Id}: leading comment[{i}]");

        if (expected.InlineComment is null)
        {
            Assert.IsNull(actual.InlineComment, $"{kat.Id}: inline comment should be absent.");
        }
        else
        {
            Assert.IsNotNull(actual.InlineComment, $"{kat.Id}: inline comment should be present.");
            Assert.AreEqual(expected.InlineComment, actual.InlineComment!.Value.Text, $"{kat.Id}: inline comment text.");
            if (expected.InlineCommentPrefix.HasValue)
                Assert.AreEqual(expected.InlineCommentPrefix.Value, actual.InlineComment.Value.Prefix, $"{kat.Id}: inline comment prefix.");
        }
    }

    /// <summary>
    /// Supplies the per-row display name used by the MSTest runner for KAT-driven tests.
    /// </summary>
    /// <param name="methodInfo">The driver method.</param>
    /// <param name="data">The row data; the single element is a <see cref="BoduConfigurationKat" />.</param>
    /// <returns>A short identifier derived from the KAT's stable ID and title.</returns>
    public static string GetKatDisplayName(System.Reflection.MethodInfo methodInfo, object[] data)
    {
        var kat = (BoduConfigurationKat)data[0];
        return $"{kat.Id} - {kat.Title}";
    }
}
