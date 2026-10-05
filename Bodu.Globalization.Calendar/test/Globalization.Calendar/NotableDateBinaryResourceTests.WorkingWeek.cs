// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResourceTests.WorkingWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateBinaryResourceTests
{
    /// <summary>
    /// Verifies that a pack stores the working week as one byte with Sunday at bit 6 and Saturday at bit 0, the layout
    /// of every pack already written, so that those packs keep reading.
    /// </summary>
    /// <param name="workingWeek">The working week to store.</param>
    /// <param name="expected">The byte the pack must hold.</param>
    [TestMethod]
    [DataRow(WorkingDaysOfWeek.MondayToFriday, (byte)0b011_1110)]
    [DataRow(WorkingDaysOfWeek.SundayToThursday, (byte)0b111_1100)]
    [DataRow(WorkingDaysOfWeek.MondayToThursdayAndSaturday, (byte)0b011_1101)]
    [DataRow(WorkingDaysOfWeek.SaturdayToWednesday, (byte)0b111_1001)]
    [DataRow(WorkingDaysOfWeek.AllDays, (byte)0b111_1111)]
    public void Write_WhenWorkingWeekIsSet_ShouldStoreSundayAtBitSix(WorkingDaysOfWeek workingWeek, byte expected)
    {
        byte[] pack = WritePack(ResourceWithWorkingWeek(workingWeek.ToDayOfWeekSet()));

        Assert.AreEqual(expected, pack[WorkingWeekOffset()]);
    }

    /// <summary>
    /// Verifies that a pack's working-week byte, Sunday at bit 6 and Saturday at bit 0, reads back as the working week
    /// it stores.
    /// </summary>
    /// <param name="workingWeek">The working week the byte stores.</param>
    /// <param name="stored">The byte in the pack.</param>
    [TestMethod]
    [DataRow(WorkingDaysOfWeek.MondayToFriday, (byte)0b011_1110)]
    [DataRow(WorkingDaysOfWeek.SundayToThursday, (byte)0b111_1100)]
    [DataRow(WorkingDaysOfWeek.MondayToThursdayAndSaturday, (byte)0b011_1101)]
    [DataRow(WorkingDaysOfWeek.SaturdayToWednesday, (byte)0b111_1001)]
    [DataRow(WorkingDaysOfWeek.AllDays, (byte)0b111_1111)]
    public void Read_WhenWorkingWeekByteIsSet_ShouldReadSundayFromBitSix(WorkingDaysOfWeek workingWeek, byte stored)
    {
        byte[] pack = WritePack(ResourceWithWorkingWeek(DayOfWeekSet.Empty));
        pack[WorkingWeekOffset()] = stored;
        RehashPayload(pack);

        NotableDateResource resource = ReadPack(pack);

        Assert.AreEqual(workingWeek.ToDayOfWeekSet(), resource.ResolutionPolicy.WorkingWeek);
    }

    /// <summary>
    /// Builds a resource with no rules or adjustments whose resolution policy has the specified working week.
    /// </summary>
    /// <param name="workingWeek">The working week.</param>
    /// <returns>The resource.</returns>
    private static NotableDateResource ResourceWithWorkingWeek(DayOfWeekSet workingWeek) =>
        new("test.binary.working-week", "1.0", new ResolutionPolicy(workingWeek: workingWeek), [], []);

    /// <summary>
    /// Finds the offset of the working-week byte in a pack: the one payload byte that differs between packs that differ
    /// only in working week.
    /// </summary>
    /// <returns>The byte's offset from the start of the pack.</returns>
    private static int WorkingWeekOffset()
    {
        byte[] none = WritePack(ResourceWithWorkingWeek(DayOfWeekSet.Empty));
        byte[] all = WritePack(ResourceWithWorkingWeek(DayOfWeekSet.All));
        Assert.AreEqual(none.Length, all.Length, "Packs that differ only in working week should be the same length.");

        int[] differing = Enumerable.Range(NotableDateBinaryFormat.HeaderLength, none.Length - NotableDateBinaryFormat.HeaderLength)
            .Where(i => none[i] != all[i])
            .ToArray();

        Assert.HasCount(1, differing, "Packs that differ only in working week should differ in one payload byte.");
        return differing[0];
    }
}
