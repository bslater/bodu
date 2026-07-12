// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.PropertyOrder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="PropertyOrderAttribute" /> controls the order in which members are emitted, overriding
/// their declaration order.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that members carrying <see cref="PropertyOrderAttribute" /> are written in ascending order regardless
    /// of their declaration order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyOrderSpecified_ShouldEmitInAscendingOrder()
    {
        string yaml = YamlSerializer.Serialize(new OrderedModel { Third = 3, First = 1, Second = 2 });

        Assert.AreEqual("First: 1\nSecond: 2\nThird: 3\n", yaml);
    }

    /// <summary>
    /// Verifies that members without an explicit order take the default order of zero and otherwise keep declaration
    /// order, so an unordered member sorts ahead of a member with a positive order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenSomeMembersUnordered_ShouldPlaceDefaultsBeforePositiveOrders()
    {
        string yaml = YamlSerializer.Serialize(new PartiallyOrderedModel { Late = 9, Plain = 1 });

        Assert.AreEqual("Plain: 1\nLate: 9\n", yaml);
    }

    /// <summary>
    /// A model whose members carry explicit write orders that differ from their declaration order.
    /// </summary>
    private sealed class OrderedModel
    {
        /// <summary>
        /// Gets or sets the value written third.
        /// </summary>
        /// <value>The third value.</value>
        [PropertyOrder(3)]
        public int Third { get; set; }

        /// <summary>
        /// Gets or sets the value written first.
        /// </summary>
        /// <value>The first value.</value>
        [PropertyOrder(1)]
        public int First { get; set; }

        /// <summary>
        /// Gets or sets the value written second.
        /// </summary>
        /// <value>The second value.</value>
        [PropertyOrder(2)]
        public int Second { get; set; }
    }

    /// <summary>
    /// A model that mixes a member with a positive order and a member with the default order of zero.
    /// </summary>
    private sealed class PartiallyOrderedModel
    {
        /// <summary>
        /// Gets or sets the value written last.
        /// </summary>
        /// <value>The late value.</value>
        [PropertyOrder(5)]
        public int Late { get; set; }

        /// <summary>
        /// Gets or sets the value written at the default order of zero.
        /// </summary>
        /// <value>The plain value.</value>
        public int Plain { get; set; }
    }
}
