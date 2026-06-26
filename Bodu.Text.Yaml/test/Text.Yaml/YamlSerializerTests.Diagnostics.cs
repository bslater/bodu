// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Diagnostics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="YamlSerializer" /> binding failures carry the dotted member path to the offending value,
/// so deserialization diagnostics are actionable for configuration documents.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that a conversion failure on a nested member reports the dotted path to that member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNestedMemberConversionFails_ShouldReportMemberPath()
    {
        var ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
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
        var ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<List<int>>("[1, x, 3]\n");
        });

        Assert.AreEqual("[1]", ex.Path);
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
}
