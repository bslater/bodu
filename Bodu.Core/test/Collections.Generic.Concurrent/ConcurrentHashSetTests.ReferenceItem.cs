// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.ReferenceItem.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// A reference type that uses the default reference-identity equality (no <see cref="object.Equals(object)" />
    /// override), used to verify reference-identity and custom-comparer behavior.
    /// </summary>
    private sealed class ReferenceItem
    {
        public ReferenceItem(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
