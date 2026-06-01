// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.KnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public sealed partial class IniTests
{
    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char}, IniParseOptions)" /> produces the expected global
    /// entries and named sections for every Wikipedia INI format Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="IniKnownAnswerVectors.SpecVectors" />.</param>
    [TestMethod]
    [DynamicData(nameof(IniKnownAnswerVectors.SpecVectors), typeof(IniKnownAnswerVectors))]
    public void Parse_ForSpecKnownAnswerVector_ShouldProduceExpectedDocument(IniKnownAnswerVector vector)
    {
        IniDocument doc = Ini.Parse(vector.Input, vector.Options ?? IniParseOptions.Default);

        Assert.AreEqual(vector.GlobalEntries.Length, doc.GlobalSection.Entries.Count,
            $"Global entry count mismatch for vector: {vector.Description}");

        for (var i = 0; i < vector.GlobalEntries.Length; i++)
        {
            Assert.AreEqual(vector.GlobalEntries[i].Key, doc.GlobalSection.Entries[i].Key,
                $"Global entry [{i}] key mismatch for vector: {vector.Description}");
            Assert.AreEqual(vector.GlobalEntries[i].Value, doc.GlobalSection.Entries[i].Value,
                $"Global entry [{i}] value mismatch for vector: {vector.Description}");
        }

        Assert.AreEqual(vector.Sections.Length, doc.Sections.Count,
            $"Section count mismatch for vector: {vector.Description}");

        for (var s = 0; s < vector.Sections.Length; s++)
        {
            (var sectionName, (string Key, string Value)[] sectionEntries) = vector.Sections[s];
            IniSection actualSection = doc.Sections[s];

            Assert.AreEqual(sectionName, actualSection.Name,
                $"Section [{s}] name mismatch for vector: {vector.Description}");

            Assert.AreEqual(sectionEntries.Length, actualSection.Entries.Count,
                $"Section [{s}] entry count mismatch for vector: {vector.Description}");

            for (var i = 0; i < sectionEntries.Length; i++)
            {
                Assert.AreEqual(sectionEntries[i].Key, actualSection.Entries[i].Key,
                    $"Section [{s}] entry [{i}] key mismatch for vector: {vector.Description}");
                Assert.AreEqual(sectionEntries[i].Value, actualSection.Entries[i].Value,
                    $"Section [{s}] entry [{i}] value mismatch for vector: {vector.Description}");
            }
        }
    }

    /// <summary>
    /// Verifies that a round-trip through <see cref="Ini.Format(IniDocument)" /> and
    /// <see cref="Ini.Parse(ReadOnlySpan{char}, IniParseOptions)" /> preserves the global entries and named
    /// sections for every Wikipedia INI format Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="IniKnownAnswerVectors.SpecVectors" />.</param>
    [TestMethod]
    [DynamicData(nameof(IniKnownAnswerVectors.SpecVectors), typeof(IniKnownAnswerVectors))]
    public void Format_ForSpecKnownAnswerVector_ShouldRoundTripDocument(IniKnownAnswerVector vector)
    {
        IniDocument original = Ini.Parse(vector.Input, vector.Options ?? IniParseOptions.Default);
        var formatted = Ini.Format(original);
        IniDocument roundTripped = Ini.Parse(formatted, vector.Options ?? IniParseOptions.Default);

        Assert.AreEqual(vector.GlobalEntries.Length, roundTripped.GlobalSection.Entries.Count,
            $"Round-trip global entry count mismatch for vector: {vector.Description}");

        for (var i = 0; i < vector.GlobalEntries.Length; i++)
        {
            Assert.AreEqual(vector.GlobalEntries[i].Key, roundTripped.GlobalSection.Entries[i].Key,
                $"Round-trip global entry [{i}] key mismatch for vector: {vector.Description}");
            Assert.AreEqual(vector.GlobalEntries[i].Value, roundTripped.GlobalSection.Entries[i].Value,
                $"Round-trip global entry [{i}] value mismatch for vector: {vector.Description}");
        }

        Assert.AreEqual(vector.Sections.Length, roundTripped.Sections.Count,
            $"Round-trip section count mismatch for vector: {vector.Description}");

        for (var s = 0; s < vector.Sections.Length; s++)
        {
            (var sectionName, (string Key, string Value)[] sectionEntries) = vector.Sections[s];
            IniSection actualSection = roundTripped.Sections[s];

            Assert.AreEqual(sectionName, actualSection.Name,
                $"Round-trip section [{s}] name mismatch for vector: {vector.Description}");

            Assert.AreEqual(sectionEntries.Length, actualSection.Entries.Count,
                $"Round-trip section [{s}] entry count mismatch for vector: {vector.Description}");

            for (var i = 0; i < sectionEntries.Length; i++)
            {
                Assert.AreEqual(sectionEntries[i].Key, actualSection.Entries[i].Key,
                    $"Round-trip section [{s}] entry [{i}] key mismatch for vector: {vector.Description}");
                Assert.AreEqual(sectionEntries[i].Value, actualSection.Entries[i].Value,
                    $"Round-trip section [{s}] entry [{i}] value mismatch for vector: {vector.Description}");
            }
        }
    }
}
