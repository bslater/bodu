// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.PropertyVisibility.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies which property members the <see cref="TomlSerializer" /> surfaces and how it binds them: read/write
/// properties, init-only properties, get-only properties, properties with non-public accessors, and the
/// <see cref="IncludeAttribute" /> opt-in. It also pins a shape that differs from the most permissive
/// <see cref="System.Text.Json.JsonSerializer" /> configuration: a get-only scalar property is written but cannot be
/// set on read. Field serialization - opt-in through <see cref="TomlSerializerOptions.IncludeFields" /> or
/// <see cref="IncludeAttribute" /> - is covered by the <c>Fields</c> partial.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that a standard read/write property is both written and assigned on read, the baseline member shape.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenReadWriteProperty_ShouldRoundTrip()
    {
        var original = new ReadWriteModel { Value = 42 };

        string text = TomlSerializer.Serialize(original);
        Assert.AreEqual("Value = 42\n", text);

        ReadWriteModel roundTripped = TomlSerializer.Deserialize<ReadWriteModel>(text);
        Assert.AreEqual(42, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that an init-only property is written and assigned on read, binding through the init-only setter.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInitOnlyProperty_ShouldRoundTrip()
    {
        var original = new InitOnlyModel { Value = 7 };

        string text = TomlSerializer.Serialize(original);
        Assert.AreEqual("Value = 7\n", text);

        InitOnlyModel roundTripped = TomlSerializer.Deserialize<InitOnlyModel>(text);
        Assert.AreEqual(7, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that a get-only scalar property with a public getter is written to the output, since the getter is
    /// public.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGetOnlyScalarProperty_ShouldWriteMember()
    {
        string text = TomlSerializer.Serialize(new GetOnlyScalarModel(42));

        Assert.AreEqual("Value = 42\n", text);
    }

    /// <summary>
    /// Verifies that a get-only scalar property is not assigned on read because it has no setter, so the constructed
    /// instance keeps the value its parameterless constructor produced rather than the value in the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGetOnlyScalarProperty_ShouldNotAssignMember()
    {
        GetOnlyScalarModel model = TomlSerializer.Deserialize<GetOnlyScalarModel>("Value = 99\n");

        Assert.AreEqual(0, model.Value);
    }

    /// <summary>
    /// Verifies that a property with a public getter and a private setter is written on serialize, because the member
    /// is surfaced for writing whenever its getter is public; the non-public setter governs whether the member is
    /// assigned on read, not whether it is written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPrivateSetterProperty_ShouldWriteMember()
    {
        string text = TomlSerializer.Serialize(new PrivateSetterModel(5));

        Assert.AreEqual("Value = 5\n", text);
    }

    /// <summary>
    /// Verifies that a property with a public getter and a private setter that is not opted into serialization is not
    /// assigned on read, so the constructed instance keeps the value its parameterless constructor produced rather
    /// than the value in the input. This matches the documented <see cref="IncludeAttribute" /> rule that a
    /// non-public setter binds only when opted in.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPrivateSetterProperty_ShouldNotAssignMember()
    {
        PrivateSetterModel model = TomlSerializer.Deserialize<PrivateSetterModel>("Value = 99\n");

        Assert.AreEqual(0, model.Value);
    }

    /// <summary>
    /// Verifies that a property with a public getter and a private setter annotated with
    /// <see cref="IncludeAttribute" /> is both written and assigned on read, binding through the non-public
    /// setter.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPrivateSetterWithInclude_ShouldRoundTrip()
    {
        var original = new IncludedPrivateSetterModel(5);

        string text = TomlSerializer.Serialize(original);
        Assert.AreEqual("Value = 5\n", text);

        IncludedPrivateSetterModel roundTripped = TomlSerializer.Deserialize<IncludedPrivateSetterModel>(text);
        Assert.AreEqual(5, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that a property with no public accessor, opted in by <see cref="IncludeAttribute" />, is written and is
    /// assigned on read through its private accessors.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPrivatePropertyHasInclude_ShouldRoundTripIt()
    {
        string text = TomlSerializer.Serialize(new IncludedPrivatePropertyModel());
        Assert.AreEqual("MyProperty = true\n", text);

        IncludedPrivatePropertyModel model = TomlSerializer.Deserialize<IncludedPrivatePropertyModel>("MyProperty = false\n");
        Assert.IsFalse(model.ReadMyProperty());
    }

    /// <summary>
    /// Verifies that a private property declared by a base class and opted in by <see cref="IncludeAttribute" /> is
    /// written after the derived type's members and is assigned on read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBaseClassPrivatePropertyHasInclude_ShouldRoundTripIt()
    {
        string text = TomlSerializer.Serialize(new IncludedDerivedModel());
        Assert.AreEqual("Visible = 4\nHidden = 3\n", text);

        IncludedDerivedModel model = TomlSerializer.Deserialize<IncludedDerivedModel>("Visible = 1\nHidden = 2\n");
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
        string text = TomlSerializer.Serialize(new UnincludedPrivatePropertyModel());
        Assert.AreEqual("Visible = 4\n", text);

        UnincludedPrivatePropertyModel model = TomlSerializer.Deserialize<UnincludedPrivatePropertyModel>("Visible = 1\nHidden = 2\n");
        Assert.AreEqual(1, model.Visible);
        Assert.AreEqual(3, model.ReadHidden());
    }

    /// <summary>
    /// A model with a single read/write property, the baseline member shape.
    /// </summary>
    private sealed class ReadWriteModel
    {
        /// <summary>
        /// Gets or sets the integer value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }
    }

    /// <summary>
    /// A model with a single init-only property.
    /// </summary>
    private sealed class InitOnlyModel
    {
        /// <summary>
        /// Gets the integer value, assignable only through an object initializer or the init-only setter.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; init; }
    }

    /// <summary>
    /// A model whose scalar property exposes a public getter but no setter, set only through the constructor.
    /// </summary>
    private sealed class GetOnlyScalarModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetOnlyScalarModel" /> class with a default value.
        /// </summary>
        public GetOnlyScalarModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetOnlyScalarModel" /> class with the specified value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        public GetOnlyScalarModel(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the integer value, exposed through a public getter with no setter.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; }
    }

    /// <summary>
    /// A model whose property has a public getter and a private setter and is not opted into serialization.
    /// </summary>
    private sealed class PrivateSetterModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateSetterModel" /> class.
        /// </summary>
        public PrivateSetterModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateSetterModel" /> class with the specified value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        public PrivateSetterModel(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the integer value, exposed through a public getter and a private setter.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; private set; }
    }

    /// <summary>
    /// A model whose private-setter property is opted into serialization through <see cref="IncludeAttribute" />.
    /// </summary>
    private sealed class IncludedPrivateSetterModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludedPrivateSetterModel" /> class.
        /// </summary>
        public IncludedPrivateSetterModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludedPrivateSetterModel" /> class with the specified value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        public IncludedPrivateSetterModel(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the integer value, opted into serialization through its private setter by
        /// <see cref="IncludeAttribute" />.
        /// </summary>
        /// <value>The value.</value>
        [Include]
        public int Value { get; private set; }
    }

    /// <summary>
    /// A model whose only member is a private property opted into serialization through <see cref="IncludeAttribute" />.
    /// </summary>
    private sealed class IncludedPrivatePropertyModel
    {
        /// <summary>
        /// Gets or sets a value indicating whether the flag is set, through private accessors only.
        /// </summary>
        /// <value>The flag; <see langword="true" /> until it is read.</value>
        [Include]
        private bool MyProperty { get; set; } = true;

        /// <summary>
        /// Returns the value of the private property.
        /// </summary>
        /// <returns>The value of <see cref="MyProperty" />.</returns>
        public bool ReadMyProperty() =>
            MyProperty;
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
