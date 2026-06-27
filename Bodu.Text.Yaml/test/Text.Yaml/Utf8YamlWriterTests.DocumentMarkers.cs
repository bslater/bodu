// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.DocumentMarkers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlWriter" /> quotes a string scalar that is, or begins with, a YAML document marker
/// (<c>---</c> / <c>...</c>) so it round-trips instead of being reparsed as a marker. A string equal to <c>...</c>, or
/// one beginning with <c>...</c> followed by a space, emitted unquoted at the start of a line would be misread as a
/// marker on reparse and silently lose its value.
/// </summary>
public partial class Utf8YamlWriterTests
{
    /// <summary>
    /// Verifies that a root string scalar resembling a document marker is emitted quoted, so parsing the output yields
    /// the original string rather than a document marker.
    /// </summary>
    /// <param name="value">The string scalar to write at the document root.</param>
    [TestMethod]
    [DataRow("...")]
    [DataRow("---")]
    [DataRow("... n")]
    [DataRow("--- n")]
    public void Write_WhenRootStringResemblesDocumentMarker_ShouldQuoteSoItRoundTrips(string value)
    {
        var yaml = Write((ref Utf8YamlWriter w) => w.WriteString(value));

        using var doc = YamlDocument.Parse(yaml);
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.ValueKind, yaml);
        Assert.AreEqual(value, doc.RootElement.GetString(), yaml);
    }
}
