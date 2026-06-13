// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.DepthCap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that the serializer enforces an absolute nesting ceiling that bounds resource use even when the caller
/// configures an arbitrarily large <see cref="TomlSerializerOptions.MaxDepth" />, so untrusted input cannot drive the
/// writer into a process-terminating <see cref="StackOverflowException" />.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that serializing an object graph nested beyond the absolute depth ceiling throws
    /// <see cref="TomlSerializationException" /> even when the configured maximum depth is far larger.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphExceedsAbsoluteCapDespiteLargeMaxDepth_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        RecursiveModel deep = new();
        for (var i = 0; i < TomlLimits.AbsoluteMaxDepth + 2; i++)
            deep = new RecursiveModel { Child = deep };

        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(deep, options);
        });
    }

    /// <summary>
    /// Verifies that a <see cref="TomlSerializerOptions.MaxDepth" /> larger than the absolute ceiling is still accepted
    /// by the property, confirming the ceiling is enforced while parsing or writing rather than at configuration time.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetAboveAbsoluteCap_ShouldBeAccepted()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        Assert.AreEqual(int.MaxValue, options.MaxDepth);
    }
}
