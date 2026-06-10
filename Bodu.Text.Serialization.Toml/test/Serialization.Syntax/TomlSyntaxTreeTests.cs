// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSyntaxTreeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Toml.Syntax;

namespace Bodu.Text.Serialization.Toml;

/// <summary>
/// Verifies that the TOML parser accepts valid documents losslessly and rejects malformed input.
/// </summary>
[TestClass]
public class TomlSyntaxTreeTests
{
    /// <summary>
    /// Verifies that parsing a valid document and reproducing it returns the exact original source.
    /// </summary>
    /// <param name="input">The valid TOML document.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("a = 1\n")]
    [DataRow("# comment\ntitle = \"x\"\n\n[owner]\nname = \"Tom\"\n")]
    [DataRow("ports = [ 8000, 8001, 8002 ]\n")]
    [DataRow("[a.b.c]\nx = true\n")]
    [DataRow("[[products]]\nname = \"Hammer\"\n\n[[products]]\nname = \"Nail\"\n")]
    [DataRow("dob = 1979-05-27T07:32:00Z\nd = 1979-05-27\nt = 07:32:00\n")]
    [DataRow("pi = 3.14159\nneg = -1\nhex = 0xff\n")]
    [DataRow("multi = \"\"\"\nline\n\"\"\"\nliteral = 'C:\\path'\n")]
    public void Parse_WhenDocumentIsValid_ShouldRoundTripLosslessly(string input)
    {
        TomlDocumentSyntax document = TomlSyntaxTree.Parse(input);

        Assert.AreEqual(input, document.ToFullString());
    }

    /// <summary>
    /// Verifies that parsing a malformed document throws <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="name">A label describing the malformed scenario.</param>
    /// <param name="input">The malformed TOML document.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("duplicate key", "a = 1\na = 2\n")]
    [DataRow("duplicate table", "[a]\n[a]\n")]
    [DataRow("missing value", "key = \n")]
    [DataRow("unterminated array", "a = [1, 2\n")]
    [DataRow("unterminated string", "a = \"abc\n")]
    [DataRow("bare key no value", "a\n")]
    [DataRow("leading zero integer", "a = 01\n")]
    [DataRow("invalid date", "a = 2026-13-01\n")]
    [DataRow("extend inline table", "a = { b = 1 }\n[a.c]\nd = 2\n")]
    public void Parse_WhenDocumentIsMalformed_ShouldThrowTomlFormatException(string name, string input)
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlSyntaxTree.Parse(input);
        });
    }

    /// <summary>
    /// Verifies that the v1.1.0 grammar accepts a time value without seconds while the strict default rejects it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTimeOmitsSeconds_ShouldDependOnSpecVersion()
    {
        const string Source = "t = 07:32\n";

        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlSyntaxTree.Parse(Source);
        });

        TomlDocumentSyntax document = TomlSyntaxTree.Parse(Source, TomlSpecVersion.V1_1);
        Assert.AreEqual(Source, document.ToFullString());
    }
}
