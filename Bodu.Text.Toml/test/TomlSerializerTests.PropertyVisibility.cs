// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.PropertyVisibility.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies which members the <see cref="TomlSerializer" /> surfaces and how it binds them: read/write properties,
/// init-only properties, get-only properties, properties with non-public accessors, and the
/// <see cref="TomlIncludeAttribute" /> opt-in. It also pins two shapes that differ from the most permissive
/// <see cref="System.Text.Json.JsonSerializer" /> configuration: a get-only scalar property is written but cannot be
/// set on read, and a public field is not surfaced as a member even with <see cref="TomlIncludeAttribute" />, because
/// the serializer maps only properties.
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

        var roundTripped = TomlSerializer.Deserialize<ReadWriteModel>(text);
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

        var roundTripped = TomlSerializer.Deserialize<InitOnlyModel>(text);
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
        var model = TomlSerializer.Deserialize<GetOnlyScalarModel>("Value = 99\n");

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
    /// than the value in the input. This matches the documented <see cref="TomlIncludeAttribute" /> rule that a
    /// non-public setter binds only when opted in.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPrivateSetterProperty_ShouldNotAssignMember()
    {
        var model = TomlSerializer.Deserialize<PrivateSetterModel>("Value = 99\n");

        Assert.AreEqual(0, model.Value);
    }

    /// <summary>
    /// Verifies that a property with a public getter and a private setter annotated with
    /// <see cref="TomlIncludeAttribute" /> is both written and assigned on read, binding through the non-public
    /// setter.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPrivateSetterWithInclude_ShouldRoundTrip()
    {
        var original = new IncludedPrivateSetterModel(5);

        string text = TomlSerializer.Serialize(original);
        Assert.AreEqual("Value = 5\n", text);

        var roundTripped = TomlSerializer.Deserialize<IncludedPrivateSetterModel>(text);
        Assert.AreEqual(5, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that a public field is not surfaced as a serializable member, so only the type's properties are
    /// written; this records the TOML serializer's property-only mapping, which is narrower than the field support of
    /// <see cref="System.Text.Json.JsonSerializer" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPublicField_ShouldNotWriteField()
    {
        string text = TomlSerializer.Serialize(new FieldAndPropertyModel { Field = 5, Property = 6 });

        Assert.AreEqual("Property = 6\n", text);
    }

    /// <summary>
    /// Verifies that a public field annotated with <see cref="TomlIncludeAttribute" /> is still not surfaced as a
    /// member, confirming the attribute opts non-public property accessors in but does not extend the mapping to
    /// fields.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenIncludedField_ShouldNotWriteField()
    {
        string text = TomlSerializer.Serialize(new IncludedFieldModel { Field = 5 });

        Assert.AreEqual(string.Empty, text);
    }

    /// <summary>
    /// A model with a single read/write property, the baseline member shape.
    /// </summary>
    private sealed class ReadWriteModel
    {
        /// <summary>
        /// Gets or sets the integer value.
        /// </summary>
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
        public int Value { get; private set; }
    }

    /// <summary>
    /// A model whose private-setter property is opted into serialization through <see cref="TomlIncludeAttribute" />.
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
        /// <see cref="TomlIncludeAttribute" />.
        /// </summary>
        /// <returns>The value.</returns>
        [TomlInclude]
        public int Value { get; private set; }
    }

    /// <summary>
    /// A model with both a public field and a public property, used to confirm only the property is mapped.
    /// </summary>
    private sealed class FieldAndPropertyModel
    {
        /// <summary>
        /// The public field, which the serializer does not surface as a member.
        /// </summary>
        public int Field;

        /// <summary>
        /// Gets or sets the public property, which the serializer surfaces as a member.
        /// </summary>
        /// <returns>The property value.</returns>
        public int Property { get; set; }
    }

    /// <summary>
    /// A model whose only candidate member is a public field annotated with <see cref="TomlIncludeAttribute" />, which
    /// the serializer still does not surface.
    /// </summary>
    private sealed class IncludedFieldModel
    {
        /// <summary>
        /// The public field annotated for inclusion, which the serializer nonetheless does not surface as a member.
        /// </summary>
        [TomlInclude]
        public int Field;
    }
}
