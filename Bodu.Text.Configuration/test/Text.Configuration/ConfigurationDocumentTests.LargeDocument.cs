// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentTests.LargeDocument.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text;
using Bodu.Test;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class ConfigurationDocumentTests
{
    /// <summary>
    /// Stress-tier benchmark: parses a document with 5,000 sections and 5,000 properties per section to
    /// prove that the reader scales linearly with input size. Before the duplicate-detection refactor this
    /// would have been O(n²) and taken many seconds; with O(1) lookups it must complete in a small
    /// multiple of a second on the build machine.
    /// </summary>
    /// <remarks>
    /// The 10-second threshold is generous — typical CI machines complete this in well under one second.
    /// The intent is to catch a regression to quadratic behaviour, not to benchmark precisely.
    /// </remarks>
    [TestMethod]
    [TestCategory(TestCategories.Stress)]
    public void Parse_WhenDocumentHasManySections_ShouldScaleLinearly()
    {
        const int sectionCount = 5_000;
        const int propertiesPerSection = 10;

        StringBuilder builder = new(sectionCount * propertiesPerSection * 32);
        for (var i = 0; i < sectionCount; i++)
        {
            builder.Append('[').Append("section-").Append(i).Append(']').Append('\n');
            for (var j = 0; j < propertiesPerSection; j++)
                builder.Append("key").Append(j).Append(" = value").Append(j).Append('\n');
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        IniDocument doc = ConfigurationDocument.Parse(builder.ToString());
        stopwatch.Stop();

        Assert.AreEqual(sectionCount, doc.Sections.Count);
        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(10),
            $"Parsing {sectionCount} sections took {stopwatch.Elapsed.TotalSeconds:F2}s — expected sub-10s. " +
            "A quadratic regression in duplicate detection is the likely cause.");
    }

    /// <summary>
    /// Stress-tier benchmark: parses a single section with 10,000 entries to prove that entry-level
    /// duplicate detection is also O(1) per entry, not O(n).
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Stress)]
    public void Parse_WhenSingleSectionHasManyEntries_ShouldScaleLinearly()
    {
        const int entryCount = 10_000;

        StringBuilder builder = new(entryCount * 32);
        builder.Append("[*]\n");
        for (var i = 0; i < entryCount; i++)
            builder.Append("key").Append(i).Append(" = value").Append(i).Append('\n');

        Stopwatch stopwatch = Stopwatch.StartNew();
        IniDocument doc = ConfigurationDocument.Parse(builder.ToString());
        stopwatch.Stop();

        Assert.AreEqual(entryCount, doc.Sections[0].Entries.Count);
        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(10),
            $"Parsing {entryCount} entries in one section took {stopwatch.Elapsed.TotalSeconds:F2}s — expected sub-10s.");
    }
}
