// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.GetObjectData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#pragma warning disable SYSLIB0050 // CrcStandard intentionally implements ISerializable; these tests exercise that contract.

using System.Runtime.Serialization;

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcStandardTests
{
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
            types: [typeof(SerializationInfo), typeof(StreamingContext)],
            modifiers: null);
        Assert.IsNotNull(deserCtor, "Private deserialisation constructor must exist.");

        var restored = (CrcStandard)deserCtor!.Invoke([info, new StreamingContext(StreamingContextStates.All)]);
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

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(            () =>
        {
            ((ISerializable)standard).GetObjectData(null!, new StreamingContext(StreamingContextStates.All));
        });
        Assert.AreEqual("info", ex.ParamName);
    }
}
