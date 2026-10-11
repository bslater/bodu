// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.PlainScalars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies plain-scalar parsing rules: the indicators a plain scalar cannot begin with, the colon and hash indicator
/// boundaries, multi-line folding, and empty (null) mapping values.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a colon without a following space stays within a plain scalar (a URL).</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenColonHasNoFollowingSpace_ShouldStayInPlainScalar()
    {
        using var doc = YamlDocument.Parse("url: http://example.com\n");
        Assert.AreEqual("http://example.com", doc.RootElement.GetProperty("url").GetString());
    }

    /// <summary>Verifies that a hash without a preceding space does not start a comment.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenHashHasNoPrecedingSpace_ShouldNotStartComment()
    {
        using var doc = YamlDocument.Parse("text: hash#tag\n");
        Assert.AreEqual("hash#tag", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a space before a hash starts a comment that truncates the value.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenSpacePrecedesHash_ShouldTruncateAtComment()
    {
        using var doc = YamlDocument.Parse("text: hello # comment\n");
        Assert.AreEqual("hello", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a multi-line plain scalar folds continuation lines into spaces.</summary>
    [TestMethod]
    public void Parse_WhenMultiLinePlain_ShouldFold()
    {
        using var doc = YamlDocument.Parse("text: this is\n  a long value\n");
        Assert.AreEqual("this is a long value", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that an empty mapping value resolves to null.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenMappingValueEmpty_ShouldResolveToNull()
    {
        using var doc = YamlDocument.Parse("key:\nother: value\n");
        Assert.AreEqual(YamlValueKind.Null, doc.RootElement.GetProperty("key").ValueKind);
        Assert.AreEqual("value", doc.RootElement.GetProperty("other").GetString());
    }

    /// <summary>Verifies that an explicit key with an empty value resolves to null.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenExplicitKeyHasEmptyValue_ShouldResolveToNull()
    {
        using var doc = YamlDocument.Parse("? key\n:\n");
        Assert.AreEqual(YamlValueKind.Null, doc.RootElement.GetProperty("key").ValueKind);
    }

    /// <summary>
    /// Verifies that a plain scalar in block context cannot begin with an indicator that YAML reserves there, nor with
    /// <c>-</c>, <c>?</c> or <c>:</c> followed by a space.
    /// </summary>
    /// <param name="yaml">A document whose plain scalar begins with an indicator.</param>
    [TestMethod]
    [DataRow("]", DisplayName = "a closing bracket")]
    [DataRow("}", DisplayName = "a closing brace")]
    [DataRow("- ]", DisplayName = "a closing bracket as a sequence entry")]
    [DataRow("a: }", DisplayName = "a closing brace as a mapping value")]
    [DataRow("a: ? b", DisplayName = "a question mark and a space as a mapping value")]
    [DataRow("a: : b", DisplayName = "a colon and a space as a mapping value")]
    [DataRow("a: @b", DisplayName = "a commercial at")]
    [DataRow("a: `b", DisplayName = "a grave accent")]
    public void Parse_WhenBlockPlainScalarStartsWithIndicator_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that a plain scalar in flow context cannot begin with an indicator that YAML reserves there, nor with
    /// <c>-</c>, <c>?</c> or <c>:</c> followed by a space or a flow indicator.
    /// </summary>
    /// <param name="yaml">A flow collection whose plain scalar begins with an indicator.</param>
    [TestMethod]
    [DataRow("[ |a ]", DisplayName = "a vertical bar")]
    [DataRow("[ >a ]", DisplayName = "a greater-than sign")]
    [DataRow("[ %a ]", DisplayName = "a percent sign")]
    [DataRow("[ @a ]", DisplayName = "a commercial at")]
    [DataRow("[ `a ]", DisplayName = "a grave accent")]
    [DataRow("{ a: ? b }", DisplayName = "a question mark and a space as a mapping value")]
    [DataRow("[ -, a ]", DisplayName = "a hyphen before a comma")]
    public void Parse_WhenFlowPlainScalarStartsWithIndicator_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that <c>-</c>, <c>?</c> and <c>:</c> may begin a plain scalar when a character that could continue
    /// the scalar follows them, in block and flow context.
    /// </summary>
    /// <param name="yaml">A document whose plain scalars begin with those indicators.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("- -a\n- ?a\n- :a\n", """["-a","?a",":a"]""", DisplayName = "block context")]
    [DataRow("[ -a, ?a, :a ]", """["-a","?a",":a"]""", DisplayName = "flow context")]
    public void Parse_WhenPlainScalarStartsWithIndicatorAndSafeCharacter_ShouldReadIt(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that an implicit mapping key, first or later in its mapping, cannot begin with an indicator that cannot
    /// begin a plain scalar, nor with <c>-</c> followed by a space.
    /// </summary>
    /// <param name="yaml">A document with an implicit key that begins with an indicator.</param>
    [TestMethod]
    [DataRow(",foo: bar", DisplayName = "a comma, first key")]
    [DataRow("@foo: bar", DisplayName = "a commercial at, first key")]
    [DataRow("`foo: bar", DisplayName = "a grave accent, first key")]
    [DataRow("k: v\n%foo: bar", DisplayName = "a percent sign, later key")]
    [DataRow("k: v\n|foo: bar", DisplayName = "a vertical bar, later key")]
    [DataRow("k: v\n>foo: bar", DisplayName = "a greater-than sign, later key")]
    [DataRow("k: v\n]foo: bar", DisplayName = "a closing bracket, later key")]
    [DataRow("k: v\n}foo: bar", DisplayName = "a closing brace, later key")]
    [DataRow("k: v\n- : bar", DisplayName = "a dash and a space, later key")]
    [DataRow("- k: v\n  @foo: bar", DisplayName = "a commercial at, later key of a nested mapping")]
    [DataRow("&a @foo: bar", DisplayName = "a commercial at after an anchor")]
    public void Parse_WhenImplicitKeyStartsWithIndicator_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that an implicit key may begin with <c>-</c>, <c>?</c> or <c>:</c> followed by a character that could
    /// continue a plain scalar, and that a value indicator at the start of an entry is an empty key.
    /// </summary>
    /// <param name="yaml">A document with such keys.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("k: v\n-a: 1\n?a: 2\n:a: 3\n", """{"k":"v","-a":1,"?a":2,":a":3}""", DisplayName = "safe characters")]
    [DataRow("k: v\n: bar\n", """{"k":"v","":"bar"}""", DisplayName = "an empty key")]
    public void Parse_WhenImplicitKeyStartsWithIndicatorAndSafeCharacter_ShouldReadIt(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that a tab never counts as indentation in a plain scalar's continuation line: a line indented by too
    /// few spaces before a tab ends the scalar, and its tab is then rejected as indentation.
    /// </summary>
    /// <param name="yaml">A document whose plain scalar is followed by a tab-indented line.</param>
    [TestMethod]
    [DataRow(" - foo\n \t- bar\n", DisplayName = "a sequence entry after a space and a tab")]
    [DataRow("- a\n\tb\n", DisplayName = "a sequence entry's scalar")]
    [DataRow("a: b\n\tc\n", DisplayName = "a mapping value")]
    [DataRow("key:\n  a\n\tb\n", DisplayName = "a mapping value on the next line")]
    public void Parse_WhenTabIndentsPlainContinuation_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that a tab after a continuation line's indentation spaces is separation, so the line continues the
    /// plain scalar.
    /// </summary>
    /// <param name="yaml">A document whose plain scalar continues after spaces and a tab.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("- a\n \tb\n", """["a b"]""", DisplayName = "a sequence entry's scalar")]
    [DataRow("a: b\n \tc\n", """{"a":"b c"}""", DisplayName = "a mapping value")]
    public void Parse_WhenTabFollowsContinuationIndentation_ShouldContinueScalar(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }
}
