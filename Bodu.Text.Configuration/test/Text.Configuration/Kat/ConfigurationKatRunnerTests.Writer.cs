// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKatRunnerTests.Writer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration.Kat;

public partial class ConfigurationKatRunnerTests
{
    /// <summary>
    /// Drives every <see cref="ConfigurationKatKind.Write" /> and
    /// <see cref="ConfigurationKatKind.RoundTrip" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.WriterData), typeof(ConfigurationKnownAnswerData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Writer_Kat(ConfigurationKat kat)
    {
        ConfigurationProfile writeProfile = MapProfile(kat.Profile);

        // Parse in Bodu mode so inline comments are captured. The KAT profile controls emission only.
        ConfigurationParseOptions parseOptions = ConfigurationParseOptions.Bodu;
        ConfigurationWriteOptions writeOptions = BuildWriteOptions(kat, writeProfile);

        ConfigurationDocument doc = ConfigurationDocument.Parse(kat.Source!, parseOptions);

        using StringWriter sw = new();
        ConfigurationDocument.Save(doc, sw, writeOptions);
        var written = sw.ToString().TrimEnd('\n', '\r');

        var expected = (kat.ExpectedText ?? string.Empty).TrimEnd('\n', '\r');

        // On Windows, the KAT data file is checked out with CRLF (`* text=auto` in .gitattributes)
        // and C# raw-string literals adopt those endings; the writer emits its configured NewLine
        // ("\n" by default), so the assertion would otherwise fail on internal CRLF/LF mismatches.
        if (OperatingSystem.IsWindows())
        {
            expected = expected.Replace("\r\n", "\n");
        }

        if (kat.Kind is ConfigurationKatKind.RoundTrip)
        {
            ConfigurationDocument reparsed = ConfigurationDocument.Parse(written, ConfigurationParseOptions.Bodu);
            AssertDocumentsEquivalent(kat, doc, reparsed);
            return;
        }

        Assert.AreEqual(expected, written, $"{kat.Id}: writer output");
    }

    private static ConfigurationWriteOptions BuildWriteOptions(ConfigurationKat kat, ConfigurationProfile profile)
    {
        var baseline = ConfigurationWriteOptions.For(profile);

        var writeInline = kat.Options switch
        {
            "WriteInlineCommentsFalse" => false,
            "WriteInlineCommentsTrue" => true,
            _ => baseline.WriteInlineComments,
        };

        return new ConfigurationWriteOptions
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

    private static void AssertDocumentsEquivalent(ConfigurationKat kat, IniDocumentBase expected, IniDocumentBase actual)
    {
        Assert.HasCount(expected.Sections.Count, actual.Sections, $"{kat.Id}: section count");
        Assert.HasCount(expected.GlobalSection.Entries.Count, actual.GlobalSection.Entries, $"{kat.Id}: preamble count");

        for (var s = 0; s < expected.Sections.Count; s++)
        {
            Assert.AreEqual(expected.Sections[s].Name, actual.Sections[s].Name, $"{kat.Id}: section[{s}].Name");
            Assert.HasCount(expected.Sections[s].Entries.Count, actual.Sections[s].Entries, $"{kat.Id}: section[{s}].Entries.Count");

            for (var p = 0; p < expected.Sections[s].Entries.Count; p++)
            {
                Assert.AreEqual(expected.Sections[s].Entries[p].Key, actual.Sections[s].Entries[p].Key, $"{kat.Id}: section[{s}].Entries[{p}].Key");
                Assert.AreEqual(expected.Sections[s].Entries[p].Value, actual.Sections[s].Entries[p].Value, $"{kat.Id}: section[{s}].Entries[{p}].Value");
            }
        }
    }
}
