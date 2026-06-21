// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigurationLoaderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
            [],
            CancellationToken.None);

        Assert.AreEqual(120, options.MaxLineLength);
    }

    /// <summary>
    /// Verifies that the loader applies values from a <c>bodu.xmldocstyle.json</c> additional file.
    /// </summary>
    [TestMethod]
    public void LoadCompilationOptions_WhenJsonFilePresent_ShouldApplyJsonValues()
    {
        var additional = ImmutableArray.Create<AdditionalText>(
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
        var additional = ImmutableArray.Create<AdditionalText>(
            new FakeAdditionalText("/repo/bodu.xmldocstyle.json", "{not valid"));

        XmlDocFormatOptions options = XmlDocConfigurationLoader.LoadCompilationOptions(additional, CancellationToken.None);

        Assert.AreEqual(120, options.MaxLineLength);
    }

    /// <summary>
    /// Verifies that malformed JSON is surfaced as a configuration error so the analyzer can report it,
    /// rather than being swallowed silently.
    /// </summary>
    [TestMethod]
    public void CollectConfigurationErrors_WhenJsonIsMalformed_ShouldReturnError()
    {
        var additional = ImmutableArray.Create<AdditionalText>(
            new FakeAdditionalText("/repo/bodu.xmldocstyle.json", "{not valid"));

        ImmutableArray<XmlDocConfigurationError> errors =
            XmlDocConfigurationLoader.CollectConfigurationErrors(additional, CancellationToken.None);

        Assert.AreEqual(1, errors.Length);
        Assert.AreEqual("/repo/bodu.xmldocstyle.json", errors[0].Location.GetLineSpan().Path);
        Assert.IsFalse(string.IsNullOrWhiteSpace(errors[0].Message));
    }

    /// <summary>
    /// Verifies that a valid configuration file produces no configuration errors.
    /// </summary>
    [TestMethod]
    public void CollectConfigurationErrors_WhenJsonIsValid_ShouldReturnNoErrors()
    {
        var additional = ImmutableArray.Create<AdditionalText>(
            new FakeAdditionalText("/repo/bodu.xmldocstyle.json", "{\"maxLineLength\":80}"));

        ImmutableArray<XmlDocConfigurationError> errors =
            XmlDocConfigurationLoader.CollectConfigurationErrors(additional, CancellationToken.None);

        Assert.AreEqual(0, errors.Length);
    }

    /// <summary>
    /// Verifies that no configuration error is produced when no configuration file is present.
    /// </summary>
    [TestMethod]
    public void CollectConfigurationErrors_WhenNoConfigFile_ShouldReturnNoErrors()
    {
        ImmutableArray<XmlDocConfigurationError> errors =
            XmlDocConfigurationLoader.CollectConfigurationErrors([], CancellationToken.None);

        Assert.AreEqual(0, errors.Length);
    }

    /// <summary>
    /// Verifies that additional files with unrelated names are ignored.
    /// </summary>
    [TestMethod]
    public void LoadCompilationOptions_WhenUnrelatedAdditionalFile_ShouldIgnore()
    {
        var additional = ImmutableArray.Create<AdditionalText>(
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
        XmlDocFormatOptions defaults = XmlDocFormatPolicyDefaults.CreateDefaults();
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
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
        XmlDocFormatOptions defaults = XmlDocFormatPolicyDefaults.CreateDefaults();
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["bodu_xmldoc_max_line_length"] = "abc",
        });

        XmlDocFormatOptions result = XmlDocConfigurationLoader.ApplyEditorConfigOverrides(defaults, config);

        Assert.AreEqual(120, result.MaxLineLength);
    }

    /// <summary>
    /// Verifies that the <c>bodu_xmldoc_indent_size</c> override sets <see cref="XmlDocFormatOptions.IndentText" />
    /// to that many spaces.
    /// </summary>
    [TestMethod]
    public void ApplyEditorConfigOverrides_WhenIndentSizeSet_ShouldSetIndentText()
    {
        XmlDocFormatOptions defaults = XmlDocFormatPolicyDefaults.CreateDefaults();
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["bodu_xmldoc_indent_size"] = "2",
        });

        XmlDocFormatOptions result = XmlDocConfigurationLoader.ApplyEditorConfigOverrides(defaults, config);

        Assert.AreEqual("  ", result.IndentText);
    }

    /// <summary>
    /// Verifies that <c>bodu_xmldoc_force_summary_multiline = false</c> removes <c>summary</c> from the
    /// force-multiline set.
    /// </summary>
    [TestMethod]
    public void ApplyEditorConfigOverrides_WhenForceSummaryMultilineFalse_ShouldRemoveSummary()
    {
        XmlDocFormatOptions defaults = XmlDocFormatPolicyDefaults.CreateDefaults();
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["bodu_xmldoc_force_summary_multiline"] = "false",
        });

        XmlDocFormatOptions result = XmlDocConfigurationLoader.ApplyEditorConfigOverrides(defaults, config);

        Assert.IsFalse(result.ForceMultilineTags.Contains("summary"));
    }

    /// <summary>
    /// Verifies that <c>bodu_xmldoc_force_para_multiline = false</c> removes <c>para</c> from the force-multiline
    /// set.
    /// </summary>
    [TestMethod]
    public void ApplyEditorConfigOverrides_WhenForceParaMultilineFalse_ShouldRemovePara()
    {
        XmlDocFormatOptions defaults = XmlDocFormatPolicyDefaults.CreateDefaults();
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["bodu_xmldoc_force_para_multiline"] = "false",
        });

        XmlDocFormatOptions result = XmlDocConfigurationLoader.ApplyEditorConfigOverrides(defaults, config);

        Assert.IsFalse(result.ForceMultilineTags.Contains("para"));
    }

    /// <summary>
    /// Verifies that <c>bodu_xmldoc_preserve_inline_tags = false</c> clears the inline-atomic tag set so inline
    /// elements may wrap.
    /// </summary>
    [TestMethod]
    public void ApplyEditorConfigOverrides_WhenPreserveInlineTagsFalse_ShouldClearInlineTags()
    {
        XmlDocFormatOptions defaults = XmlDocFormatPolicyDefaults.CreateDefaults();
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["bodu_xmldoc_preserve_inline_tags"] = "false",
        });

        XmlDocFormatOptions result = XmlDocConfigurationLoader.ApplyEditorConfigOverrides(defaults, config);

        Assert.AreEqual(0, result.InlineTags.Count);
    }

    /// <summary>
    /// Verifies that the resolved line ending defaults to CRLF when not specified.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenNotSpecified_ShouldReturnCrlf()
    {
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>());

        var result = XmlDocConfigurationLoader.ResolveLineEnding(config);

        Assert.AreEqual("\r\n", result);
    }

    /// <summary>
    /// Verifies that the resolved line ending honours an explicit LF override.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenSpecifiedLf_ShouldReturnLf()
    {
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["end_of_line"] = "lf",
        });

        var result = XmlDocConfigurationLoader.ResolveLineEnding(config);

        Assert.AreEqual("\n", result);
    }

    /// <summary>
    /// Verifies that the resolved line ending honours an explicit CRLF override.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenSpecifiedCrlf_ShouldReturnCrlf()
    {
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["end_of_line"] = "crlf",
        });

        var result = XmlDocConfigurationLoader.ResolveLineEnding(config);

        Assert.AreEqual("\r\n", result);
    }

    /// <summary>
    /// Verifies that, with no <c>end_of_line</c> key, the line ending is inferred from the file's first newline
    /// — an LF file resolves to LF rather than the CRLF default.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenNotSpecifiedButFileIsLf_ShouldReturnLf()
    {
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>());

        var result = XmlDocConfigurationLoader.ResolveLineEnding(config, SourceText.From("line one\nline two\n"));

        Assert.AreEqual("\n", result);
    }

    /// <summary>
    /// Verifies that, with no <c>end_of_line</c> key, a CRLF file is inferred as CRLF.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenNotSpecifiedButFileIsCrlf_ShouldReturnCrlf()
    {
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>());

        var result = XmlDocConfigurationLoader.ResolveLineEnding(config, SourceText.From("line one\r\nline two\r\n"));

        Assert.AreEqual("\r\n", result);
    }

    /// <summary>
    /// Verifies that an explicit <c>end_of_line</c> key wins over the file's observed newline style.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenEditorConfigSpecifiedAndFileDiffers_ShouldHonourEditorConfig()
    {
        var config = new FakeAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["end_of_line"] = "lf",
        });

        var result = XmlDocConfigurationLoader.ResolveLineEnding(config, SourceText.From("line one\r\nline two\r\n"));

        Assert.AreEqual("\n", result);
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
            if (this._values.TryGetValue(key, out var raw) && raw is not null)
            {
                value = raw;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
