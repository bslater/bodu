// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKatRunnerTests.Writer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using Bodu.Text.Configuration.Test.Infrastructure;

namespace Bodu.Text.Configuration.Kat;

public partial class BoduConfigurationKatRunnerTests
{
    /// <summary>
    /// Drives every <see cref="BoduConfigurationKatKind.Write" /> and
    /// <see cref="BoduConfigurationKatKind.RoundTrip" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [DataTestMethod]
    [DynamicData(nameof(BoduConfigurationKnownAnswerData.WriterData),
        typeof(BoduConfigurationKnownAnswerData),
        DynamicDataSourceType.Property,
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Writer_Kat(BoduConfigurationKat kat)
    {
        BoduConfigurationProfile writeProfile = MapProfile(kat.Profile);

        // Parse in Bodu mode so inline comments are captured. The KAT profile controls emission only.
        BoduConfigurationParseOptions parseOptions = BoduConfigurationParseOptions.Bodu;
        BoduConfigurationWriteOptions writeOptions = BuildWriteOptions(kat, writeProfile);

        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(kat.Source!, parseOptions);

        using StringWriter sw = new();
        doc.Save(sw, writeOptions);
        string written = sw.ToString().TrimEnd('\n', '\r');

        string expected = (kat.ExpectedText ?? string.Empty).TrimEnd('\n', '\r');

        if (kat.Kind is BoduConfigurationKatKind.RoundTrip)
        {
            BoduConfigurationDocument reparsed = BoduConfigurationDocument.Parse(written, BoduConfigurationParseOptions.Bodu);
            AssertDocumentsEquivalent(kat, doc, reparsed);
            return;
        }

        Assert.AreEqual(expected, written, $"{kat.Id}: writer output");
    }

    private static BoduConfigurationWriteOptions BuildWriteOptions(BoduConfigurationKat kat, BoduConfigurationProfile profile)
    {
        BoduConfigurationWriteOptions baseline = BoduConfigurationWriteOptions.For(profile);

        bool writeInline = kat.Options switch
        {
            "WriteInlineCommentsFalse" => false,
            "WriteInlineCommentsTrue" => true,
            _ => baseline.WriteInlineComments,
        };

        return new BoduConfigurationWriteOptions
        {
            Profile = profile,
            Encoding = baseline.Encoding,
            NewLine = baseline.NewLine,
            KeyValueSeparator = baseline.KeyValueSeparator,
            CommentPrefix = baseline.CommentPrefix,
            PreserveComments = baseline.PreserveComments,
            WriteInlineComments = writeInline,
            InsertBlankLineBetweenSections = baseline.InsertBlankLineBetweenSections,
        };
    }

    private static void AssertDocumentsEquivalent(BoduConfigurationKat kat, BoduConfigurationDocument expected, BoduConfigurationDocument actual)
    {
        Assert.AreEqual(expected.Sections.Count, actual.Sections.Count, $"{kat.Id}: section count");
        Assert.AreEqual(expected.Preamble.Properties.Count, actual.Preamble.Properties.Count, $"{kat.Id}: preamble count");

        for (int s = 0; s < expected.Sections.Count; s++)
        {
            Assert.AreEqual(expected.Sections[s].Pattern, actual.Sections[s].Pattern, $"{kat.Id}: section[{s}].Pattern");
            Assert.AreEqual(expected.Sections[s].Properties.Count, actual.Sections[s].Properties.Count, $"{kat.Id}: section[{s}].Properties.Count");

            for (int p = 0; p < expected.Sections[s].Properties.Count; p++)
            {
                Assert.AreEqual(expected.Sections[s].Properties[p].RawKey, actual.Sections[s].Properties[p].RawKey, $"{kat.Id}: section[{s}].Properties[{p}].RawKey");
                Assert.AreEqual(expected.Sections[s].Properties[p].Value, actual.Sections[s].Properties[p].Value, $"{kat.Id}: section[{s}].Properties[{p}].Value");
            }
        }
    }
}
