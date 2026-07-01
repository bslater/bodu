// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.MergeKeyBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="YamlSerializerOptions.MergeKeyBehavior" /> is applied during deserialization.
/// </summary>
public partial class YamlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that the merge-key behavior configured on <see cref="YamlSerializerOptions" /> is applied during
    /// deserialization, retaining the literal <c>&lt;&lt;</c> key when merge handling is disabled.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMergeKeyDisabled_ShouldRetainLiteralKey()
    {
        var options = new YamlSerializerOptions { MergeKeyBehavior = YamlMergeKeyBehavior.Disabled };

        Dictionary<string, object> value = YamlSerializer.Deserialize<Dictionary<string, object>>("base: &b\n  a: 1\nobj:\n  <<: *b\n", options)!;

        var obj = (Dictionary<string, object?>)value["obj"]!;
        Assert.IsTrue(obj.ContainsKey("<<"));
    }

    /// <summary>
    /// Verifies that the default merge-key behavior on <see cref="YamlSerializerOptions" /> expands the merge during
    /// deserialization.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMergeKeyDefault_ShouldExpand()
    {
        Dictionary<string, object> value = YamlSerializer.Deserialize<Dictionary<string, object>>("base: &b\n  a: 1\nobj:\n  <<: *b\n")!;

        var obj = (Dictionary<string, object?>)value["obj"]!;
        Assert.IsFalse(obj.ContainsKey("<<"));
        Assert.AreEqual(1L, obj["a"]);
    }
}
