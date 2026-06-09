// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlParseTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Behavioural tests for <see cref="Toml.Parse(ReadOnlySpan{char})" /> under the default strict TOML v1.0.0 profile,
/// covering the value types, tables, dotted keys, arrays, inline tables, arrays of tables, and the specification's
/// rejection rules. Version-boundary behaviour (the TOML v1.1.0 additions) lives in <c>TomlReaderVersionTests</c>.
/// </summary>
[TestClass]
public sealed class TomlParseTests
{
    private static TomlTable Parse(string source) => Toml.Parse(source);

    private static string Str(TomlValue v) => ((TomlString)v).Value;

    private static long Int(TomlValue v) => ((TomlInteger)v).Value;

    [TestMethod]
    public void Parse_WhenBasicKeyValue_ShouldReadString()
    {
        var doc = Parse("""key = "value" """);
        Assert.AreEqual("value", Str(doc["key"]));
    }

    [TestMethod]
    public void Parse_WhenIntegerForms_ShouldReadValues()
    {
        var doc = Parse("a = 42\nb = -17\nc = 1_000\nd = 0xDEAD_BEEF\ne = 0o755\nf = 0b1101");
        Assert.AreEqual(42, Int(doc["a"]));
        Assert.AreEqual(-17, Int(doc["b"]));
        Assert.AreEqual(1000, Int(doc["c"]));
        Assert.AreEqual(0xDEADBEEF, Int(doc["d"]));
        Assert.AreEqual(493, Int(doc["e"]));
        Assert.AreEqual(13, Int(doc["f"]));
    }

    [TestMethod]
    public void Parse_WhenFloatForms_ShouldReadValues()
    {
        var doc = Parse("a = 3.1415\nb = 5e+22\nc = -2E-2\nd = 6.626e-34\ne = 224_617.445_991\nf = inf\ng = -inf\nh = nan");
        Assert.AreEqual(3.1415, ((TomlFloat)doc["a"]).Value, 1e-9);
        Assert.AreEqual(5e22, ((TomlFloat)doc["b"]).Value);
        Assert.IsTrue(double.IsPositiveInfinity(((TomlFloat)doc["f"]).Value));
        Assert.IsTrue(double.IsNegativeInfinity(((TomlFloat)doc["g"]).Value));
        Assert.IsTrue(double.IsNaN(((TomlFloat)doc["h"]).Value));
    }

    [TestMethod]
    public void Parse_WhenBooleans_ShouldReadValues()
    {
        var doc = Parse("t = true\nf = false");
        Assert.IsTrue(((TomlBoolean)doc["t"]).Value);
        Assert.IsFalse(((TomlBoolean)doc["f"]).Value);
    }

    [TestMethod]
    public void Parse_WhenBasicStringEscapes_ShouldDecode()
    {
        var doc = Parse("""s = "a\tb\nc\"d\\eé" """);
        Assert.AreEqual("a\tb\nc\"d\\eé", Str(doc["s"]));
    }

    [TestMethod]
    public void Parse_WhenLiteralString_ShouldNotUnescape()
    {
        var doc = Parse("""path = 'C:\Users\nodejs' """);
        Assert.AreEqual(@"C:\Users\nodejs", Str(doc["path"]));
    }

    [TestMethod]
    public void Parse_WhenMultilineBasicWithLineEndingBackslash_ShouldTrim()
    {
        var doc = Parse("str = \"\"\"\nThe quick brown \\\n\n  fox jumps over \\\n    the lazy dog.\"\"\"");
        Assert.AreEqual("The quick brown fox jumps over the lazy dog.", Str(doc["str"]));
    }

    [TestMethod]
    public void Parse_WhenMultilineBasicTrimsLeadingNewline_ShouldMatch()
    {
        var doc = Parse("str = \"\"\"\nRoses are red\nViolets are blue\"\"\"");
        Assert.AreEqual("Roses are red\nViolets are blue", Str(doc["str"]));
    }

    [TestMethod]
    public void Parse_WhenMultilineBasicWithInteriorQuotes_ShouldKeep()
    {
        var doc = Parse("str = \"\"\"Here are two quotation marks: \"\". Simple enough.\"\"\"");
        Assert.AreEqual("Here are two quotation marks: \"\". Simple enough.", Str(doc["str"]));
    }

    [TestMethod]
    public void Parse_WhenDottedKeys_ShouldCreateNestedTables()
    {
        var doc = Parse("physical.color = \"orange\"\nphysical.shape = \"round\"\nsite.\"google.com\" = true");
        var physical = (TomlTable)doc["physical"];
        Assert.AreEqual("orange", Str(physical["color"]));
        Assert.AreEqual("round", Str(physical["shape"]));
        Assert.IsTrue(((TomlBoolean)((TomlTable)doc["site"])["google.com"]).Value);
    }

    [TestMethod]
    public void Parse_WhenStandardTables_ShouldGroupKeys()
    {
        var doc = Parse("[table-1]\nkey1 = \"a\"\nkey2 = 123\n\n[table-2]\nkey1 = \"b\"");
        Assert.AreEqual("a", Str(((TomlTable)doc["table-1"])["key1"]));
        Assert.AreEqual(123, Int(((TomlTable)doc["table-1"])["key2"]));
        Assert.AreEqual("b", Str(((TomlTable)doc["table-2"])["key1"]));
    }

    [TestMethod]
    public void Parse_WhenSuperTableImplicit_ShouldCreateAncestors()
    {
        var doc = Parse("[x.y.z.w]\na = 1\n\n[x]\nb = 2");
        var x = (TomlTable)doc["x"];
        Assert.AreEqual(2, Int(x["b"]));
        Assert.AreEqual(1, Int(((TomlTable)((TomlTable)((TomlTable)x["y"])["z"])["w"])["a"]));
    }

    [TestMethod]
    public void Parse_WhenArray_ShouldReadElements()
    {
        var doc = Parse("a = [ 1, 2, 3 ]\nb = [ \"x\", 'y' ]\nc = [\n  1,\n  2, # trailing comma ok\n]");
        var a = (TomlArray)doc["a"];
        Assert.AreEqual(3, a.Count);
        Assert.AreEqual(2, Int(a[1]));
        Assert.AreEqual(2, ((TomlArray)doc["c"]).Count);
    }

    [TestMethod]
    public void Parse_WhenInlineTable_ShouldReadKeys()
    {
        var doc = Parse("name = { first = \"Tom\", last = \"Preston-Werner\" }\npoint = {x=1,y=2}\nanimal = { type.name = \"pug\" }");
        var name = (TomlTable)doc["name"];
        Assert.AreEqual("Tom", Str(name["first"]));
        Assert.AreEqual(1, Int(((TomlTable)doc["point"])["x"]));
        Assert.AreEqual("pug", Str(((TomlTable)((TomlTable)doc["animal"])["type"])["name"]));
    }

    [TestMethod]
    public void Parse_WhenArrayOfTables_ShouldAppendElements()
    {
        var doc = Parse("[[product]]\nname = \"Hammer\"\nsku = 738594937\n\n[[product]]\n\n[[product]]\nname = \"Nail\"\ncolor = \"gray\"");
        var products = (TomlArray)doc["product"];
        Assert.AreEqual(3, products.Count);
        Assert.AreEqual("Hammer", Str(((TomlTable)products[0])["name"]));
        Assert.AreEqual(0, ((TomlTable)products[1]).Count);
        Assert.AreEqual("Nail", Str(((TomlTable)products[2])["name"]));
    }

    [TestMethod]
    public void Parse_WhenNestedArrayOfTables_ShouldMatchSpecExample()
    {
        var doc = Parse("[[fruits]]\nname = \"apple\"\n\n[fruits.physical]\ncolor = \"red\"\n\n[[fruits.varieties]]\nname = \"red delicious\"\n\n[[fruits.varieties]]\nname = \"granny smith\"\n\n[[fruits]]\nname = \"banana\"\n\n[[fruits.varieties]]\nname = \"plantain\"");
        var fruits = (TomlArray)doc["fruits"];
        Assert.AreEqual(2, fruits.Count);
        var apple = (TomlTable)fruits[0];
        Assert.AreEqual("red", Str(((TomlTable)apple["physical"])["color"]));
        Assert.AreEqual(2, ((TomlArray)apple["varieties"]).Count);
        Assert.AreEqual("plantain", Str(((TomlTable)((TomlArray)((TomlTable)fruits[1])["varieties"])[0])["name"]));
    }

    [TestMethod]
    public void Parse_WhenOffsetDateTime_ShouldReadInstant()
    {
        var doc = Parse("odt = 1979-05-27T07:32:00Z\nspace = 1979-05-27 07:32:00Z");
        Assert.AreEqual(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero), ((TomlOffsetDateTime)doc["odt"]).Value);
        Assert.AreEqual(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero), ((TomlOffsetDateTime)doc["space"]).Value);
    }

    [TestMethod]
    public void Parse_WhenLocalDateTimeDateAndTime_ShouldReadValues()
    {
        var doc = Parse("ldt = 1979-05-27T07:32:00\nld = 1979-05-27\nlt = 07:32:00");
        Assert.AreEqual(new DateTime(1979, 5, 27, 7, 32, 0), ((TomlLocalDateTime)doc["ldt"]).Value);
        Assert.AreEqual(new DateOnly(1979, 5, 27), ((TomlLocalDate)doc["ld"]).Value);
        Assert.AreEqual(new TimeOnly(7, 32, 0), ((TomlLocalTime)doc["lt"]).Value);
    }

    [TestMethod]
    [DataRow("a = 1\na = 2")]                              // duplicate key
    [DataRow("[a]\n[a]")]                                  // duplicate table
    [DataRow("[fruit]\napple = 1\n[fruit.apple]\nx = 1")] // redefine value as table
    [DataRow("fruit.apple = 1\nfruit.apple.smooth = true")]
    [DataRow("fruits = []\n[[fruits]]")]                   // append to static array
    [DataRow("[[fruits]]\n[fruits]")]                      // table conflicts with array of tables
    [DataRow("a = { b = 1 }\na.c = 2")]                    // extend inline table
    [DataRow("key = ")]                                    // missing value
    [DataRow("= 1")]                                       // missing key
    [DataRow("a = 1 b = 2")]                               // two values on a line
    [DataRow("a = [1, 2")]                                 // unterminated array
    [DataRow("invalid_float_1 = .7")]
    [DataRow("invalid_float_2 = 7.")]
    [DataRow("invalid_float_3 = 3.e+20")]
    [DataRow("leading_zero = 01")]
    [DataRow("bad = 1__0")]
    [DataRow("overflow = 9223372036854775808")]              // 2^63, above long.MaxValue
    [DataRow("leap_time = 23:59:60")]                        // leap second (local time)
    [DataRow("leap_datetime = 1979-05-27T23:59:60Z")]        // leap second (offset date-time)
    [DataRow("bad_hour = 24:00:00")]                         // hour out of range
    [DataRow("bad_minute = 23:60:00")]                       // minute out of range
    public void Parse_WhenInvalid_ShouldThrowTomlFormatException(string source)
    {
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(source));
    }

    [TestMethod]
    public void TryParse_WhenInvalid_ShouldReturnFalse()
    {
        Assert.IsFalse(Toml.TryParse("a = ", out var doc));
        Assert.IsNull(doc);
    }

    [TestMethod]
    public void TryParse_WhenValid_ShouldReturnTrue()
    {
        Assert.IsTrue(Toml.TryParse("a = 1", out var doc));
        Assert.AreEqual(1, Int(doc["a"]));
    }
}
