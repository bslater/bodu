// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Diagnostics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="YamlSerializer" /> binding failures carry the dotted member path to the offending value and
/// its line and column, so deserialization diagnostics are actionable for configuration documents.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that a conversion failure on a nested member reports the dotted path to that member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNestedMemberConversionFails_ShouldReportMemberPath()
    {
        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<Outer>("Inner:\n  Port: not-a-number\n");
        });

        Assert.AreEqual("Inner.Port", ex.Path);
    }

    /// <summary>
    /// Verifies that a conversion failure on a sequence element reports the element index in the path.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSequenceElementConversionFails_ShouldReportIndexPath()
    {
        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<List<int>>("[1, x, 3]\n");
        });

        Assert.AreEqual("[1]", ex.Path);
    }

    /// <summary>
    /// Verifies that a scalar that cannot bind to its member, <c>x</c> for an <see cref="int" /> matched
    /// case-insensitively by the key <c>port</c>, is reported at the scalar, line 2, column 7, under the path of the key
    /// the document spells.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenValueCannotBind_ShouldReportLineAndColumn()
    {
        var options = new YamlSerializerOptions { PropertyNameCaseInsensitive = true };

        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<PortSettings>("a: 1\nport: x\n", options);
        });

        Assert.AreEqual(2, ex.LineNumber, "LineNumber");
        Assert.AreEqual(7, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual("port", ex.Path, "Path");
    }

    /// <summary>
    /// Verifies that a scalar that cannot bind is reported at the scalar, with its line, column and offset, and under
    /// the path of the keys and indices that lead to it, for a root member, a nested member and sequence elements.
    /// </summary>
    /// <param name="yaml">A document with a scalar that cannot bind to its member.</param>
    /// <param name="line">The line of the scalar.</param>
    /// <param name="column">The column of the scalar.</param>
    /// <param name="offset">The byte offset of the scalar.</param>
    /// <param name="path">The path expected.</param>
    [TestMethod]
    [DataRow("Port: x\n", 1, 7, 6, "Port", DisplayName = "a root member")]
    [DataRow("Inner:\n  Port: x\n", 2, 9, 15, "Inner.Port", DisplayName = "a nested member")]
    [DataRow("Ports: [1, x, 3]\n", 1, 12, 11, "Ports[1]", DisplayName = "a flow sequence element")]
    [DataRow("Ports:\n- 1\n- x\n", 3, 3, 13, "Ports[1]", DisplayName = "a block sequence element")]
    public void Deserialize_WhenScalarCannotBind_ShouldReportScalarPositionAndPath(
        string yaml,
        int line,
        int column,
        int offset,
        string path)
    {
        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<BindingTarget>(yaml);
        });

        Assert.AreEqual(line, ex.LineNumber, "LineNumber");
        Assert.AreEqual(column, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(offset, ex.Offset, "Offset");
        Assert.AreEqual(path, ex.Path, "Path");
    }

    /// <summary>
    /// Verifies that a root value that cannot bind, a scalar read as a sequence, is reported at the value, line 1,
    /// column 1.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRootValueCannotBind_ShouldReportValuePosition()
    {
        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<List<int>>("x\n");
        });

        Assert.AreEqual(1, ex.LineNumber, "LineNumber");
        Assert.AreEqual(1, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(0, ex.Offset, "Offset");
    }

    /// <summary>
    /// Verifies that the path of a binding failure names each key as the document spells it, not the member's name,
    /// when keys match members case-insensitively.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenKeysMatchMembersCaseInsensitively_ShouldReportKeysAsWrittenInPath()
    {
        var options = new YamlSerializerOptions { PropertyNameCaseInsensitive = true };

        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<BindingTarget>("inner:\n  port: x\n", options);
        });

        Assert.AreEqual("inner.port", ex.Path);
    }

    /// <summary>A target whose members bind a root scalar, a nested object and a sequence.</summary>
    private sealed class BindingTarget
    {
        /// <summary>Gets or sets the port number.</summary>
        public int Port { get; set; }

        /// <summary>Gets or sets the nested value.</summary>
        public Inner? Inner { get; set; }

        /// <summary>Gets or sets the port numbers.</summary>
        public List<int>? Ports { get; set; }
    }

    /// <summary>A nested target whose inner member exercises path-rich diagnostics.</summary>
    private sealed class Outer
    {
        /// <summary>Gets or sets the inner value.</summary>
        public Inner? Inner { get; set; }
    }

    /// <summary>A target with a single integer member used to force a conversion failure.</summary>
    private sealed class Inner
    {
        /// <summary>Gets or sets the port number.</summary>
        public int Port { get; set; }
    }

    /// <summary>A target with a single integer member, bound from a key whose case differs.</summary>
    private sealed class PortSettings
    {
        /// <summary>Gets or sets the port number.</summary>
        /// <value>The port number.</value>
        public int Port { get; set; }
    }
}
