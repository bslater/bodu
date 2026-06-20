// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.PropertyOrder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="TomlPropertyOrderAttribute" /> governs the on-the-wire sequence of a table's key/value
/// lines. Unlike Bodu's Bencode serializer, whose canonical key sorting overrides any configured order, the TOML
/// writer preserves the order in which members are presented, so the attribute has a real, observable effect on the
/// serialized text: members are written in ascending order value, with declaration order breaking ties.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that members without <see cref="TomlPropertyOrderAttribute" /> are written in declaration order, the
    /// baseline the attribute reorders.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNoOrderAttributes_ShouldEmitDeclarationOrder()
    {
        string text = TomlSerializer.Serialize(new UnorderedTrioModel { Alpha = 1, Bravo = 2, Charlie = 3 });

        Assert.AreEqual("Alpha = 1\nBravo = 2\nCharlie = 3\n", text);
    }

    /// <summary>
    /// Verifies that members whose <see cref="TomlPropertyOrderAttribute" /> values reverse the declaration order are
    /// emitted in ascending attribute order, proving the attribute reorders the serialized key/value lines.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyOrderReversesDeclaration_ShouldEmitAttributeOrder()
    {
        // Alpha is declared first but ordered last (10); Charlie is declared last but ordered first (-10).
        string text = TomlSerializer.Serialize(new OrderedTrioModel { Alpha = 1, Bravo = 2, Charlie = 3 });

        Assert.AreEqual("Charlie = 3\nBravo = 2\nAlpha = 1\n", text);
    }

    /// <summary>
    /// Verifies that a member with a negative order value is written before its siblings that keep the default order
    /// of zero, mirroring the System.Text.Json before-default-order contract.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNegativeOrderOnLastDeclaredMember_ShouldEmitMemberFirst()
    {
        string text = TomlSerializer.Serialize(new BeforeDefaultOrderModel { First = 1, Second = 2, Promoted = 3 });

        Assert.AreEqual("Promoted = 3\nFirst = 1\nSecond = 2\n", text);
    }

    /// <summary>
    /// Verifies that a member with a positive order value is written after its siblings that keep the default order of
    /// zero, mirroring the System.Text.Json after-default-order contract.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPositiveOrderOnFirstDeclaredMember_ShouldEmitMemberLast()
    {
        string text = TomlSerializer.Serialize(new AfterDefaultOrderModel { Demoted = 1, First = 2, Second = 3 });

        Assert.AreEqual("First = 2\nSecond = 3\nDemoted = 1\n", text);
    }

    /// <summary>
    /// Verifies that members sharing the same order value keep their relative declaration order, so the order value
    /// partitions the members and declaration order breaks ties within a partition.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenOrderValuesTie_ShouldBreakTiesByDeclarationOrder()
    {
        string text = TomlSerializer.Serialize(new TiedOrderModel { A = 1, B = 2, C = 3, D = 4 });

        // A and C share order 1 and keep declaration order; B and D share the default order 0 and precede them.
        Assert.AreEqual("B = 2\nD = 4\nA = 1\nC = 3\n", text);
    }

    /// <summary>
    /// Verifies that a model whose members carry <see cref="TomlPropertyOrderAttribute" /> round-trips, confirming the
    /// reordered output is read back into the correct members.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPropertyOrderAttributePresent_ShouldRoundTrip()
    {
        var original = new OrderedTrioModel { Alpha = 1, Bravo = 2, Charlie = 3 };

        string text = TomlSerializer.Serialize(original);
        OrderedTrioModel roundTripped = TomlSerializer.Deserialize<OrderedTrioModel>(text);

        Assert.AreEqual(1, roundTripped.Alpha);
        Assert.AreEqual(2, roundTripped.Bravo);
        Assert.AreEqual(3, roundTripped.Charlie);
    }

    /// <summary>
    /// A model whose three members carry <see cref="TomlPropertyOrderAttribute" /> values that reverse the declaration
    /// order, used to prove the attribute reorders the serialized lines.
    /// </summary>
    private sealed class OrderedTrioModel
    {
        /// <summary>
        /// Gets or sets the value declared first but ordered last.
        /// </summary>
        /// <returns>The value.</returns>
        [TomlPropertyOrder(10)]
        public int Alpha { get; set; }

        /// <summary>
        /// Gets or sets the value declared and ordered in the middle.
        /// </summary>
        /// <returns>The value.</returns>
        [TomlPropertyOrder(0)]
        public int Bravo { get; set; }

        /// <summary>
        /// Gets or sets the value declared last but ordered first.
        /// </summary>
        /// <returns>The value.</returns>
        [TomlPropertyOrder(-10)]
        public int Charlie { get; set; }
    }

    /// <summary>
    /// A model identical to <see cref="OrderedTrioModel" /> but without any <see cref="TomlPropertyOrderAttribute" />,
    /// used to pin the declaration-order baseline.
    /// </summary>
    private sealed class UnorderedTrioModel
    {
        /// <summary>
        /// Gets or sets the first declared value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Alpha { get; set; }

        /// <summary>
        /// Gets or sets the second declared value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Bravo { get; set; }

        /// <summary>
        /// Gets or sets the third declared value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Charlie { get; set; }
    }

    /// <summary>
    /// A model whose last declared member carries a negative order value, promoting it ahead of the default-ordered
    /// members.
    /// </summary>
    private sealed class BeforeDefaultOrderModel
    {
        /// <summary>
        /// Gets or sets the first default-ordered value.
        /// </summary>
        /// <returns>The value.</returns>
        public int First { get; set; }

        /// <summary>
        /// Gets or sets the second default-ordered value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Second { get; set; }

        /// <summary>
        /// Gets or sets the value promoted ahead of its siblings by a negative order.
        /// </summary>
        /// <returns>The value.</returns>
        [TomlPropertyOrder(-1)]
        public int Promoted { get; set; }
    }

    /// <summary>
    /// A model whose first declared member carries a positive order value, demoting it behind the default-ordered
    /// members.
    /// </summary>
    private sealed class AfterDefaultOrderModel
    {
        /// <summary>
        /// Gets or sets the value demoted behind its siblings by a positive order.
        /// </summary>
        /// <returns>The value.</returns>
        [TomlPropertyOrder(1)]
        public int Demoted { get; set; }

        /// <summary>
        /// Gets or sets the first default-ordered value.
        /// </summary>
        /// <returns>The value.</returns>
        public int First { get; set; }

        /// <summary>
        /// Gets or sets the second default-ordered value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Second { get; set; }
    }

    /// <summary>
    /// A model with two members ordered 1 and two members at the default order 0, used to confirm declaration order
    /// breaks ties.
    /// </summary>
    private sealed class TiedOrderModel
    {
        /// <summary>
        /// Gets or sets the first member of the order-1 partition.
        /// </summary>
        /// <returns>The value.</returns>
        [TomlPropertyOrder(1)]
        public int A { get; set; }

        /// <summary>
        /// Gets or sets the first member of the default partition.
        /// </summary>
        /// <returns>The value.</returns>
        public int B { get; set; }

        /// <summary>
        /// Gets or sets the second member of the order-1 partition.
        /// </summary>
        /// <returns>The value.</returns>
        [TomlPropertyOrder(1)]
        public int C { get; set; }

        /// <summary>
        /// Gets or sets the second member of the default partition.
        /// </summary>
        /// <returns>The value.</returns>
        public int D { get; set; }
    }
}
