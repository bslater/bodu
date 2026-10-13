// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatBindingTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides tests for the shared <see cref="FlatBinding" />, compiled into the test project of each line format that
/// compiles it.
/// </summary>
[TestClass]
public sealed partial class FlatBindingTests
{
    /// <summary>
    /// A value type, which can always be created.
    /// </summary>
    private struct ValueSample
    {
        /// <summary>
        /// Gets or sets a number.
        /// </summary>
        public int Number { get; set; }
    }

    /// <summary>
    /// A class with a public parameterless constructor.
    /// </summary>
    private sealed class CreatableSample
    {
        /// <summary>
        /// Gets or sets a number.
        /// </summary>
        public int Number { get; set; }
    }

    /// <summary>
    /// An abstract class, which cannot be created.
    /// </summary>
    private abstract class AbstractSample
    {
    }

    /// <summary>
    /// A class whose only constructor takes an argument, so it cannot be created.
    /// </summary>
    private sealed class ConstructedSample
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructedSample" /> class.
        /// </summary>
        /// <param name="number">The number.</param>
        public ConstructedSample(int number) =>
            Number = number;

        /// <summary>
        /// Gets the number.
        /// </summary>
        public int Number { get; }
    }

    /// <summary>
    /// Creates the exception the tests expect for a type that cannot be created.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The exception.</returns>
    private static InvalidOperationException CreateNotCreatableException(Type type) =>
        new(type.Name);
}
