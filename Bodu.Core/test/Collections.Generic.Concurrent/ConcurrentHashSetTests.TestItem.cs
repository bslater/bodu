// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.TestItem.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// A reference type with value-based equality, used to verify that the set honors a type's
    /// <see cref="object.Equals(object)" /> and <see cref="object.GetHashCode" /> implementations.
    /// </summary>
    private sealed record TestItem(int Value);
}
