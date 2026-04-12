using System.Reflection;

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that the CircularBuffer enumerator is defined as a value type (struct).
        /// </summary>
        [TestMethod]
        [TestCategory("Structural")]
        public void StructureOf_CircularBufferEnumerator_ShouldBeStructType()
        {
            var enumeratorType = typeof(CircularBuffer<int>.Enumerator);
            Assert.IsTrue(enumeratorType.IsValueType, "Enumerator must be a value type (struct).");
        }

        /// <summary>
        /// Verifies that all public properties of the CircularBuffer enumerator are immutable (no public setters).
        /// </summary>
        [TestMethod]
        [TestCategory("Structural")]
        public void StructureOf_CircularBufferEnumerator_ShouldExposeOnlyImmutablePublicProperties()
        {
            var enumeratorType = typeof(CircularBuffer<int>.Enumerator);

            var mutableProperties = enumeratorType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
                .ToList();

            Assert.AreEqual(0, mutableProperties.Count,
                $"Enumerator exposes mutable public properties: {string.Join(", ", mutableProperties.Select(p => p.Name))}");
        }
    }
}