// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeTests.UnsignedIntegers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies that the mutable node tree supports the full unsigned 64-bit integer range the reader accepts: values
/// above <see cref="long.MaxValue" /> parse, create, convert, compare, clone, and round-trip.
/// </summary>
public partial class BencodeNodeTests
{
    /// <summary>
    /// Verifies that parsing an integer above <see cref="long.MaxValue" /> yields a node readable through
    /// <c>GetValue&lt;ulong&gt;()</c> that round-trips to the same canonical bytes.
    /// </summary>
    [TestMethod]
    public void Parse_WhenIntegerExceedsInt64_ShouldRoundTripThroughUInt64()
    {
        byte[] data = "i9223372036854775808e"u8.ToArray();

        var node = BencodeNode.Parse(data);

        Assert.IsNotNull(node);
        Assert.AreEqual(9223372036854775808UL, node.GetValue<ulong>());
        Assert.AreEqual(9223372036854775808UL, (ulong)node);
        CollectionAssert.AreEqual(data, node.ToByteArray());
    }

    /// <summary>
    /// Verifies that requesting a narrower integer type from a value above <see cref="long.MaxValue" /> throws
    /// <see cref="InvalidOperationException" />, the node model's conversion-failure contract.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueExceedsInt64AndInt64Requested_ShouldThrowInvalidOperationException()
    {
        BencodeNode node = BencodeValue.Create(ulong.MaxValue);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node.GetValue<long>();
        });

        Assert.IsFalse(node.AsValue().TryGetValue(out long _));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.Create(ulong)" /> normalizes values within the signed range, so equal
    /// integers compare deeply equal regardless of which factory created them.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenSignedAndUnsignedFactoriesHoldSameValue_ShouldBeEqual()
    {
        Assert.IsTrue(BencodeNode.DeepEquals(BencodeValue.Create(42UL), BencodeValue.Create(42L)));
        Assert.IsTrue(BencodeNode.DeepEquals(BencodeValue.Create(ulong.MaxValue), BencodeValue.Create(ulong.MaxValue)));
        Assert.IsFalse(BencodeNode.DeepEquals(BencodeValue.Create(ulong.MaxValue), BencodeValue.Create(-1L)));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.DeepClone" /> preserves a value above <see cref="long.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenValueExceedsInt64_ShouldPreserveUnsignedValue()
    {
        BencodeNode original = BencodeValue.Create(18446744073709551615UL);

        BencodeNode clone = original.DeepClone();

        Assert.AreEqual(ulong.MaxValue, clone.GetValue<ulong>());
        Assert.IsTrue(BencodeNode.DeepEquals(original, clone));
        Assert.AreEqual("18446744073709551615", clone.AsValue().ToString());
    }

    /// <summary>
    /// Verifies that the implicit <see cref="ulong" /> conversion wraps the value as an integer node usable inside a
    /// tree that serializes canonically.
    /// </summary>
    [TestMethod]
    public void ImplicitOperator_WhenUInt64Assigned_ShouldCreateIntegerNode()
    {
        var root = new BencodeObject
        {
            ["size"] = 9223372036854775808UL,
        };

        Assert.AreEqual(BencodeValueKind.Integer, root["size"]!.GetValueKind());
        CollectionAssert.AreEqual("d4:sizei9223372036854775808ee"u8.ToArray(), root.ToByteArray());
    }
}
