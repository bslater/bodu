// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.TestItem.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    private sealed record TestItem
    {
        public int Value { get; set; }

        public TestItem(int value) { Value = value; }

        public override string ToString() => $"Item({Value})";
    }
}
