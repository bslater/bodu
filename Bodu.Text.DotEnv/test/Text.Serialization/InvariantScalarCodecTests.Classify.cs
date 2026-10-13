// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantScalarCodecTests.Classify.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class InvariantScalarCodecTests
{
    /// <summary>
    /// Verifies that every primitive type, <see cref="nint" /> and <see cref="nuint" /> included, is a scalar, as the
    /// binders' former scalar test treated it.
    /// </summary>
    [TestMethod]
    public void Classify_WhenTypeIsAPrimitive_ShouldReturnAKind()
    {
        Type[] primitives =
        [
            typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int),
            typeof(uint), typeof(long), typeof(ulong), typeof(nint), typeof(nuint), typeof(float), typeof(double),
        ];

        foreach (Type type in primitives)
            Assert.AreNotEqual(FlatScalarKind.None, InvariantScalarCodec.Classify(type), type.Name);
    }

    /// <summary>
    /// Verifies that the nullable form of a scalar has the kind of the scalar it makes nullable.
    /// </summary>
    [TestMethod]
    public void Classify_WhenTypeIsNullable_ShouldReturnTheKindOfItsUnderlyingType()
    {
        Assert.AreEqual(FlatScalarKind.NativeInteger, InvariantScalarCodec.Classify(typeof(nint?)));
        Assert.AreEqual(FlatScalarKind.DateOnly, InvariantScalarCodec.Classify(typeof(DateOnly?)));
        Assert.AreEqual(FlatScalarKind.Enum, InvariantScalarCodec.Classify(typeof(DayOfWeek?)));
    }

    /// <summary>
    /// Verifies that an enumeration is classified as <see cref="FlatScalarKind.Enum" />, whatever its underlying type.
    /// </summary>
    [TestMethod]
    public void Classify_WhenTypeIsAnEnumeration_ShouldReturnEnum()
    {
        Assert.AreEqual(FlatScalarKind.Enum, InvariantScalarCodec.Classify(typeof(DayOfWeek)));
        Assert.AreEqual(FlatScalarKind.Enum, InvariantScalarCodec.Classify(typeof(ConsoleKey)));
    }

    /// <summary>
    /// Verifies that a type mapped to members, a collection and <see cref="object" /> are not scalars.
    /// </summary>
    [TestMethod]
    public void Classify_WhenTypeIsNotAScalar_ShouldReturnNone()
    {
        Assert.AreEqual(FlatScalarKind.None, InvariantScalarCodec.Classify(typeof(object)));
        Assert.AreEqual(FlatScalarKind.None, InvariantScalarCodec.Classify(typeof(List<int>)));
        Assert.AreEqual(FlatScalarKind.None, InvariantScalarCodec.Classify(typeof(InvariantScalarCodecTests)));
        Assert.IsFalse(InvariantScalarCodec.IsScalar(typeof(Version)));
    }

    /// <summary>
    /// Verifies that every scalar type the codec lists reads back, under both policies, the text written for a value
    /// of it, so that no scalar a binder writes is one it cannot read.
    /// </summary>
    [TestMethod]
    public void ScalarTypes_WhenAValueOfEachIsFormatted_ShouldParseBackToTheValue()
    {
        foreach (Type type in InvariantScalarCodec.ScalarTypes)
        {
            object value = type == typeof(string) ? "text"
                : type == typeof(Uri) ? new Uri("https://example.com/a")
                : type == typeof(char) ? 'c'
                : Activator.CreateInstance(type)!;

            foreach (InvariantScalarCodec.Policy policy in Enum.GetValues<InvariantScalarCodec.Policy>())
            {
                string text = InvariantScalarCodec.Format(value, policy);

                object? parsed = InvariantScalarCodec.Parse(text, type, policy);

                Assert.AreEqual(value, parsed, $"{type.Name} under {policy}: {text}");
            }
        }
    }
}
