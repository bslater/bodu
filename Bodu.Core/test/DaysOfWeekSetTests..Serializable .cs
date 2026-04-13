// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Serializable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.Serialization;

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet" /> can be serialized via
        /// <see cref="System.Runtime.Serialization.ISerializable.GetObjectData" /> and reconstructed via
        /// the private serialization constructor, producing an instance equal to the original.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        [DataRow((byte)0b0111110)] // weekdays
        [DataRow((byte)0b1000001)] // weekend
        [DataRow((byte)127)]
        public void Serialization_WhenRoundTripped_ShouldProduceEqualInstance(byte mask)
        {
            var original = DaysOfWeekSet.FromByte(mask);

            var info = new SerializationInfo(typeof(DaysOfWeekSet), new FormatterConverter());
            var context = new StreamingContext(StreamingContextStates.All);

            ((ISerializable)original).GetObjectData(info, context);

            var ctor = typeof(DaysOfWeekSet).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(SerializationInfo), typeof(StreamingContext) },
                modifiers: null);

            Assert.IsNotNull(ctor, "The private serialization constructor must exist on DaysOfWeekSet.");

            var deserialized = (DaysOfWeekSet)ctor.Invoke(new object[] { info, context });

            Assert.AreEqual(original, deserialized,
                $"Round-tripped instance must equal the original for bitmask {mask}.");
        }

        /// <summary>
        /// Verifies that <see cref="System.Runtime.Serialization.ISerializable.GetObjectData" /> throws
        /// <see cref="ArgumentNullException" /> when a <see langword="null" />
        /// <see cref="SerializationInfo" /> is supplied.
        /// </summary>
        [TestMethod]
        public void GetObjectData_WhenInfoIsNull_ShouldThrowArgumentNullException()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            var context = new StreamingContext(StreamingContextStates.All);

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ((ISerializable)set).GetObjectData(null!, context);
            });
        }

        /// <summary>
        /// Verifies that the private serialization constructor throws <see cref="SerializationException" />
        /// when the stored bitmask value exceeds the valid range of 0–127.
        /// </summary>
        [TestMethod]
        public void SerializationConstructor_WhenStoredValueIsOutOfRange_ShouldThrowSerializationException()
        {
            var info = new SerializationInfo(typeof(DaysOfWeekSet), new FormatterConverter());
            var context = new StreamingContext(StreamingContextStates.All);

            // Manually inject an out-of-range value using the same key the implementation writes.
            info.AddValue("_selectedDays", (byte)200);

            var ctor = typeof(DaysOfWeekSet).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(SerializationInfo), typeof(StreamingContext) },
                modifiers: null);

            Assert.IsNotNull(ctor, "The private serialization constructor must exist on DaysOfWeekSet.");

            var ex = Assert.ThrowsExactly<TargetInvocationException>(() =>
                ctor.Invoke(new object[] { info, context }));

            Assert.IsInstanceOfType<SerializationException>(ex.InnerException,
                "The inner exception must be SerializationException for an out-of-range stored value.");
        }
    }
}