namespace Bodu.Extensions
{
    [TestClass]
    public partial class ComparableHelperTests
    {
        /// <summary>
        /// A simple test object that implements <see cref="IComparable{T}" /> for use in comparison-based unit tests.
        /// </summary>
        public sealed class SimpleTestObject : IComparable<SimpleTestObject>
        {
            /// <summary>
            /// Gets the integer value used for comparison.
            /// </summary>
            public int Value { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="SimpleTestObject" /> class.
            /// </summary>
            /// <param name="value">The value to assign for comparison purposes.</param>
            public SimpleTestObject(int value)
            {
                Value = value;
            }

            /// <inheritdoc />
            public int CompareTo(SimpleTestObject? other)
            {
                if (other is null)
                    return 1; // Non-null is greater than null
                return Value.CompareTo(other.Value);
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
            {
                return obj is SimpleTestObject other && Value == other.Value;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return $"SimpleTestObject({Value})";
            }
        }
    }
}