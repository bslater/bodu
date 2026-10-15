// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnfoldedTestDataTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bodu.Test;

/// <summary>
/// Verifies <see cref="UnfoldedTestData" />: that it reports a method asking to unfold rows MSTest cannot serialize, and
/// nothing else.
/// </summary>
[TestClass]
public sealed class UnfoldedTestDataTests
{
    /// <summary>
    /// Verifies that a method asking to unfold rows of a serializable record is not reported.
    /// </summary>
    [TestMethod]
    public void FindRowsThatCannotUnfold_WhenRowsAreSerializable_ShouldReportNothing()
    {
        IReadOnlyList<string> problems = UnfoldedTestData.FindRowsThatCannotUnfold([typeof(SerializableRows)]);

        Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that a method asking to unfold <see cref="PropertyInfo" /> rows, which MSTest cannot serialize, is
    /// reported by name.
    /// </summary>
    [TestMethod]
    public void FindRowsThatCannotUnfold_WhenRowsCannotBeSerialized_ShouldReportTheMethod()
    {
        IReadOnlyList<string> problems = UnfoldedTestData.FindRowsThatCannotUnfold([typeof(ReflectionRows)]);

        Assert.HasCount(1, problems);
        Assert.StartsWith("ReflectionRows.Run: MSTest cannot serialize its ", problems[0]);
    }

    /// <summary>
    /// Verifies that a method asking to unfold rows that the serializer reads back changed, without throwing, is
    /// reported by name.
    /// </summary>
    [TestMethod]
    public void FindRowsThatCannotUnfold_WhenRowsChangeInARoundTrip_ShouldReportTheMethod()
    {
        IReadOnlyList<string> problems = UnfoldedTestData.FindRowsThatCannotUnfold([typeof(LossyRows)]);

        Assert.HasCount(1, problems);
        Assert.StartsWith("LossyRows.Run: MSTest reads its LossyRow rows back changed", problems[0]);
    }

    /// <summary>
    /// Verifies that rows MSTest cannot serialize are not reported when the method does not ask to unfold them.
    /// </summary>
    [TestMethod]
    public void FindRowsThatCannotUnfold_WhenMethodDoesNotAskToUnfold_ShouldReportNothing()
    {
        IReadOnlyList<string> problems = UnfoldedTestData.FindRowsThatCannotUnfold([typeof(FoldedReflectionRows)]);

        Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// A row MSTest can serialize.
    /// </summary>
    /// <param name="Name">The row's name.</param>
    /// <param name="Value">The row's value.</param>
    [Serializable]
    public sealed record SampleRow(string Name, int Value);

    /// <summary>
    /// A row whose <see cref="Value" /> the serializer skips, so a copy read back is not equal to it.
    /// </summary>
    /// <param name="Name">The row's name.</param>
    [Serializable]
    public sealed record LossyRow(string Name)
    {
        /// <summary>
        /// Gets the row's value, which is not serialized.
        /// </summary>
        /// <value>The value; zero in a copy read back.</value>
        [field: NonSerialized]
        public int Value { get; init; }
    }

    /// <summary>
    /// A method that asks to unfold rows of <see cref="SampleRow" />.
    /// </summary>
    [SuppressMessage("Usage", "MSTEST0030:Type containing [TestMethod] should be marked with [TestClass]", Justification = "A sample for UnfoldedTestData to read, which MSTest must not discover and run.")]
    public sealed class SerializableRows
    {
        /// <summary>
        /// Gets the rows.
        /// </summary>
        /// <value>Two rows.</value>
        public static IEnumerable<object[]> Rows => [[new SampleRow("a", 1)], [new SampleRow("b", 2)]];

        /// <summary>
        /// Stands for a test over the rows.
        /// </summary>
        /// <param name="row">The row.</param>
        [TestMethod(UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Unfold)]
        [DynamicData(nameof(Rows))]
        public void Run(SampleRow row) => ArgumentNullException.ThrowIfNull(row);
    }

    /// <summary>
    /// A method that asks to unfold rows of <see cref="LossyRow" />.
    /// </summary>
    [SuppressMessage("Usage", "MSTEST0030:Type containing [TestMethod] should be marked with [TestClass]", Justification = "A sample for UnfoldedTestData to read, which MSTest must not discover and run.")]
    public sealed class LossyRows
    {
        /// <summary>
        /// Gets the rows.
        /// </summary>
        /// <value>One row with a nonzero <see cref="LossyRow.Value" />.</value>
        public static IEnumerable<object[]> Rows => [[new LossyRow("a") { Value = 1 }]];

        /// <summary>
        /// Stands for a test over the rows.
        /// </summary>
        /// <param name="row">The row.</param>
        [TestMethod(UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Unfold)]
        [DynamicData(nameof(Rows))]
        public void Run(LossyRow row) => ArgumentNullException.ThrowIfNull(row);
    }

    /// <summary>
    /// A method that asks to unfold <see cref="PropertyInfo" /> rows.
    /// </summary>
    [SuppressMessage("Usage", "MSTEST0030:Type containing [TestMethod] should be marked with [TestClass]", Justification = "A sample for UnfoldedTestData to read, which MSTest must not discover and run.")]
    public sealed class ReflectionRows
    {
        /// <summary>
        /// Gets the rows.
        /// </summary>
        /// <value>One row, the <see cref="string.Length" /> property.</value>
        public static IEnumerable<object[]> Rows => [[typeof(string).GetProperty(nameof(string.Length))!]];

        /// <summary>
        /// Stands for a test over the rows.
        /// </summary>
        /// <param name="property">The row.</param>
        [TestMethod(UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Unfold)]
        [DynamicData(nameof(Rows))]
        public void Run(PropertyInfo property) => ArgumentNullException.ThrowIfNull(property);
    }

    /// <summary>
    /// A method that leaves its <see cref="PropertyInfo" /> rows folded.
    /// </summary>
    [SuppressMessage("Usage", "MSTEST0030:Type containing [TestMethod] should be marked with [TestClass]", Justification = "A sample for UnfoldedTestData to read, which MSTest must not discover and run.")]
    public sealed class FoldedReflectionRows
    {
        /// <summary>
        /// Gets the rows.
        /// </summary>
        /// <value>One row, the <see cref="string.Length" /> property.</value>
        public static IEnumerable<object[]> Rows => [[typeof(string).GetProperty(nameof(string.Length))!]];

        /// <summary>
        /// Stands for a test over the rows.
        /// </summary>
        /// <param name="property">The row.</param>
        [TestMethod]
        [DynamicData(nameof(Rows))]
        public void Run(PropertyInfo property) => ArgumentNullException.ThrowIfNull(property);
    }
}
