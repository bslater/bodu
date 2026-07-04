// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.SlotLayout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    /// <summary>
    /// Verifies that the private <c>Slot</c> struct declares its hot fields (<c>Sequence</c> and <c>Value</c>) before
    /// its cache-line padding fields. Under <see cref="System.Runtime.InteropServices.LayoutKind.Sequential" /> the
    /// declaration order is the memory order, so the padding must trail the hot fields to isolate one slot's hot
    /// fields from the next slot's — padding declared ahead of them does not.
    /// </summary>
    /// <remarks>
    /// This guards the intent of the false-sharing mitigation; the actual throughput benefit is validated by a
    /// benchmark, not a unit test.
    /// </remarks>
    [TestMethod]
    public void Slot_ShouldDeclareHotFieldsBeforePaddingFields()
    {
        Type slotType = typeof(ConcurrentCircularBuffer<object>).GetNestedType("Slot", BindingFlags.NonPublic)!;
        Assert.IsNotNull(slotType, "The private Slot struct could not be located via reflection.");

        FieldInfo[] fields = slotType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        int lastHotIndex = -1;
        int firstPaddingIndex = int.MaxValue;

        for (int i = 0; i < fields.Length; i++)
        {
            string name = fields[i].Name;
            if (name is "Sequence" or "Value")
                lastHotIndex = Math.Max(lastHotIndex, i);
            else
                firstPaddingIndex = Math.Min(firstPaddingIndex, i);
        }

        Assert.AreNotEqual(-1, lastHotIndex, "Expected to find the Sequence/Value hot fields on Slot.");
        Assert.IsLessThan(firstPaddingIndex, lastHotIndex,
            "Slot's hot fields (Sequence, Value) must be declared before any padding field so the trailing padding " +
            "isolates the slot from the next slot's hot fields.");
    }
}
