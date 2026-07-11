// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.PropertyVisibility.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using System.Text;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies which property members the <see cref="BencodeSerializer" /> surfaces and how it binds them: read/write
/// properties, init-only properties, get-only properties, properties with non-public accessors, and the
/// <see cref="IncludeAttribute" /> opt-in. It also pins a Bencode-specific shape that differs from the most
/// permissive <see cref="System.Text.Json.JsonSerializer" /> configuration: a get-only scalar property is written but
/// cannot be set on read. Field serialization — opt-in through
/// <see cref="BencodeSerializerOptions.IncludeFields" /> or <see cref="IncludeAttribute" /> — is covered by
/// the <c>Fields</c> partial.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that a standard read/write property is both written and assigned on read, the baseline member shape.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenReadWriteProperty_ShouldRoundTrip()
    {
        var original = new ReadWriteModel { Value = 42 };

        byte[] bytes = BencodeSerializer.Serialize(original);
        Assert.AreEqual("d5:Valuei42ee", Encoding.Latin1.GetString(bytes));

        ReadWriteModel roundTripped = BencodeSerializer.Deserialize<ReadWriteModel>(bytes);
        Assert.AreEqual(42, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that an init-only property is written and assigned on read, binding through the init-only setter.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInitOnlyProperty_ShouldRoundTrip()
    {
        var original = new InitOnlyModel { Value = 7 };

        byte[] bytes = BencodeSerializer.Serialize(original);
        Assert.AreEqual("d5:Valuei7ee", Encoding.Latin1.GetString(bytes));

        InitOnlyModel roundTripped = BencodeSerializer.Deserialize<InitOnlyModel>(bytes);
        Assert.AreEqual(7, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that a get-only scalar property with a public getter is written to the output, since the getter is
    /// public.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGetOnlyScalarProperty_ShouldWriteMember()
    {
        byte[] bytes = BencodeSerializer.Serialize(new GetOnlyScalarModel(42));

        Assert.AreEqual("d5:Valuei42ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a get-only scalar property is not assigned on read because it has no setter, so the constructed
    /// instance keeps the value its parameterless constructor produced rather than the value in the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGetOnlyScalarProperty_ShouldNotAssignMember()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei99ee");

        GetOnlyScalarModel model = BencodeSerializer.Deserialize<GetOnlyScalarModel>(bytes);

        Assert.AreEqual(0, model.Value);
    }

    /// <summary>
    /// Verifies that a property with a public getter and a private setter is written on serialize, because the member is
    /// surfaced for writing whenever its getter is public; the non-public setter governs whether the member is assigned
    /// on read, not whether it is written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPrivateSetterProperty_ShouldWriteMember()
    {
        byte[] bytes = BencodeSerializer.Serialize(new PrivateSetterModel(5));

        Assert.AreEqual("d5:Valuei5ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a property with a public getter and a private setter that is not opted into serialization is not
    /// assigned on read, so the constructed instance keeps the value its parameterless constructor produced rather than
    /// the value in the input. This matches the documented <see cref="IncludeAttribute" /> rule that a non-public
    /// setter binds only when opted in.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPrivateSetterProperty_ShouldNotAssignMember()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei99ee");

        PrivateSetterModel model = BencodeSerializer.Deserialize<PrivateSetterModel>(bytes);

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

        byte[] bytes = BencodeSerializer.Serialize(original);
        Assert.AreEqual("d5:Valuei5ee", Encoding.Latin1.GetString(bytes));

        IncludedPrivateSetterModel roundTripped = BencodeSerializer.Deserialize<IncludedPrivateSetterModel>(bytes);
        Assert.AreEqual(5, roundTripped.Value);
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

}
