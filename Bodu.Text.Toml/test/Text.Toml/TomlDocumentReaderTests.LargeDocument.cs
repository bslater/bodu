// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.LargeDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text;
using Bodu.Text.Toml.Document;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that parsing a table with a very large number of keys scales roughly linearly, guarding against a
/// regression to the O(n²) per-key sibling scan that previously made a large flat table a denial-of-service vector.
/// </summary>
public partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that a single table with tens of thousands of keys parses well within a generous time bound. The
    /// previous linear key lookup per inserted key made this quadratic; the hashed child index keeps it linear.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Parse_WhenTableHasManyKeys_ShouldScaleLinearly()
    {
        const int keyCount = 100_000;
        var sb = new StringBuilder(keyCount * 12);
        for (int i = 0; i < keyCount; i++)
            sb.Append('k').Append(i).Append(" = ").Append(i).Append('\n');

        byte[] utf8 = Encoding.UTF8.GetBytes(sb.ToString());

        var stopwatch = Stopwatch.StartNew();
        using TomlDocument document = TomlDocument.Parse(utf8);
        stopwatch.Stop();

        int count = 0;
        foreach (var _ in document.RootElement.EnumerateObject())
            count++;

        Assert.AreEqual(keyCount, count);
        Assert.IsLessThan(TimeSpan.FromSeconds(10), stopwatch.Elapsed,
            "A quadratic regression in per-key lookup is the likely cause of exceeding the time bound.");
    }
}
