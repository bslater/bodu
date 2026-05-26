// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentKatTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration.Kat;

/// <summary>
/// Drives <see cref="ConfigurationDocumentKat" />, <see cref="ConfigurationResolverKat" />, and
/// <see cref="ConfigurationViewGetterKat{T}" /> rows against the public Bodu configuration surface.
/// Each KAT row asserts one observable outcome — parse + resolve + lookup — and surfaces the row's
/// <c>Name</c> in failure diagnostics. Bespoke parser coverage (profiles, duplicate-key behaviour,
/// diagnostic modes) remains in the existing <c>ConfigurationDocumentTests.*</c> and
/// <c>ConfigurationKatRunnerTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class ConfigurationDocumentKatTests
{
    private static IReadOnlyList<ConfigurationDocumentKat> DocumentKats { get; } = new ConfigurationDocumentKat[]
    {
        new(
            Name: "wildcard section, single key",
            Text: "[*]\nname = alice\n",
            SectionGlob: "*",
            Key: "name",
            ExpectedValue: "alice"),

        new(
            Name: "absent key returns null",
            Text: "[*]\nname = alice\n",
            SectionGlob: "*",
            Key: "missing",
            ExpectedValue: null),
    };

    private static IReadOnlyList<ConfigurationResolverKat> ResolverKats { get; } = new ConfigurationResolverKat[]
    {
        new(
            Name: "wildcard matches any path",
            Text: "[*]\nindent_style = space\n",
            ResolvePath: "src/Program.cs",
            Key: "indent_style",
            ExpectedValue: "space"),

        new(
            Name: "extension glob matches .cs but not .md",
            Text: "[*.cs]\ninsert_final_newline = true\n",
            ResolvePath: "src/Program.cs",
            Key: "insert_final_newline",
            ExpectedValue: "true"),

        new(
            Name: "extension glob misses non-matching extension",
            Text: "[*.cs]\ninsert_final_newline = true\n",
            ResolvePath: "README.md",
            Key: "insert_final_newline",
            ExpectedValue: null),
    };

    private static IReadOnlyList<ConfigurationViewGetterKat<bool>> BooleanGetterKats { get; } =
        new ConfigurationViewGetterKat<bool>[]
    {
        new(
            Name: "true literal",
            Text: "[*]\nflag = true\n",
            Key: "flag",
            ExpectedValue: true),

        new(
            Name: "false literal",
            Text: "[*]\nflag = false\n",
            Key: "flag",
            ExpectedValue: false),
    };

    private static IReadOnlyList<ConfigurationViewGetterKat<int>> Int32GetterKats { get; } =
        new ConfigurationViewGetterKat<int>[]
    {
        new(
            Name: "positive integer",
            Text: "[*]\nport = 8080\n",
            Key: "port",
            ExpectedValue: 8080),

        new(
            Name: "zero",
            Text: "[*]\ncount = 0\n",
            Key: "count",
            ExpectedValue: 0),

        new(
            Name: "negative integer",
            Text: "[*]\noffset = -42\n",
            Key: "offset",
            ExpectedValue: -42),
    };

    /// <summary>
    /// Verifies that each <see cref="ConfigurationDocumentKat" /> row parses to a document whose
    /// resolved view returns the expected value for the probed key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDocumentKatIsKnown_ShouldYieldExpectedValue()
    {
        foreach (ConfigurationDocumentKat kat in DocumentKats)
        {
            IniDocument doc = ConfigurationDocument.Parse(kat.Text);
            ConfigurationView view = doc.Resolve("any.cs");

            Assert.AreEqual(kat.ExpectedValue, view[kat.Key], $"KAT '{kat.Name}': parsed view returned unexpected value.");
        }
    }

    /// <summary>
    /// Verifies that each <see cref="ConfigurationResolverKat" /> row resolves the supplied file path
    /// against the parsed document and returns the expected value for the probed key.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathIsKnown_ShouldYieldExpectedValue()
    {
        foreach (ConfigurationResolverKat kat in ResolverKats)
        {
            IniDocument doc = ConfigurationDocument.Parse(kat.Text);
            ConfigurationView view = doc.Resolve(kat.ResolvePath);

            Assert.AreEqual(kat.ExpectedValue, view[kat.Key], $"Resolver KAT '{kat.Name}': resolved view returned unexpected value.");
        }
    }

    /// <summary>
    /// Verifies that each <see cref="ConfigurationViewGetterKat{T}" /> row drives the
    /// <see cref="ConfigurationView.GetBoolean(string)" /> getter to its expected value.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenKatIsKnown_ShouldYieldExpectedValue()
    {
        foreach (ConfigurationViewGetterKat<bool> kat in BooleanGetterKats)
        {
            IniDocument doc = ConfigurationDocument.Parse(kat.Text);
            ConfigurationView view = doc.Resolve("any.cs");

            Assert.AreEqual(kat.ExpectedValue, view.GetBoolean(kat.Key), $"Boolean getter KAT '{kat.Name}': unexpected value.");
        }
    }

    /// <summary>
    /// Verifies that each <see cref="ConfigurationViewGetterKat{T}" /> row drives the
    /// <see cref="ConfigurationView.GetInt32(string)" /> getter to its expected value.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenKatIsKnown_ShouldYieldExpectedValue()
    {
        foreach (ConfigurationViewGetterKat<int> kat in Int32GetterKats)
        {
            IniDocument doc = ConfigurationDocument.Parse(kat.Text);
            ConfigurationView view = doc.Resolve("any.cs");

            Assert.AreEqual(kat.ExpectedValue, view.GetInt32(kat.Key), $"Int32 getter KAT '{kat.Name}': unexpected value.");
        }
    }
}
