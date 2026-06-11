// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.Arrays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that an empty array surfaces as adjacent start and end array boundaries with no elements.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayIsEmpty_ShouldSurfaceEmptyArray()
    {
        TomlDocumentReader reader = Create("v = []\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartArray);
        ExpectToken(ref reader, TomlTokenType.EndArray);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that an array with a trailing comma is accepted and surfaces its declared elements.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayHasTrailingComma_ShouldSurfaceElements()
    {
        TomlDocumentReader reader = Create("v = [1, 2, 3,]\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartArray);
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(2L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(3L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndArray);
    }

    /// <summary>
    /// Verifies that an array may hold elements of differing value kinds.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayHasMixedTypes_ShouldSurfaceEachElementKind()
    {
        TomlDocumentReader reader = Create("v = [1, \"two\", true, 3.5]\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartArray);

        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("two", reader.GetString());
        ExpectToken(ref reader, TomlTokenType.Boolean);
        Assert.IsTrue(reader.GetBoolean());
        ExpectToken(ref reader, TomlTokenType.Float);
        Assert.AreEqual(3.5d, reader.GetDouble());

        ExpectToken(ref reader, TomlTokenType.EndArray);
    }

    /// <summary>
    /// Verifies that a nested array surfaces as a start/end array boundary nested within the outer array.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayIsNested_ShouldSurfaceNestedBoundaries()
    {
        TomlDocumentReader reader = Create("v = [[1, 2], [3]]\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartArray);

        ExpectToken(ref reader, TomlTokenType.StartArray);
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(2L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndArray);

        ExpectToken(ref reader, TomlTokenType.StartArray);
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(3L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndArray);

        ExpectToken(ref reader, TomlTokenType.EndArray);
    }

    /// <summary>
    /// Verifies that newlines and comments interspersed between array elements are ignored.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenArraySpansLinesWithComments_ShouldSurfaceElements()
    {
        TomlDocumentReader reader = Create("v = [\n  1, # first\n  2,\n  # comment line\n  3\n]\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartArray);
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(2L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(3L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndArray);
    }

    /// <summary>
    /// Verifies that an unterminated or malformed array is rejected with <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="source">The malformed array document under test.</param>
    [TestMethod]
    [DataRow("v = [1, 2\n", DisplayName = "unterminated")]
    [DataRow("v = [1 2]\n", DisplayName = "missing comma")]
    [DataRow("v = [, 1]\n", DisplayName = "leading comma")]
    [DataRow("v = [1,, 2]\n", DisplayName = "double comma")]
    [TestCategory("Regression")]
    public void Read_WhenArrayMalformed_ShouldThrowTomlFormatException(string source)
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create(source);
        });
    }
}
