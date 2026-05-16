// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigurationLoaderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Bodu.CodeStyle.XmlDocumentation;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Test.Configuration;

[TestClass]
public sealed class XmlDocConfigurationLoaderTests
{
    /// <summary>
    /// Verifies that the loader returns the Bodu defaults when no additional files are present.
    /// </summary>
    [TestMethod]
    public void LoadCompilationOptions_WhenNoAdditionalFiles_ShouldReturnDefaults()
    {
        XmlDocFormatOptions options = XmlDocConfigurationLoader.LoadCompilationOptions(
            ImmutableArray<AdditionalText>.Empty,
            CancellationToken.None);

        Assert.AreEqual(120, options.MaxLineLength);
    }

    /// <summary>
    /// Verifies that the loader applies values from a <c>bodu.xmldocstyle.json</c> additional file.
    /// </summary>
    [TestMethod]
    public void LoadCompilationOptions_WhenJsonFilePresent_ShouldApplyJsonValues()
    {
        ImmutableArray<AdditionalText> additional = ImmutableArray.Create<AdditionalText>(
            new FakeAdditionalText("/repo/bodu.xmldocstyle.json", "{\"maxLineLength\":80}"));

        XmlDocFormatOptions options = XmlDocConfigurationLoader.LoadCompilationOptions(additional, CancellationToken.None);

        Assert.AreEqual(80, options.MaxLineLength);
    }

    /// <summary>
    /// Verifies that malformed JSON does not crash the loader; defaults are used instead.
    /// </summary>
    [TestMethod]
    public void LoadCompilationOptions_WhenJsonIsMalformed_ShouldFallBackToDefaults()
    {
        ImmutableArray<AdditionalText> additional = ImmutableArray.Create<AdditionalText>(
            new FakeAdditionalText("/repo/bodu.xmldocstyle.json", "{not valid"));

        XmlDocFormatOptions options = XmlDocConfigurationLoader.LoadCompilationOptions(additional, CancellationToken.None);

        Assert.AreEqual(120, options.MaxLineLength);
    }

    /// <summary>
    /// Verifies that additional files with unrelated names are ignored.
    /// </summary>
    [TestMethod]
    public void LoadCompilationOptions_WhenUnrelatedAdditionalFile_ShouldIgnore()
    {
        ImmutableArray<AdditionalText> additional = ImmutableArray.Create<AdditionalText>(
            new FakeAdditionalText("/repo/foo.txt", "{\"maxLineLength\":80}"));

        XmlDocFormatOptions options = XmlDocConfigurationLoader.LoadCompilationOptions(additional, CancellationToken.None);

        Assert.AreEqual(120, options.MaxLineLength);
    }

    /// <summary>
    /// Verifies that <see cref="XmlDocConfigurationLoader.ApplyEditorConfigOverrides" /> overrides the max line
    /// length when the corresponding <c>.editorconfig</c> key is present.
    /// </summary>
    [TestMethod]
    public void ApplyEditorConfigOverrides_WhenMaxLineLengthOverride_ShouldApply()
    {
        XmlDocFormatOptions defaults = XmlDocFormatPolicyDefaults.CreateBoduDefaults();
        FakeAnalyzerConfigOptions config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["bodu_xmldoc_max_line_length"] = "100",
        });

        XmlDocFormatOptions result = XmlDocConfigurationLoader.ApplyEditorConfigOverrides(defaults, config);

        Assert.AreEqual(100, result.MaxLineLength);
    }

    /// <summary>
    /// Verifies that a non-numeric override is ignored and the compilation-level value is preserved.
    /// </summary>
    [TestMethod]
    public void ApplyEditorConfigOverrides_WhenMaxLineLengthIsNotInteger_ShouldKeepCompilationValue()
    {
        XmlDocFormatOptions defaults = XmlDocFormatPolicyDefaults.CreateBoduDefaults();
        FakeAnalyzerConfigOptions config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["bodu_xmldoc_max_line_length"] = "abc",
        });

        XmlDocFormatOptions result = XmlDocConfigurationLoader.ApplyEditorConfigOverrides(defaults, config);

        Assert.AreEqual(120, result.MaxLineLength);
    }

    /// <summary>
    /// Verifies that the resolved line ending defaults to CRLF when not specified.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenNotSpecified_ShouldReturnCrlf()
    {
        FakeAnalyzerConfigOptions config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>());

        string result = XmlDocConfigurationLoader.ResolveLineEnding(config);

        Assert.AreEqual("\r\n", result);
    }

    /// <summary>
    /// Verifies that the resolved line ending honours an explicit LF override.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenSpecifiedLf_ShouldReturnLf()
    {
        FakeAnalyzerConfigOptions config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["end_of_line"] = "lf",
        });

        string result = XmlDocConfigurationLoader.ResolveLineEnding(config);

        Assert.AreEqual("\n", result);
    }

    /// <summary>
    /// Verifies that the resolved line ending honours an explicit CRLF override.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenSpecifiedCrlf_ShouldReturnCrlf()
    {
        FakeAnalyzerConfigOptions config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["end_of_line"] = "crlf",
        });

        string result = XmlDocConfigurationLoader.ResolveLineEnding(config);

        Assert.AreEqual("\r\n", result);
    }

    private sealed class FakeAdditionalText : AdditionalText
    {
        private readonly string _content;

        public FakeAdditionalText(string path, string content)
        {
            this.Path = path;
            this._content = content;
        }

        public override string Path { get; }

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(this._content);
    }

    private sealed class FakeAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public FakeAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
        {
            this._values = values;
        }

        public override bool TryGetValue(string key, out string value)
        {
            if (this._values.TryGetValue(key, out string? raw) && raw is not null)
            {
                value = raw;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
