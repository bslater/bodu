// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.PropertyOrder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies how <see cref="Serialization.BencodePropertyOrderAttribute" /> interacts with the
/// <see cref="BencodeSerializer" />. Unlike <see cref="System.Text.Json.JsonSerializer" />, whose output preserves the
/// configured property order, a Bencode dictionary is always emitted in canonical ascending bytewise key order, so the
/// attribute governs only the sequence in which members are presented to the writer and has no observable effect on the
/// serialized bytes. These tests pin that Bencode-specific contract.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that the serialized bytes of a model carrying <see cref="Serialization.BencodePropertyOrderAttribute" />
    /// are identical to the bytes of an otherwise equal model without the attribute, proving the attribute does not
    /// influence the on-the-wire order, which the writer always sorts to canonical ascending key order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyOrderAttributePresent_ShouldEmitSameCanonicalBytesAsWithout()
    {
        byte[] ordered = BencodeSerializer.Serialize(new OrderedTrioModel { Alpha = 1, Bravo = 2, Charlie = 3 });
        byte[] unordered = BencodeSerializer.Serialize(new UnorderedTrioModel { Alpha = 1, Bravo = 2, Charlie = 3 });

        CollectionAssert.AreEqual(unordered, ordered);
    }

    /// <summary>
    /// Verifies that a model whose members carry out-of-sequence
    /// <see cref="Serialization.BencodePropertyOrderAttribute" /> values still serializes its keys in canonical
    /// ascending bytewise order rather than in the attribute-specified order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyOrderReversesDeclaredKeys_ShouldStillEmitCanonicalKeyOrder()
    {
        // Alpha is ordered last (10) and Charlie first (-10), the reverse of canonical key order; output must ignore it.
        byte[] bytes = BencodeSerializer.Serialize(new OrderedTrioModel { Alpha = 1, Bravo = 2, Charlie = 3 });

        Assert.AreEqual("d5:Alphai1e5:Bravoi2e7:Charliei3ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a model whose members carry <see cref="Serialization.BencodePropertyOrderAttribute" /> round-trips,
    /// confirming the attribute affects neither the written bytes nor the ability to read them back.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPropertyOrderAttributePresent_ShouldRoundTrip()
    {
        var original = new OrderedTrioModel { Alpha = 1, Bravo = 2, Charlie = 3 };

        byte[] bytes = BencodeSerializer.Serialize(original);
        OrderedTrioModel roundTripped = BencodeSerializer.Deserialize<OrderedTrioModel>(bytes);

        Assert.AreEqual(1, roundTripped.Alpha);
        Assert.AreEqual(2, roundTripped.Bravo);
        Assert.AreEqual(3, roundTripped.Charlie);
    }

    /// <summary>
    /// A model whose three members carry <see cref="Serialization.BencodePropertyOrderAttribute" /> values that run
    /// counter to canonical key order, used to prove the attribute does not change the serialized bytes.
    /// </summary>
    private sealed class OrderedTrioModel
    {
        /// <summary>
        /// Gets or sets the value whose key sorts first but is ordered last.
        /// </summary>
        /// <returns>The value.</returns>
        [Serialization.BencodePropertyOrder(10)]
        public int Alpha { get; set; }

        /// <summary>
        /// Gets or sets the value whose key and order both sit in the middle.
        /// </summary>
        /// <returns>The value.</returns>
        [Serialization.BencodePropertyOrder(0)]
        public int Bravo { get; set; }

        /// <summary>
        /// Gets or sets the value whose key sorts last but is ordered first.
        /// </summary>
        /// <returns>The value.</returns>
        [Serialization.BencodePropertyOrder(-10)]
        public int Charlie { get; set; }
    }

    /// <summary>
    /// A model identical to <see cref="OrderedTrioModel" /> but without any
    /// <see cref="Serialization.BencodePropertyOrderAttribute" />, used as the baseline for byte-for-byte comparison.
    /// </summary>
    private sealed class UnorderedTrioModel
    {
        /// <summary>
        /// Gets or sets the value whose key sorts first.
        /// </summary>
        /// <returns>The value.</returns>
        public int Alpha { get; set; }

        /// <summary>
        /// Gets or sets the value whose key sorts in the middle.
        /// </summary>
        /// <returns>The value.</returns>
        public int Bravo { get; set; }

        /// <summary>
        /// Gets or sets the value whose key sorts last.
        /// </summary>
        /// <returns>The value.</returns>
        public int Charlie { get; set; }
    }
}
