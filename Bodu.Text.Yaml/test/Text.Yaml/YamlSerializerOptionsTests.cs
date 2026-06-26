// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Nodes;
using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the BCL-aligned behaviors added to <see cref="YamlSerializerOptions" /> and the serializer: freeze-on-use,
/// number handling, unmapped-member handling, and duplicate wire-name detection.
/// </summary>
[TestClass]
public sealed class YamlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that an options instance becomes read-only after it is used and rejects further mutation.
    /// </summary>
    [TestMethod]
    public void Options_WhenUsed_ShouldBecomeReadOnlyAndRejectMutation()
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

    /// <summary>
    /// Verifies that <see cref="YamlValue.GetValue{T}" /> wraps a failed conversion in an
    /// <see cref="InvalidOperationException" /> that carries the original cause.
    /// </summary>
    [TestMethod]
    public void YamlValueGetValue_WhenConversionFails_ShouldThrowWithInnerException()
    {
        var value = YamlValue.Create("not-a-number");

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => value.GetValue<int>());
        Assert.IsNotNull(ex.InnerException);
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
