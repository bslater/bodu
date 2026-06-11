// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.MaxDepth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="BencodeSerializerOptions.MaxDepth" /> bounds the nesting the serializer accepts on both
/// sides of the round trip: a graph deeper than the limit throws when writing and when reading, a graph within the limit
/// succeeds, and the property itself rejects a negative value and treats zero as the default.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that serializing a POCO graph nested deeper than <see cref="BencodeSerializerOptions.MaxDepth" /> throws
    /// <see cref="BencodeSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphExceedsMaxDepth_ShouldThrowBencodeSerializationException()
    {
        var options = new BencodeSerializerOptions { MaxDepth = 2 };
        var deep = new RecursiveModel { Child = new RecursiveModel { Child = new RecursiveModel() } };

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(deep, options);
        });
    }

    /// <summary>
    /// Verifies that serializing a POCO graph nested no deeper than <see cref="BencodeSerializerOptions.MaxDepth" />
    /// succeeds and emits canonical bytes.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphWithinMaxDepth_ShouldSucceed()
    {
        var options = new BencodeSerializerOptions { MaxDepth = 2 };
        var shallow = new RecursiveModel { Child = new RecursiveModel() };

        byte[] bytes = BencodeSerializer.Serialize(shallow, options);

        Assert.AreEqual("d5:Childdee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that deserializing Bencode nested deeper than <see cref="BencodeSerializerOptions.MaxDepth" /> throws
    /// <see cref="BencodeFormatException" />, surfaced from the reader.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenInputExceedsMaxDepth_ShouldThrowBencodeFormatException()
    {
        var options = new BencodeSerializerOptions { MaxDepth = 2 };
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Childd5:Childdeeee");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RecursiveModel>(bytes, options);
        });
    }

    /// <summary>
    /// Verifies that deserializing Bencode nested no deeper than <see cref="BencodeSerializerOptions.MaxDepth" />
    /// succeeds.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenInputWithinMaxDepth_ShouldSucceed()
    {
        var options = new BencodeSerializerOptions { MaxDepth = 2 };
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Childdee");

        var model = BencodeSerializer.Deserialize<RecursiveModel>(bytes, options);

        Assert.IsNotNull(model.Child);
        Assert.IsNull(model.Child.Child);
    }

    /// <summary>
    /// Verifies that setting <see cref="BencodeSerializerOptions.MaxDepth" /> to a negative value throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetToNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new BencodeSerializerOptions();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            options.MaxDepth = -1;
        });
    }

    /// <summary>
    /// Verifies that setting <see cref="BencodeSerializerOptions.MaxDepth" /> to zero resets it to
    /// <see cref="BencodeSerializerOptions.DefaultMaxDepth" /> rather than disabling depth tracking.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetToZero_ShouldResetToDefault()
    {
        var options = new BencodeSerializerOptions { MaxDepth = 0 };

        Assert.AreEqual(BencodeSerializerOptions.DefaultMaxDepth, options.MaxDepth);
    }

    /// <summary>
    /// Verifies that setting <see cref="BencodeSerializerOptions.MaxDepth" /> after the options have been used to
    /// serialize throws <see cref="InvalidOperationException" />, because the options are then read-only.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetAfterOptionsReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new BencodeSerializerOptions();
        _ = BencodeSerializer.Serialize(new RecursiveModel(), options);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.MaxDepth = 8;
        });
    }

    /// <summary>
    /// A self-referential model used to build graphs of arbitrary nesting depth.
    /// </summary>
    private sealed class RecursiveModel
    {
        /// <summary>
        /// Gets or sets the nested child, omitted from the output when <see langword="null" />.
        /// </summary>
        /// <returns>The child, or <see langword="null" />.</returns>
        public RecursiveModel? Child { get; set; }
    }
}
