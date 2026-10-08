// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetTests.TryFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu;

public partial class DayOfWeekSetTests
{
    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider)" /> and
    /// <see cref="DayOfWeekSet.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider)" /> write, for all 128 sets in
    /// every format and the default, the text <see cref="DayOfWeekSet.ToString(string)" /> returns.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenAnySetIsWrittenInAnyFormat_ShouldWriteTheTextToStringReturns()
    {
        char[] chars = new char[64];
        byte[] bytes = new byte[64];

        for (ulong bits = 0; bits <= 0x7F; bits++)
        {
            DayOfWeekSet set = DayOfWeekSet.FromUInt64(bits);

            foreach (string format in FormatsAndDefault)
            {
                string expected = set.ToString(format);

                Assert.IsTrue(set.TryFormat(chars, out int charsWritten, format), $"bits {bits:X2}, format '{format}'");
                Assert.AreEqual(expected, new string(chars, 0, charsWritten), $"bits {bits:X2}, format '{format}'");
                Assert.IsTrue(set.TryFormat(bytes, out int bytesWritten, format), $"bits {bits:X2}, format '{format}', UTF-8");
                Assert.AreEqual(expected, Encoding.UTF8.GetString(bytes, 0, bytesWritten), $"bits {bits:X2}, format '{format}', UTF-8");
            }
        }
    }
}
