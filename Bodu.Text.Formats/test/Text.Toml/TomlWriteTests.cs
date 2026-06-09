// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlWriteTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Text.Toml;

/// <summary>
/// Behavioural tests for <see cref="Toml.Format(TomlTable)" /> — value formatting, table and array-of-tables section
/// layout, key quoting, and parse/format round-trips.
/// </summary>
[TestClass]
public sealed class TomlWriteTests
{
    [TestMethod]
    public void Format_WhenScalars_ShouldWriteInlineKeyValues()
    {
        var table = new TomlTable
        {
            { "name", new TomlString("Tom") },
            { "age", new TomlInteger(42) },
            { "ratio", new TomlFloat(3.5) },
            { "active", new TomlBoolean(true) },
        };

        var text = Toml.Format(table);

        Assert.AreEqual("name = \"Tom\"\nage = 42\nratio = 3.5\nactive = true\n", text);
    }

    [TestMethod]
    public void Format_WhenFloatIsWhole_ShouldEmitFractionalPoint()
    {
        var table = new TomlTable { { "x", new TomlFloat(5.0) } };

        Assert.AreEqual("x = 5.0\n", Toml.Format(table));
    }

    [TestMethod]
    public void Format_WhenSpecialFloats_ShouldEmitKeywords()
    {
        var table = new TomlTable
        {
            { "i", new TomlFloat(double.PositiveInfinity) },
            { "ni", new TomlFloat(double.NegativeInfinity) },
            { "n", new TomlFloat(double.NaN) },
        };

        Assert.AreEqual("i = inf\nni = -inf\nn = nan\n", Toml.Format(table));
    }

    [TestMethod]
    public void Format_WhenStringHasReservedCharacters_ShouldEscape()
    {
        var table = new TomlTable { { "s", new TomlString("a\tb\nc\"d\\e") } };

        Assert.AreEqual("s = \"a\\tb\\nc\\\"d\\\\e\\u0001\"\n", Toml.Format(table));
    }

    [TestMethod]
    public void Format_WhenKeyNeedsQuoting_ShouldQuote()
    {
        var table = new TomlTable { { "a.b", new TomlInteger(1) }, { "plain", new TomlInteger(2) } };

        Assert.AreEqual("\"a.b\" = 1\nplain = 2\n", Toml.Format(table));
    }

    [TestMethod]
    public void Format_WhenSubTable_ShouldWriteSection()
    {
        var inner = new TomlTable { { "host", new TomlString("localhost") } };
        var root = new TomlTable { { "title", new TomlString("x") }, { "server", inner } };

        Assert.AreEqual("title = \"x\"\n\n[server]\nhost = \"localhost\"\n", Toml.Format(root));
    }

    [TestMethod]
    public void Format_WhenNestedSubTables_ShouldWriteDottedHeaders()
    {
        var z = new TomlTable { { "a", new TomlInteger(1) } };
        var y = new TomlTable { { "z", z } };
        var root = new TomlTable { { "x", new TomlTable { { "y", y } } } };

        Assert.AreEqual("[x]\n\n[x.y]\n\n[x.y.z]\na = 1\n", Toml.Format(root));
    }

    [TestMethod]
    public void Format_WhenArrayOfTables_ShouldWriteDoubleBracketSections()
    {
        var array = new TomlArray
        {
            new TomlTable { { "name", new TomlString("Hammer") } },
            new TomlTable { { "name", new TomlString("Nail") } },
        };
        var root = new TomlTable { { "product", array } };

        Assert.AreEqual("[[product]]\nname = \"Hammer\"\n\n[[product]]\nname = \"Nail\"\n", Toml.Format(root));
    }

    [TestMethod]
    public void Format_WhenInlineArray_ShouldWriteBracketList()
    {
        var root = new TomlTable
        {
            { "nums", new TomlArray { new TomlInteger(1), new TomlInteger(2), new TomlInteger(3) } },
            { "empty", new TomlArray() },
        };

        Assert.AreEqual("nums = [1, 2, 3]\nempty = []\n", Toml.Format(root));
    }

    [TestMethod]
    public void Format_WhenDateTimes_ShouldWriteRfc3339()
    {
        var root = new TomlTable
        {
            { "odt", new TomlOffsetDateTime(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero)) },
            { "off", new TomlOffsetDateTime(new DateTimeOffset(1979, 5, 27, 0, 32, 0, TimeSpan.FromHours(-7))) },
            { "ld", new TomlLocalDate(new DateOnly(1979, 5, 27)) },
            { "lt", new TomlLocalTime(new TimeOnly(7, 32, 0)) },
        };

        var text = Toml.Format(root);

        StringAssert.Contains(text, "odt = 1979-05-27T07:32:00Z");
        StringAssert.Contains(text, "off = 1979-05-27T00:32:00-07:00");
        StringAssert.Contains(text, "ld = 1979-05-27");
        StringAssert.Contains(text, "lt = 07:32:00");
    }

    [TestMethod]
    public void Format_WhenNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Toml.Format(null!));
    }

    /// <summary>
    /// Verifies that a representative set of documents round-trips through parse → format → parse with an equal model.
    /// </summary>
    /// <param name="source">The TOML document.</param>
    [TestMethod]
    [DataRow("a = \"x\"\nb = 1\nc = 3.5\nd = true\ne = -2\nf = 1_000")]
    [DataRow("[t]\nx = 1\n[t.u]\ny = 2\n[t.u.v]\nz = 3")]
    [DataRow("a.b.c = 1\na.b.d = 2")]
    [DataRow("arr = [1, 2, [3, 4], \"x\"]")]
    [DataRow("inline = { a = 1, b = { c = 2 }, d = [1, 2] }")]
    [DataRow("[[p]]\nn = 1\n[[p]]\nn = 2\n[[p]]\nn = 3")]
    [DataRow("[[fruits]]\nname = \"apple\"\n[fruits.physical]\ncolor = \"red\"\n[[fruits.varieties]]\nname = \"red delicious\"\n[[fruits]]\nname = \"banana\"")]
    [DataRow("\"weird key\" = 1\nnums = [0.1, 1, 6.626e-34]")]
    [DataRow("odt = 1979-05-27T07:32:00Z\nld = 1979-05-27\nlt = 07:32:00.5")]
    [DataRow("s = \"line1\\nline2\\ttab\\\"quote\"")]
    public void FormatRoundTrip_WhenParsedThenFormatted_ShouldYieldEqualModel(string source)
    {
        TomlTable original = Toml.Parse(source);
        TomlTable reparsed = Toml.Parse(Toml.Format(original));

        Assert.AreEqual(Normalize(original), Normalize(reparsed));
    }

    private static string Normalize(TomlValue value)
    {
        switch (value)
        {
            case TomlTable table:
                var entries = new List<string>();
                foreach (KeyValuePair<string, TomlValue> pair in table)
                    entries.Add("<" + pair.Key + ">=" + Normalize(pair.Value));
                entries.Sort(StringComparer.Ordinal);
                return "{" + string.Join(",", entries) + "}";
            case TomlArray array:
                var items = new List<string>();
                foreach (TomlValue item in array)
                    items.Add(Normalize(item));
                return "[" + string.Join(",", items) + "]";
            default:
                return value.Kind + ":" + value;
        }
    }
}
