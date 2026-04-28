// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

using System.Runtime.Serialization;

/// <summary>
/// Contains unit tests for <see cref="CrcStandard" /> covering construction guards, equality semantics, and the
/// <see cref="ISerializable" /> round-trip contract.
/// </summary>
[TestClass]
public partial class CrcStandardTests
{
    private static CrcStandard CreateReference(
        string name = "Test",
        int size = 32,
        ulong polynomial = 0x04C11DB7UL,
        ulong initialValue = 0xFFFFFFFFUL,
        bool reflectIn = true,
        bool reflectOut = true,
        ulong xOrOut = 0xFFFFFFFFUL)
        => new(name, size, polynomial, initialValue, reflectIn, reflectOut, xOrOut);

    /// <summary>
    /// Verifies that passing <see langword="null" /> as the CRC name to the constructor throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> equal to <c>name</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(
            () => new CrcStandard(null!, 32, 0x04C11DB7UL, 0xFFFFFFFFUL, true, true, 0xFFFFFFFFUL));
        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that passing an empty string as the CRC name to the constructor throws
    /// <see cref="ArgumentException" /> with <c>ParamName</c> equal to <c>name</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNameIsEmpty_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(
            () => new CrcStandard(string.Empty, 32, 0x04C11DB7UL, 0xFFFFFFFFUL, true, true, 0xFFFFFFFFUL));
        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="CrcStandard" /> with a <paramref name="size" /> below
    /// <see cref="CrcStandard.MinSize" /> throws <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c>
    /// equal to <c>size</c>.
    /// </summary>
    /// <param name="size">The invalid width under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void Ctor_WhenSizeIsBelowMin_ShouldThrowArgumentOutOfRangeException(int size)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new CrcStandard("Test", size, 0x1UL, 0x0UL, false, false, 0x0UL));
        Assert.AreEqual("size", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="CrcStandard" /> with a <paramref name="size" /> above
    /// <see cref="CrcStandard.MaxSize" /> throws <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c>
    /// equal to <c>size</c>.
    /// </summary>
    /// <param name="size">The invalid width under test.</param>
    [TestMethod]
    [DataRow(65)]
    [DataRow(128)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenSizeIsAboveMax_ShouldThrowArgumentOutOfRangeException(int size)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new CrcStandard("Test", size, 0x1UL, 0x0UL, false, false, 0x0UL));
        Assert.AreEqual("size", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a successfully constructed <see cref="CrcStandard" /> returns the supplied parameter values
    /// through every init-only property.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParametersAreValid_ShouldRoundTripAllInitOnlyValues()
    {
        CrcStandard standard = new(
            name: "Round-Trip",
            size: 24,
            polynomial: 0x864CFBUL,
            initialValue: 0xB704CEUL,
            reflectIn: false,
            reflectOut: true,
            xOrOut: 0xDEADBEEFUL);

        Assert.AreEqual("Round-Trip", standard.Name);
        Assert.AreEqual(24, standard.Size);
        Assert.AreEqual(0x864CFBUL, standard.Polynomial);
        Assert.AreEqual(0xB704CEUL, standard.InitialValue);
        Assert.IsFalse(standard.ReflectIn);
        Assert.IsTrue(standard.ReflectOut);
        Assert.AreEqual(0xDEADBEEFUL, standard.XOrOut);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.Equals(CrcStandard?)" /> returns <see langword="false" /> when the
    /// comparand is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenTypedOtherIsNull_ShouldReturnFalse()
    {
        CrcStandard a = CreateReference();
        Assert.IsFalse(a.Equals(null));
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.Equals(object?)" /> returns <see langword="false" /> when the
    /// comparand is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenObjectOtherIsNull_ShouldReturnFalse()
    {
        CrcStandard a = CreateReference();
        Assert.IsFalse(a.Equals((object?)null));
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.Equals(object?)" /> returns <see langword="false" /> when the
    /// comparand is not a <see cref="CrcStandard" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenObjIsDifferentType_ShouldReturnFalse()
    {
        CrcStandard a = CreateReference();
        Assert.IsFalse(a.Equals("not a CrcStandard"));
    }

    /// <summary>
    /// Verifies that two <see cref="CrcStandard" /> instances with identical parameters compare equal and
    /// produce the same hash code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAllFieldsMatch_ShouldReturnTrueAndEqualHashCode()
    {
        CrcStandard a = CreateReference();
        CrcStandard b = CreateReference();

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals((object)b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that changing any single field between two <see cref="CrcStandard" /> instances causes
    /// <see cref="CrcStandard.Equals(CrcStandard?)" /> to return <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAnySingleFieldDiffers_ShouldReturnFalse()
    {
        CrcStandard baseStandard = CreateReference();

        Assert.IsFalse(baseStandard.Equals(CreateReference(name: "Other")));
        Assert.IsFalse(baseStandard.Equals(CreateReference(size: 16)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(polynomial: 0x1021UL)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(initialValue: 0UL)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(reflectIn: false)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(reflectOut: false)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(xOrOut: 0UL)));
    }

    /// <summary>
    /// Verifies that <see cref="ISerializable.GetObjectData" /> populates the
    /// <see cref="SerializationInfo" /> with the expected field set, and that a round-trip through the
    /// private deserialisation constructor reproduces an equal instance.
    /// </summary>
    [TestMethod]
    public void GetObjectData_WhenRoundTripped_ShouldProduceEqualInstance()
    {
        CrcStandard original = CreateReference(name: "Round-Trip", size: 24);

        SerializationInfo info = new(typeof(CrcStandard), new FormatterConverter());
        ((ISerializable)original).GetObjectData(info, new StreamingContext(StreamingContextStates.All));

        Assert.AreEqual(original.Name, info.GetString(nameof(CrcStandard.Name)));
        Assert.AreEqual(original.Size, info.GetInt32(nameof(CrcStandard.Size)));
        Assert.AreEqual(original.Polynomial, info.GetUInt64(nameof(CrcStandard.Polynomial)));
        Assert.AreEqual(original.InitialValue, info.GetUInt64(nameof(CrcStandard.InitialValue)));
        Assert.AreEqual(original.ReflectIn, info.GetBoolean(nameof(CrcStandard.ReflectIn)));
        Assert.AreEqual(original.ReflectOut, info.GetBoolean(nameof(CrcStandard.ReflectOut)));
        Assert.AreEqual(original.XOrOut, info.GetUInt64(nameof(CrcStandard.XOrOut)));

        // Replay the serialised payload through the private deserialisation constructor to prove the type can
        // reconstruct itself from its own GetObjectData output.
        System.Reflection.ConstructorInfo? deserCtor = typeof(CrcStandard).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(SerializationInfo), typeof(StreamingContext) },
            modifiers: null);
        Assert.IsNotNull(deserCtor, "Private deserialisation constructor must exist.");

        CrcStandard restored = (CrcStandard)deserCtor!.Invoke(new object[] { info, new StreamingContext(StreamingContextStates.All) });
        Assert.IsTrue(original.Equals(restored));
        Assert.AreEqual(original.GetHashCode(), restored.GetHashCode());
    }

    /// <summary>
    /// Verifies that <see cref="ISerializable.GetObjectData" /> throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> equal to <c>info</c> when invoked with a <see langword="null" />
    /// <see cref="SerializationInfo" />.
    /// </summary>
    [TestMethod]
    public void GetObjectData_WhenInfoIsNull_ShouldThrowArgumentNullException()
    {
        CrcStandard standard = CreateReference();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(
            () => ((ISerializable)standard).GetObjectData(null!, new StreamingContext(StreamingContextStates.All)));
        Assert.AreEqual("info", ex.ParamName);
    }
}
