// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.ReferenceItem.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// A reference type that intentionally does not override Equals or GetHashCode,
    /// so that equality falls back to reference identity. Used to test Contains behaviour
    /// for types without custom equality.
    /// </summary>
    private sealed class ReferenceItem
    {

        public ReferenceItem(int value) => Value = value;

        public int Value { get; }

    }

}
