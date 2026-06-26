// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the BCL-aligned behaviors added to <see cref="YamlSerializerOptions" /> and the serializer: freeze-on-use,
/// number handling, unmapped-member handling, and duplicate wire-name detection.
/// </summary>
[TestClass]
public partial class YamlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that an options instance becomes read-only after it is used and rejects further mutation.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_WhenOptionsUsed_ShouldBecomeReadOnlyAndRejectMutation()
    {
        var options = new YamlSerializerOptions();

        _ = YamlSerializer.Serialize(1, options);

        Assert.IsTrue(options.IsReadOnly);
        Assert.ThrowsExactly<InvalidOperationException>(() => options.IncludeFields = true);
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializerOptions.MakeReadOnly" /> freezes the instance.
    /// </summary>
    [TestMethod]
    public void MakeReadOnly_ShouldFreezeOptions()
    {
        var options = new YamlSerializerOptions();
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() => options.SpecVersion = YamlSpecVersion.V1_1);
    }

    /// <summary>
    /// Verifies that the merge-key behavior configured on <see cref="YamlSerializerOptions" /> is applied during
    /// deserialization, retaining the literal <c>&lt;&lt;</c> key when merge handling is disabled.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMergeKeyDisabled_ShouldRetainLiteralKey()
    {
        var options = new YamlSerializerOptions { MergeKeyBehavior = YamlMergeKeyBehavior.Disabled };

        var value = YamlSerializer.Deserialize<Dictionary<string, object>>("base: &b\n  a: 1\nobj:\n  <<: *b\n", options)!;

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
        var value = YamlSerializer.Deserialize<Dictionary<string, object>>("base: &b\n  a: 1\nobj:\n  <<: *b\n")!;

        var obj = (Dictionary<string, object?>)value["obj"]!;
        Assert.IsFalse(obj.ContainsKey("<<"));
        Assert.AreEqual(1L, obj["a"]);
    }

    /// <summary>
    /// Verifies that <see cref="YamlNumberHandling.AllowFloatToInteger" /> truncates a fractional float into an integer
    /// target.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAllowFloatToInteger_ShouldTruncate()
    {
        var options = new YamlSerializerOptions { NumberHandling = YamlNumberHandling.AllowFloatToInteger };

        var value = YamlSerializer.Deserialize<int>("3.9\n", options);

        Assert.AreEqual(3, value);
    }

    /// <summary>
    /// Verifies that an unmapped key is ignored under the default <see cref="YamlUnmappedMemberHandling.Skip" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndSkip_ShouldIgnore()
    {
        var value = YamlSerializer.Deserialize<Point>("X: 1\nextra: 2\n");

        Assert.IsNotNull(value);
        Assert.AreEqual(1, value!.X);
    }

    /// <summary>
    /// Verifies that an unmapped key throws under <see cref="YamlUnmappedMemberHandling.Disallow" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndDisallow_ShouldThrow()
    {
        var options = new YamlSerializerOptions { UnmappedMemberHandling = YamlUnmappedMemberHandling.Disallow };

        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<Point>("X: 1\nextra: 2\n", options);
        });
    }

    /// <summary>
    /// Verifies that a type mapping two members to the same YAML key is rejected with
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDuplicateWireName_ShouldThrow()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = YamlSerializer.Serialize(new Collision());
        });
    }

    /// <summary>A simple target type with a single mapped member.</summary>
    private sealed class Point
    {
        /// <summary>Gets or sets the x coordinate.</summary>
        public int X { get; set; }
    }

    /// <summary>A type whose explicit names collide on a single YAML key.</summary>
    private sealed class Collision
    {
        /// <summary>Gets or sets the first colliding member.</summary>
        [YamlPropertyName("k")]
        public int A { get; set; }

        /// <summary>Gets or sets the second colliding member.</summary>
        [YamlPropertyName("k")]
        public int B { get; set; }
    }
}
