// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlWriterModelBoundaryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Tests for object-model states that the parser never produces but the mutable model permits, ensuring the writer
/// defends itself rather than emitting invalid TOML or recursing without bound: reference cycles, unpaired surrogates,
/// and control-character keys. The closed <see cref="TomlValue" /> hierarchy makes an unknown value kind impossible to
/// author, so that case is a compile-time guarantee rather than a runtime test.
/// </summary>
[TestClass]
public sealed class TomlWriterModelBoundaryTests
{
    /// <summary>
    /// Verifies that a table that directly references itself is rejected with <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Format_WhenTableContainsItself_ShouldThrowInvalidOperationException()
    {
        var table = new TomlTable();
        table["self"] = table;

        Assert.ThrowsExactly<InvalidOperationException>(() => Toml.Format(table));
    }

    /// <summary>
    /// Verifies that an inline array that references itself is rejected with <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Format_WhenArrayContainsItself_ShouldThrowInvalidOperationException()
    {
        var array = new TomlArray();
        array.Add(array);
        var document = new TomlTable { ["a"] = array };

        Assert.ThrowsExactly<InvalidOperationException>(() => Toml.Format(document));
    }

    /// <summary>
    /// Verifies that an indirect cycle through an array of tables is rejected with
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Format_WhenCycleThroughArrayOfTables_ShouldThrowInvalidOperationException()
    {
        var inner = new TomlTable();
        var array = new TomlArray();
        array.Add(inner);
        inner["back"] = array;
        var document = new TomlTable { ["x"] = array };

        Assert.ThrowsExactly<InvalidOperationException>(() => Toml.Format(document));
    }

    /// <summary>
    /// Verifies that a shared but acyclic sub-table is written without error, confirming that cycle detection releases
    /// nodes on exit.
    /// </summary>
    [TestMethod]
    public void Format_WhenSubtableSharedAcyclically_ShouldNotThrow()
    {
        var shared = new TomlTable { ["v"] = new TomlInteger(1) };
        var document = new TomlTable { ["a"] = shared, ["b"] = shared };

        var text = Toml.Format(document);

        TomlTable reparsed = Toml.Parse(text);
        Assert.AreEqual(1, ((TomlInteger)((TomlTable)reparsed["a"])["v"]).Value);
        Assert.AreEqual(1, ((TomlInteger)((TomlTable)reparsed["b"])["v"]).Value);
    }

    /// <summary>
    /// Verifies that a string value containing an unpaired surrogate is rejected before writing.
    /// </summary>
    [TestMethod]
    public void Format_WhenStringHasUnpairedSurrogate_ShouldThrowInvalidOperationException()
    {
        var document = new TomlTable { ["s"] = new TomlString("\uD800") };

        Assert.ThrowsExactly<InvalidOperationException>(() => Toml.Format(document));
    }

    /// <summary>
    /// Verifies that a key containing an unpaired surrogate is rejected before writing.
    /// </summary>
    [TestMethod]
    public void Format_WhenKeyHasUnpairedSurrogate_ShouldThrowInvalidOperationException()
    {
        var document = new TomlTable { ["\uDC00"] = new TomlInteger(1) };

        Assert.ThrowsExactly<InvalidOperationException>(() => Toml.Format(document));
    }

    /// <summary>
    /// Verifies that a valid non-BMP scalar (a surrogate pair) is written and round-trips intact.
    /// </summary>
    [TestMethod]
    public void Format_WhenStringHasSurrogatePair_ShouldRoundTrip()
    {
        var document = new TomlTable { ["s"] = new TomlString("\U0001F600") };

        TomlTable reparsed = Toml.Parse(Toml.Format(document));

        Assert.AreEqual("\U0001F600", ((TomlString)reparsed["s"]).Value);
    }

    /// <summary>
    /// Verifies that a key containing a newline is quoted and escaped so that it round-trips to the same key.
    /// </summary>
    [TestMethod]
    public void Format_WhenKeyContainsNewline_ShouldEmitQuotedKeyThatRoundTrips()
    {
        var document = new TomlTable { ["line\nkey"] = new TomlInteger(1) };

        TomlTable reparsed = Toml.Parse(Toml.Format(document));

        Assert.AreEqual(1, ((TomlInteger)reparsed["line\nkey"]).Value);
    }

    /// <summary>
    /// Verifies that a document containing every scalar kind is written as strict TOML v1.0.0 (it reparses under the
    /// default profile) and round-trips to an equal model.
    /// </summary>
    [TestMethod]
    public void Format_WhenDocumentContainsAllScalarKinds_ShouldEmitStrictV10ThatRoundTrips()
    {
        var document = new TomlTable
        {
            ["s"] = new TomlString("text"),
            ["i"] = new TomlInteger(long.MinValue),
            ["f"] = new TomlFloat(1.25),
            ["b"] = new TomlBoolean(true),
            ["odt"] = new TomlOffsetDateTime(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero)),
            ["ldt"] = new TomlLocalDateTime(new DateTime(1979, 5, 27, 7, 32, 0)),
            ["ld"] = new TomlLocalDate(new DateOnly(1979, 5, 27)),
            ["lt"] = new TomlLocalTime(new TimeOnly(7, 32, 0)),
        };

        // Parsing under the strict v1.0 default proves the writer emitted no v1.1-only syntax.
        TomlTable reparsed = Toml.Parse(Toml.Format(document));

        Assert.AreEqual("text", ((TomlString)reparsed["s"]).Value);
        Assert.AreEqual(long.MinValue, ((TomlInteger)reparsed["i"]).Value);
        Assert.AreEqual(1.25, ((TomlFloat)reparsed["f"]).Value);
        Assert.IsTrue(((TomlBoolean)reparsed["b"]).Value);
        Assert.AreEqual(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero), ((TomlOffsetDateTime)reparsed["odt"]).Value);
        Assert.AreEqual(new DateTime(1979, 5, 27, 7, 32, 0), ((TomlLocalDateTime)reparsed["ldt"]).Value);
        Assert.AreEqual(new DateOnly(1979, 5, 27), ((TomlLocalDate)reparsed["ld"]).Value);
        Assert.AreEqual(new TimeOnly(7, 32, 0), ((TomlLocalTime)reparsed["lt"]).Value);
    }
}
