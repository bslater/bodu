// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.TestItem.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        private sealed record TestItem
        {
            public int Value { get; set; }

            public TestItem(int value) { Value = value; }

            public override string ToString() => $"Item({Value})";
        }
    }
}
