// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDifferentialTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Globalization;
using System.Text;

namespace Bodu.Text.Toml;

/// <summary>
/// Differential tests that validate the Bodu TOML parser against the independent <c>Tomlyn</c> reference parser. The
/// value-equality corpus uses TOML 1.0-common constructs (the subset both libraries accept); TOML 1.1.0-specific
/// features are covered by <see cref="TomlParseTests" />. The rejection corpus asserts both libraries reject the same
/// malformed documents.
/// </summary>
[TestClass]
public sealed class TomlDifferentialTests
{
    /// <summary>
    /// Verifies that the Bodu parser produces the same normalized model as Tomlyn for a corpus of valid documents.
    /// </summary>
    /// <param name="source">The TOML document.</param>
    [TestMethod]
    [DataRow("a = \"x\"\nb = 1\nc = 3.5\nd = true\ne = -2\nf = 1_000")]
    [DataRow("[t]\nx = 1\n[t.u]\ny = 2\n[t.u.v]\nz = 3")]
    [DataRow("a.b.c = 1\na.b.d = 2\nname = \"orange\"")]
    [DataRow("arr = [1, 2, [3, 4], \"x\", 'y']")]
    [DataRow("inline = { a = 1, b = { c = 2 }, d = [1, 2] }")]
    [DataRow("[[p]]\nn = 1\n[[p]]\nn = 2\n[[p]]\nn = 3")]
    [DataRow("nums = [0.1, 0.2, 1, 2]\nhex = 0xDEADBEEF\noct = 0o755\nbin = 0b1101")]
    [DataRow("[[fruits]]\nname = \"apple\"\n[fruits.physical]\ncolor = \"red\"\n[[fruits.varieties]]\nname = \"red delicious\"\n[[fruits]]\nname = \"banana\"")]
    [DataRow("\"127.0.0.1\" = 1\n\"character encoding\" = 2\n'key2' = 3")]
    [DataRow("str = \"\"\"\nRoses are red\nViolets are blue\"\"\"\nlit = 'C:\\path'")]
    [DataRow("flt = 6.626e-34\nbig = 9_223_372_036_854_775_807\nneg = -17")]
    public void Parse_WhenValidDocument_ShouldMatchTomlyn(string source)
    {
        var bodu = NormalizeBodu(Toml.Parse(source));
        var tomlyn = NormalizeTomlyn(global::Tomlyn.Toml.ToModel(source));

        Assert.AreEqual(tomlyn, bodu);
    }

    /// <summary>
    /// Verifies that both Bodu and Tomlyn reject the same malformed documents.
    /// </summary>
    /// <param name="source">The malformed TOML document.</param>
    [TestMethod]
    [DataRow("a = 1\na = 2")]
    [DataRow("[a]\n[a]")]
    [DataRow("[a]\nb = 1\n[a.b]\nc = 2")]
    [DataRow("fruit.apple = 1\nfruit.apple.smooth = true")]
    [DataRow("fruits = []\n[[fruits]]")]
    [DataRow("[[fruits]]\n[fruits]")]
    [DataRow("a = { b = 1 }\na.c = 2")]
    [DataRow("key = ")]
    [DataRow("= 1")]
    [DataRow("a = 1 b = 2")]
    [DataRow("a = [1, 2")]
    [DataRow("x = .7")]
    [DataRow("x = 7.")]
    [DataRow("x = 3.e+20")]
    [DataRow("x = 01")]
    [DataRow("x = 1__0")]
    [DataRow("x = 0xGG")]
    [DataRow("s = \"unterminated")]
    [DataRow("[unterminated")]
    public void Parse_WhenInvalidDocument_ShouldBeRejectedByBothParsers(string source)
    {
        var boduRejects = !Toml.TryParse(source, out _);
        var tomlynRejects = !global::Tomlyn.Toml.TryToModel<global::Tomlyn.Model.TomlTable>(source, out _, out var diagnostics) || diagnostics.HasErrors;

        Assert.IsTrue(boduRejects, "Bodu accepted a document Tomlyn rejects.");
        Assert.IsTrue(tomlynRejects, "Tomlyn accepted this document; the case may not be invalid.");
    }

    /// <summary>
    /// Reduces a Bodu document to a canonical, order-independent string for comparison.
    /// </summary>
    /// <param name="value">The Bodu value.</param>
    /// <returns>The canonical representation.</returns>
    private static string NormalizeBodu(TomlValue value)
    {
        switch (value)
        {
            case TomlTable table:
                var entries = new List<string>();
                foreach (var pair in table)
                    entries.Add(Quote(pair.Key) + "=" + NormalizeBodu(pair.Value));
                entries.Sort(StringComparer.Ordinal);
                return "{" + string.Join(",", entries) + "}";
            case TomlArray array:
                var items = new List<string>();
                foreach (var item in array)
                    items.Add(NormalizeBodu(item));
                return "[" + string.Join(",", items) + "]";
            case TomlString s:
                return "S:" + s.Value;
            case TomlInteger i:
                return "I:" + i.Value.ToString(CultureInfo.InvariantCulture);
            case TomlFloat f:
                return "F:" + CanonicalDouble(f.Value);
            case TomlBoolean b:
                return "B:" + (b.Value ? "true" : "false");
            default:
                return "?:" + value;
        }
    }

    /// <summary>
    /// Reduces a Tomlyn model object to the same canonical string form as <see cref="NormalizeBodu" />.
    /// </summary>
    /// <param name="value">The Tomlyn model object.</param>
    /// <returns>The canonical representation.</returns>
    private static string NormalizeTomlyn(object? value)
    {
        switch (value)
        {
            case IDictionary<string, object?> table:
                var entries = new List<string>();
                foreach (var pair in table)
                    entries.Add(Quote(pair.Key) + "=" + NormalizeTomlyn(pair.Value));
                entries.Sort(StringComparer.Ordinal);
                return "{" + string.Join(",", entries) + "}";
            case string s:
                return "S:" + s;
            case bool b:
                return "B:" + (b ? "true" : "false");
            case long i:
                return "I:" + i.ToString(CultureInfo.InvariantCulture);
            case double f:
                return "F:" + CanonicalDouble(f);
            case IEnumerable enumerable:
                var items = new List<string>();
                foreach (var item in enumerable)
                    items.Add(NormalizeTomlyn(item));
                return "[" + string.Join(",", items) + "]";
            default:
                return "?:" + value;
        }
    }

    /// <summary>
    /// Renders a double in a canonical form shared by both parsers.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The canonical string.</returns>
    private static string CanonicalDouble(double value) =>
        double.IsNaN(value) ? "nan"
        : double.IsPositiveInfinity(value) ? "inf"
        : double.IsNegativeInfinity(value) ? "-inf"
        : value.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>
    /// Wraps a key in delimiters so distinct keys cannot collide after concatenation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The quoted key.</returns>
    private static string Quote(string key) =>
        "<" + key + ">";
}
