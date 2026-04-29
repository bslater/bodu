// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Serializable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.Serialization;

namespace Bodu;

#pragma warning disable SYSLIB0050 // Type or member is obsolete (formatter-based serialization)

public partial class WeekPatternTests
{
    /// <summary>
    /// Verifies that a <see cref="WeekPattern" /> can be serialised and reconstructed via the private
    /// serialisation constructor, producing an instance equal to the original.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0)]
    [DataRow((byte)1)]
    [DataRow((byte)0b0111110)]
    [DataRow((byte)0b1000001)]
    [DataRow((byte)127)]
    public void Serialization_WhenRoundTripped_ShouldProduceEqualInstance(byte mask)
    {
        var original = WeekPattern.FromByte(mask);
        var info = new SerializationInfo(typeof(WeekPattern), new FormatterConverter());
        var context = new StreamingContext(StreamingContextStates.All);

        ((ISerializable)original).GetObjectData(info, context);

        var ctor = typeof(WeekPattern).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(SerializationInfo), typeof(StreamingContext) },
            modifiers: null);

        Assert.IsNotNull(ctor, "The private serialisation constructor must exist on WeekPattern.");

        var deserialized = (WeekPattern)ctor.Invoke(new object[] { info, context });
        Assert.AreEqual(original, deserialized,
            $"Round-tripped instance must equal the original for bitmask {mask}.");
    }

    /// <summary>
    /// Verifies that <see cref="ISerializable.GetObjectData" /> throws <see cref="ArgumentNullException" />
    /// when a <see langword="null" /> <see cref="SerializationInfo" /> is supplied.
    /// </summary>
    [TestMethod]
    public void GetObjectData_WhenInfoIsNull_ShouldThrowException()
    {
        var pattern = new WeekPattern(DayOfWeek.Monday);
        var context = new StreamingContext(StreamingContextStates.All);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            ((ISerializable)pattern).GetObjectData(null!, context));
    }

    /// <summary>
    /// Verifies that the private serialisation constructor throws <see cref="SerializationException" />
    /// when the stored bitmask value exceeds the valid range of 0–127.
    /// </summary>
    [TestMethod]
    public void SerializationConstructor_WhenStoredValueIsOutOfRange_ShouldThrowException()
    {
        var info = new SerializationInfo(typeof(WeekPattern), new FormatterConverter());
        var context = new StreamingContext(StreamingContextStates.All);

        info.AddValue("_selectedDays", (byte)200);

        var ctor = typeof(WeekPattern).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(SerializationInfo), typeof(StreamingContext) },
            modifiers: null);

        Assert.IsNotNull(ctor);

        var ex = Assert.ThrowsExactly<TargetInvocationException>(() =>
            ctor.Invoke(new object[] { info, context }));

        Assert.IsInstanceOfType<SerializationException>(ex.InnerException);
    }
}

#pragma warning restore SYSLIB0050
