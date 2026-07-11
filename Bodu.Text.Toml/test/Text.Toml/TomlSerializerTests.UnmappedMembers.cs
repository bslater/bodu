// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.UnmappedMembers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Serialization;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies how <see cref="TomlSerializer" /> treats a table key that maps to no member: the default
/// <see cref="UnmappedMemberHandling.Skip" />, the serializer-wide
/// <see cref="UnmappedMemberHandling.Disallow" /> on <see cref="TomlSerializerOptions" />, and the per-type
/// <see cref="UnmappedMemberHandlingAttribute" /> that overrides the options-level default.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that an unmapped key is silently skipped by default, so a document carrying extra keys still binds the
    /// matched members.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndDefaultHandling_ShouldSkip()
    {
        PlainNameModel model = TomlSerializer.Deserialize<PlainNameModel>("Name = \"n\"\nunknown = 1\n");

        Assert.AreEqual("n", model.Name);
    }

    /// <summary>
    /// Verifies that an unmapped key throws <see cref="TomlSerializationException" /> when the serializer-wide
    /// handling is <see cref="UnmappedMemberHandling.Disallow" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndOptionsDisallow_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<PlainNameModel>("Name = \"n\"\nunknown = 1\n", options);
        });

        Assert.IsTrue(ex.Message.Contains("unknown", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a document whose every key maps to a member does not throw under
    /// <see cref="UnmappedMemberHandling.Disallow" />, confirming the policy fires only on genuinely unmapped
    /// keys.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAllKeysMappedAndOptionsDisallow_ShouldNotThrow()
    {
        var options = new TomlSerializerOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };

        PlainNameModel model = TomlSerializer.Deserialize<PlainNameModel>("Name = \"n\"\n", options);

        Assert.AreEqual("n", model.Name);
    }

    /// <summary>
    /// Verifies that a type annotated with <see cref="UnmappedMemberHandlingAttribute" /> set to
    /// <see cref="UnmappedMemberHandling.Disallow" /> throws on an unmapped key even when the options leave the
    /// default <see cref="UnmappedMemberHandling.Skip" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAttributeDisallowsAndOptionsDefault_ShouldThrowTomlSerializationException()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<DisallowAttributeModel>("Name = \"n\"\nunknown = 1\n");
        });

        Assert.IsTrue(ex.Message.Contains("unknown", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a type annotated with <see cref="UnmappedMemberHandlingAttribute" /> set to
    /// <see cref="UnmappedMemberHandling.Skip" /> skips an unmapped key even when the options set the
    /// serializer-wide default to <see cref="UnmappedMemberHandling.Disallow" />, so the attribute overrides the
    /// options.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAttributeSkipsAndOptionsDisallow_ShouldSkip()
    {
        var options = new TomlSerializerOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };

        SkipAttributeModel model = TomlSerializer.Deserialize<SkipAttributeModel>("Name = \"n\"\nunknown = 1\n", options);

        Assert.AreEqual("n", model.Name);
    }

    /// <summary>
    /// Verifies that the exception message for an unmapped key reports both the offending key and the target type, so
    /// the failure is diagnosable.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyDisallowed_ShouldReportKeyAndTypeInMessage()
    {
        var options = new TomlSerializerOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<PlainNameModel>("unknown = 1\n", options);
        });

        Assert.IsTrue(ex.Message.Contains("unknown", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains(nameof(PlainNameModel), StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that constructing <see cref="UnmappedMemberHandlingAttribute" /> with an undefined
    /// <see cref="UnmappedMemberHandling" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>unmappedMemberHandling</c>.
    /// </summary>
    [TestMethod]
    public void UnmappedMemberHandlingAttribute_WhenHandlingUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new UnmappedMemberHandlingAttribute((UnmappedMemberHandling)99);
        }, "unmappedMemberHandling");
    }

    /// <summary>
    /// A model with a single member and no special unmapped-member handling.
    /// </summary>
    private sealed class PlainNameModel
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model whose type-level attribute disallows unmapped keys.
    /// </summary>
    [UnmappedMemberHandling(UnmappedMemberHandling.Disallow)]
    private sealed class DisallowAttributeModel
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model whose type-level attribute skips unmapped keys, overriding an options-level
    /// <see cref="UnmappedMemberHandling.Disallow" />.
    /// </summary>
    [UnmappedMemberHandling(UnmappedMemberHandling.Skip)]
    private sealed class SkipAttributeModel
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;
    }
}
