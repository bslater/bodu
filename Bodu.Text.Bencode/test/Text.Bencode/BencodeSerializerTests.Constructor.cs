// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Constructor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies how <see cref="BencodeSerializer" /> selects and invokes a constructor when deserializing: parameterless
/// versus parameterized construction, positional records, the <see cref="BencodeConstructorAttribute" /> override,
/// greatest-arity selection, and how constructor parameters match the read members by name, naming policy, and case.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that a type with only a parameterized constructor is constructed through that constructor, binding each
    /// argument from the matching read member by its CLR name.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSingleParameterizedConstructor_ShouldBindArgumentsByName()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d3:Agei30e4:Name5:Alicee");

        ImmutablePerson model = BencodeSerializer.Deserialize<ImmutablePerson>(bytes);

        Assert.AreEqual("Alice", model.Name);
        Assert.AreEqual(30, model.Age);
    }

    /// <summary>
    /// Verifies that a constructor parameter is matched to a member case-insensitively, so a lower-case parameter binds
    /// to a Pascal-case property.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConstructorParameterCaseDiffersFromProperty_ShouldBindByName()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei7ee");

        LowerParameter model = BencodeSerializer.Deserialize<LowerParameter>(bytes);

        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that a constructor parameter binds to the member it maps to even when that member carries a
    /// <see cref="BencodePropertyNameAttribute" />, so the value read under the wire key flows to the constructor.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConstructorParameterBindsRenamedMember_ShouldReadFromWireName()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d2:idi9ee");

        RenamedConstructorMember model = BencodeSerializer.Deserialize<RenamedConstructorMember>(bytes);

        Assert.AreEqual(9, model.Identifier);
    }

    /// <summary>
    /// Verifies that a constructor parameter binds through the wire name produced by a camel-case naming policy, so the
    /// argument is supplied from the camel-cased dictionary key.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNamingPolicyAndParameterizedConstructor_ShouldBindFromPolicyWireName()
    {
        var options = new BencodeSerializerOptions { PropertyNamingPolicy = BencodeNamingPolicy.CamelCase };
        byte[] bytes = Encoding.Latin1.GetBytes("d9:firstName5:Alicee");

        ImmutableName model = BencodeSerializer.Deserialize<ImmutableName>(bytes, options);

        Assert.AreEqual("Alice", model.FirstName);
    }

    /// <summary>
    /// Verifies that a positional record round-trips: its primary-constructor parameters are written as dictionary
    /// members and read back through the synthesized constructor.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPositionalRecord_ShouldRoundTrip()
    {
        var original = new PointRecord(4, 5);

        byte[] bytes = BencodeSerializer.Serialize(original);
        Assert.AreEqual("d1:Xi4e1:Yi5ee", Encoding.Latin1.GetString(bytes));

        PointRecord roundTripped = BencodeSerializer.Deserialize<PointRecord>(bytes);
        Assert.AreEqual(4, roundTripped.X);
        Assert.AreEqual(5, roundTripped.Y);
    }

    /// <summary>
    /// Verifies that a constructor marked with <see cref="BencodeConstructorAttribute" /> is selected even when a public
    /// parameterless constructor is also declared, so the attributed constructor receives the read members.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConstructorAttributePresent_ShouldUseAttributedConstructor()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d1:ai3ee");

        AttributedConstructorModel model = BencodeSerializer.Deserialize<AttributedConstructorModel>(bytes);

        Assert.AreEqual(3, model.A);
        Assert.AreEqual("attributed", model.SelectedConstructor);
    }

    /// <summary>
    /// Verifies that when a type declares a public parameterless constructor alongside parameterized ones and none is
    /// attributed, the parameterless constructor is preferred and the members are set through their setters.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenParameterlessConstructorAvailable_ShouldPreferParameterlessAndSetMembers()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d1:ai1e1:bi2ee");

        MixedConstructorModel model = BencodeSerializer.Deserialize<MixedConstructorModel>(bytes);

        Assert.AreEqual("parameterless", model.SelectedConstructor);
        Assert.AreEqual(1, model.A);
        Assert.AreEqual(2, model.B);
    }

    /// <summary>
    /// Verifies that when a type declares only parameterized constructors and none is attributed, the constructor with
    /// the greatest number of parameters is chosen.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMultipleParameterizedConstructors_ShouldUseGreatestArity()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d1:ai1e1:bi2ee");

        GreatestArityModel model = BencodeSerializer.Deserialize<GreatestArityModel>(bytes);

        Assert.AreEqual(2, model.ParameterCount);
        Assert.AreEqual(1, model.A);
        Assert.AreEqual(2, model.B);
    }

    /// <summary>
    /// Verifies that a constructor parameter absent from the input falls back to its declared default value rather than
    /// failing, leaving the corresponding member at that default.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOptionalConstructorParameterAbsent_ShouldUseParameterDefault()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name5:Alicee");

        OptionalParameterModel model = BencodeSerializer.Deserialize<OptionalParameterModel>(bytes);

        Assert.AreEqual("Alice", model.Name);
        Assert.AreEqual(99, model.Age);
    }

    /// <summary>
    /// Verifies that a member without a matching constructor parameter is set through its setter after the constructor
    /// runs, so a parameterized constructor and settable members compose.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemberNotBoundToConstructorParameter_ShouldSetThroughSetter()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name5:Alice5:Notes5:hello6:Statusi2ee");

        PartiallyBoundModel model = BencodeSerializer.Deserialize<PartiallyBoundModel>(bytes);

        Assert.AreEqual("Alice", model.Name);
        Assert.AreEqual("hello", model.Notes);
        Assert.AreEqual(2, model.Status);
    }

    /// <summary>
    /// Verifies that a record with a parameterized constructor whose parameter binds a renamed member round-trips
    /// through both the renamed wire key and the constructor.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenRecordParameterBindsRenamedMember_ShouldRoundTrip()
    {
        var original = new RenamedConstructorMember(11);

        byte[] bytes = BencodeSerializer.Serialize(original);
        Assert.AreEqual("d2:idi11ee", Encoding.Latin1.GetString(bytes));

        RenamedConstructorMember roundTripped = BencodeSerializer.Deserialize<RenamedConstructorMember>(bytes);
        Assert.AreEqual(11, roundTripped.Identifier);
    }

    /// <summary>
    /// An immutable type whose two read-only properties are populated only through its parameterized constructor.
    /// </summary>
    private sealed class ImmutablePerson
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutablePerson" /> class.
        /// </summary>
        /// <param name="name">The person's name.</param>
        /// <param name="age">The person's age.</param>
        public ImmutablePerson(string name, int age)
        {
            Name = name;
            Age = age;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; }

        /// <summary>
        /// Gets the age.
        /// </summary>
        /// <returns>The age.</returns>
        public int Age { get; }
    }

    /// <summary>
    /// A type whose constructor parameter is lower-case where the bound property is Pascal-case, exercising
    /// case-insensitive parameter-to-member matching.
    /// </summary>
    private sealed class LowerParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LowerParameter" /> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public LowerParameter(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Value { get; }
    }

    /// <summary>
    /// A type whose constructor parameter binds a property that is renamed on the wire by
    /// <see cref="BencodePropertyNameAttribute" />.
    /// </summary>
    private sealed class RenamedConstructorMember
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenamedConstructorMember" /> class.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        public RenamedConstructorMember(int identifier)
        {
            Identifier = identifier;
        }

        /// <summary>
        /// Gets the identifier, written under the wire name <c>id</c>.
        /// </summary>
        /// <returns>The identifier.</returns>
        [BencodePropertyName("id")]
        public int Identifier { get; }
    }

    /// <summary>
    /// An immutable type whose constructor parameter binds a property mapped through a naming policy.
    /// </summary>
    private sealed class ImmutableName
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableName" /> class.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        public ImmutableName(string firstName)
        {
            FirstName = firstName;
        }

        /// <summary>
        /// Gets the first name.
        /// </summary>
        /// <returns>The first name.</returns>
        public string FirstName { get; }
    }

    /// <summary>
    /// A positional record whose primary-constructor parameters surface as serializable members.
    /// </summary>
    /// <param name="X">The x coordinate.</param>
    /// <param name="Y">The y coordinate.</param>
    private sealed record PointRecord(int X, int Y);

    /// <summary>
    /// A type with both a parameterless and an attributed parameterized constructor, used to confirm the attributed one
    /// is selected.
    /// </summary>
    private sealed class AttributedConstructorModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributedConstructorModel" /> class.
        /// </summary>
        public AttributedConstructorModel()
        {
            SelectedConstructor = "parameterless";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributedConstructorModel" /> class, the constructor the
        /// serializer is directed to use.
        /// </summary>
        /// <param name="a">The value.</param>
        [BencodeConstructor]
        public AttributedConstructorModel(int a)
        {
            A = a;
            SelectedConstructor = "attributed";
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <returns>The value.</returns>
        public int A { get; set; }

        /// <summary>
        /// Gets the label of the constructor that ran.
        /// </summary>
        /// <returns>The constructor label.</returns>
        public string SelectedConstructor { get; }
    }

    /// <summary>
    /// A type with a parameterless constructor and parameterized constructors, none attributed, used to confirm the
    /// parameterless constructor is preferred.
    /// </summary>
    private sealed class MixedConstructorModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MixedConstructorModel" /> class.
        /// </summary>
        public MixedConstructorModel()
        {
            SelectedConstructor = "parameterless";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MixedConstructorModel" /> class with both values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        public MixedConstructorModel(int a, int b)
        {
            A = a;
            B = b;
            SelectedConstructor = "parameterized";
        }

        /// <summary>
        /// Gets or sets the first value.
        /// </summary>
        /// <returns>The first value.</returns>
        public int A { get; set; }

        /// <summary>
        /// Gets or sets the second value.
        /// </summary>
        /// <returns>The second value.</returns>
        public int B { get; set; }

        /// <summary>
        /// Gets the label of the constructor that ran.
        /// </summary>
        /// <returns>The constructor label.</returns>
        public string SelectedConstructor { get; }
    }

    /// <summary>
    /// A type with only parameterized constructors of differing arity, used to confirm the greatest-arity constructor is
    /// chosen.
    /// </summary>
    private sealed class GreatestArityModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GreatestArityModel" /> class with one value.
        /// </summary>
        /// <param name="a">The first value.</param>
        public GreatestArityModel(int a)
        {
            A = a;
            ParameterCount = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GreatestArityModel" /> class with two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        public GreatestArityModel(int a, int b)
        {
            A = a;
            B = b;
            ParameterCount = 2;
        }

        /// <summary>
        /// Gets the first value.
        /// </summary>
        /// <returns>The first value.</returns>
        public int A { get; }

        /// <summary>
        /// Gets the second value.
        /// </summary>
        /// <returns>The second value.</returns>
        public int B { get; }

        /// <summary>
        /// Gets the parameter count of the constructor that ran.
        /// </summary>
        /// <returns>The parameter count.</returns>
        public int ParameterCount { get; }
    }

    /// <summary>
    /// A type whose constructor declares a defaulted parameter, used to confirm an absent member falls back to the
    /// parameter default.
    /// </summary>
    private sealed class OptionalParameterModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionalParameterModel" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="age">The age, defaulting to <c>99</c>.</param>
        public OptionalParameterModel(string name, int age = 99)
        {
            Name = name;
            Age = age;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; }

        /// <summary>
        /// Gets the age.
        /// </summary>
        /// <returns>The age.</returns>
        public int Age { get; }
    }

    /// <summary>
    /// A type whose constructor binds only one member, leaving the others to be set through their setters after
    /// construction.
    /// </summary>
    private sealed class PartiallyBoundModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartiallyBoundModel" /> class.
        /// </summary>
        /// <param name="name">The name bound through the constructor.</param>
        public PartiallyBoundModel(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name, bound through the constructor.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the notes, set through the setter after construction.
        /// </summary>
        /// <returns>The notes.</returns>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status, set through the setter after construction.
        /// </summary>
        /// <returns>The status.</returns>
        public int Status { get; set; }
    }
}
