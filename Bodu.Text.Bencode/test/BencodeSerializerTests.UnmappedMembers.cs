// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.UnmappedMembers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies how <see cref="BencodeSerializer" /> treats a dictionary key that maps to no member: the default
/// <see cref="BencodeUnmappedMemberHandling.Skip" />, the serializer-wide
/// <see cref="BencodeUnmappedMemberHandling.Disallow" /> on <see cref="BencodeSerializerOptions" />, and the
/// per-type <see cref="BencodeUnmappedMemberHandlingAttribute" /> that overrides the options-level default.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that an unmapped key is silently skipped by default, so a document carrying extra keys still binds the
    /// matched members.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndDefaultHandling_ShouldSkip()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:n7:unknowni1ee");

        var model = BencodeSerializer.Deserialize<PlainNameModel>(bytes);

        Assert.AreEqual("n", model.Name);
    }

    /// <summary>
    /// Verifies that an unmapped key throws <see cref="BencodeSerializationException" /> when the serializer-wide
    /// handling is <see cref="BencodeUnmappedMemberHandling.Disallow" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndOptionsDisallow_ShouldThrowBencodeSerializationException()
    {
        var options = new BencodeSerializerOptions { UnmappedMemberHandling = BencodeUnmappedMemberHandling.Disallow };
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:n7:unknowni1ee");

        var ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<PlainNameModel>(bytes, options);
        });

        Assert.IsTrue(ex.Message.Contains("unknown", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a document whose every key maps to a member does not throw under
    /// <see cref="BencodeUnmappedMemberHandling.Disallow" />, confirming the policy fires only on genuinely unmapped
    /// keys.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAllKeysMappedAndOptionsDisallow_ShouldNotThrow()
    {
        var options = new BencodeSerializerOptions { UnmappedMemberHandling = BencodeUnmappedMemberHandling.Disallow };
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:ne");

        var model = BencodeSerializer.Deserialize<PlainNameModel>(bytes, options);

        Assert.AreEqual("n", model.Name);
    }

    /// <summary>
    /// Verifies that a type annotated with <see cref="BencodeUnmappedMemberHandlingAttribute" /> set to
    /// <see cref="BencodeUnmappedMemberHandling.Disallow" /> throws on an unmapped key even when the options leave the
    /// default <see cref="BencodeUnmappedMemberHandling.Skip" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAttributeDisallowsAndOptionsDefault_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:n7:unknowni1ee");

        var ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<DisallowAttributeModel>(bytes);
        });

        Assert.IsTrue(ex.Message.Contains("unknown", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a type annotated with <see cref="BencodeUnmappedMemberHandlingAttribute" /> set to
    /// <see cref="BencodeUnmappedMemberHandling.Skip" /> skips an unmapped key even when the options set the
    /// serializer-wide default to <see cref="BencodeUnmappedMemberHandling.Disallow" />, so the attribute overrides the
    /// options.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAttributeSkipsAndOptionsDisallow_ShouldSkip()
    {
        var options = new BencodeSerializerOptions { UnmappedMemberHandling = BencodeUnmappedMemberHandling.Disallow };
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:n7:unknowni1ee");

        var model = BencodeSerializer.Deserialize<SkipAttributeModel>(bytes, options);

        Assert.AreEqual("n", model.Name);
    }

    /// <summary>
    /// Verifies that the exception message for an unmapped key reports both the offending key and the target type, so
    /// the failure is diagnosable.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyDisallowed_ShouldReportKeyAndTypeInMessage()
    {
        var options = new BencodeSerializerOptions { UnmappedMemberHandling = BencodeUnmappedMemberHandling.Disallow };
        byte[] bytes = Encoding.Latin1.GetBytes("d7:unknowni1ee");

        var ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<PlainNameModel>(bytes, options);
        });

        Assert.IsTrue(ex.Message.Contains("unknown", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains(nameof(PlainNameModel), StringComparison.Ordinal));
    }

    /// <summary>
    /// A model with a single member and no special unmapped-member handling.
    /// </summary>
    private sealed class PlainNameModel
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model whose type-level attribute disallows unmapped keys.
    /// </summary>
    [BencodeUnmappedMemberHandling(BencodeUnmappedMemberHandling.Disallow)]
    private sealed class DisallowAttributeModel
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model whose type-level attribute skips unmapped keys, overriding an options-level
    /// <see cref="BencodeUnmappedMemberHandling.Disallow" />.
    /// </summary>
    [BencodeUnmappedMemberHandling(BencodeUnmappedMemberHandling.Skip)]
    private sealed class SkipAttributeModel
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;
    }
}
