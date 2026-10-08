// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Include.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="IncludeAttribute" /> forces a member to participate: a property with a non-public setter
/// or with no public accessor at all is bound, and a public field is surfaced even when
/// <see cref="YamlSerializerOptions.IncludeFields" /> is disabled.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that a property with a non-public setter marked <c>[Include]</c> is both written and read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenIncludeOnNonPublicSetter_ShouldRoundTrip()
    {
        var model = new IncludeSetterModel(5);

        string yaml = YamlSerializer.Serialize(model);
        Assert.AreEqual("Total: 5\n", yaml);

        IncludeSetterModel back = YamlSerializer.Deserialize<IncludeSetterModel>("Total: 9\n")!;
        Assert.AreEqual(9, back.Total);
    }

    /// <summary>
    /// Verifies that a public field marked <c>[Include]</c> is surfaced even though
    /// <see cref="YamlSerializerOptions.IncludeFields" /> is disabled.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenIncludeOnFieldAndFieldsDisabled_ShouldRoundTrip()
    {
        var model = new IncludeFieldModel { Retries = 3 };

        string yaml = YamlSerializer.Serialize(model);
        Assert.AreEqual("Retries: 3\n", yaml);

        IncludeFieldModel back = YamlSerializer.Deserialize<IncludeFieldModel>("Retries: 8\n")!;
        Assert.AreEqual(8, back.Retries);
    }

    /// <summary>
    /// Verifies that a property with no public accessor, opted in by <see cref="IncludeAttribute" />, is written and is
    /// assigned on read through its private accessors.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPrivatePropertyHasInclude_ShouldRoundTripIt()
    {
        string yaml = YamlSerializer.Serialize(new IncludedPrivatePropertyModel());
        Assert.AreEqual("Count: 3\n", yaml);

        IncludedPrivatePropertyModel model = YamlSerializer.Deserialize<IncludedPrivatePropertyModel>("Count: 8\n")!;
        Assert.AreEqual(8, model.ReadCount());
    }

    /// <summary>
    /// Verifies that a private property declared by a base class and opted in by <see cref="IncludeAttribute" /> is
    /// written after the derived type's members and is assigned on read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBaseClassPrivatePropertyHasInclude_ShouldRoundTripIt()
    {
        string yaml = YamlSerializer.Serialize(new IncludedDerivedModel());
        Assert.AreEqual("Visible: 4\nHidden: 3\n", yaml);

        IncludedDerivedModel model = YamlSerializer.Deserialize<IncludedDerivedModel>("Visible: 1\nHidden: 2\n")!;
        Assert.AreEqual(1, model.Visible);
        Assert.AreEqual(2, model.ReadHidden());
    }

    /// <summary>
    /// Verifies that a private property without <see cref="IncludeAttribute" /> is neither written nor assigned on
    /// read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPrivatePropertyHasNoInclude_ShouldIgnoreIt()
    {
        string yaml = YamlSerializer.Serialize(new UnincludedPrivatePropertyModel());
        Assert.AreEqual("Visible: 4\n", yaml);

        UnincludedPrivatePropertyModel model = YamlSerializer.Deserialize<UnincludedPrivatePropertyModel>("Visible: 1\nHidden: 2\n")!;
        Assert.AreEqual(1, model.Visible);
        Assert.AreEqual(3, model.ReadHidden());
    }

    /// <summary>A model whose property exposes only a non-public setter, opted in by <see cref="IncludeAttribute" />.</summary>
    private sealed class IncludeSetterModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeSetterModel" /> class.
        /// </summary>
        public IncludeSetterModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeSetterModel" /> class with a total.
        /// </summary>
        /// <param name="total">The total.</param>
        public IncludeSetterModel(int total)
        {
            Total = total;
        }

        /// <summary>
        /// Gets the total, assigned through a non-public setter.
        /// </summary>
        /// <value>The total.</value>
        [Include]
        public int Total { get; private set; }
    }

    /// <summary>A model whose public field is opted in by <see cref="IncludeAttribute" />.</summary>
    private sealed class IncludeFieldModel
    {
        /// <summary>The retry count, surfaced by <see cref="IncludeAttribute" /> despite fields being disabled.</summary>
        [Include]
        public int Retries;
    }

    /// <summary>
    /// A model whose only member is a private property opted into serialization through <see cref="IncludeAttribute" />.
    /// </summary>
    private sealed class IncludedPrivatePropertyModel
    {
        /// <summary>
        /// Gets or sets the count, through private accessors only.
        /// </summary>
        /// <value>The count; 3 until it is read.</value>
        [Include]
        private int Count { get; set; } = 3;

        /// <summary>
        /// Returns the value of the private property.
        /// </summary>
        /// <returns>The value of <see cref="Count" />.</returns>
        public int ReadCount() =>
            Count;
    }

    /// <summary>
    /// A base class whose private property is opted into serialization through <see cref="IncludeAttribute" />.
    /// </summary>
    private class IncludedBaseModel
    {
        /// <summary>
        /// Gets or sets the hidden value, through private accessors only.
        /// </summary>
        /// <value>The hidden value; 3 until it is read.</value>
        [Include]
        private int Hidden { get; set; } = 3;

        /// <summary>
        /// Returns the value of the private property.
        /// </summary>
        /// <returns>The value of <see cref="Hidden" />.</returns>
        public int ReadHidden() =>
            Hidden;
    }

    /// <summary>
    /// A model that adds a public property to <see cref="IncludedBaseModel" />.
    /// </summary>
    private sealed class IncludedDerivedModel
        : IncludedBaseModel
    {
        /// <summary>
        /// Gets or sets the visible value.
        /// </summary>
        /// <value>The visible value; 4 until it is read.</value>
        public int Visible { get; set; } = 4;
    }

    /// <summary>
    /// A model with a public property and a private property that is not opted into serialization.
    /// </summary>
    private sealed class UnincludedPrivatePropertyModel
    {
        /// <summary>
        /// Gets or sets the visible value.
        /// </summary>
        /// <value>The visible value; 4 until it is read.</value>
        public int Visible { get; set; } = 4;

        /// <summary>
        /// Gets or sets the hidden value, through private accessors only.
        /// </summary>
        /// <value>The hidden value; always 3, since the serializer does not bind it.</value>
        private int Hidden { get; set; } = 3;

        /// <summary>
        /// Returns the value of the private property.
        /// </summary>
        /// <returns>The value of <see cref="Hidden" />.</returns>
        public int ReadHidden() =>
            Hidden;
    }
}
