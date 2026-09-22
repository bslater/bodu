// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg.Samples.MessageReading.Scenarios;

namespace Bodu.Formats.Outlook.Msg.Samples.MessageReading;

/// <summary>
/// Entry point for the Outlook message sample: reading a <c>.msg</c> file with
/// <c>Bodu.Formats.Outlook.Msg</c>. The sample writes its own input first, using
/// <c>Bodu.IO.Compound</c> to author a small MS-OXMSG container, so it ships no binary fixture and
/// runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Authors a message into a temporary file, runs every scenario over it, then deletes it.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Formats.Outlook.Msg.Samples.MessageReading");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        // The reader is read-only by design, so the sample authors its own input rather than
        // shipping a .msg fixture. MsgAuthor.cs documents the layout it writes.
        var path = Path.Combine(Path.GetTempPath(), $"bodu-sample-{Guid.NewGuid():N}.msg");

        try
        {
            MsgAuthor.WriteSampleMessage(path);
            Console.WriteLine($"Authored a {new FileInfo(path).Length}-byte .msg to read back.");
            Console.WriteLine();

            ReadingAMessage.Run(path);
            PropertyModel.Run(path);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        Console.WriteLine("Done.");
    }
}
