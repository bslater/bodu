using System.Collections;

namespace Bodu.Collections
{
    /// <summary>
    /// Represents a typed test plan for recursive or iterator-based methods.
    /// </summary>
    /// <typeparam name="TSource">The input sequence element type.</typeparam>
    public class EnumerableTestPlan<TSource>
    {
        public string Name { get; }

        /// <summary>
        /// The strongly-typed source input sequence.
        /// </summary>
        public IEnumerable<TSource> Source { get; }

        /// <summary>
        /// The strongly-typed selector used to transform results into comparable values.
        /// </summary>
        public Func<TSource, object> ResultSelector { get; }

        /// <summary>
        /// The strongly-typed transformation or query logic to test.
        /// </summary>
        public Func<IEnumerable<TSource>, IEnumerable> Invoke { get; }

        /// <summary>
        /// The expected transformed actual.
        /// </summary>
        public IEnumerable<object> ExpectedResult { get; }

        public override string ToString() => Name;

        public EnumerableTestPlan(string name, IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable> invoke, IEnumerable<object> expectedResult, Func<TSource, object> resultSelector = null)
        {
            Name = name;
            Source = source;
            Invoke = invoke;
            ExpectedResult = expectedResult;
            ResultSelector = resultSelector;
        }
    }
}