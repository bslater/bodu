// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReadingAMessage.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook;

namespace Bodu.Formats.Outlook.Msg.Samples.MessageReading.Scenarios;

/// <summary>
/// Demonstrates opening a <c>.msg</c> file with <see cref="OutlookMessage" /> and reading the parts a consumer
/// usually wants: the scalar conveniences, the recipient table, and the attachments.
/// </summary>
public static class ReadingAMessage
{
    /// <summary>
    /// Opens the authored message and prints its headline fields, recipients, and attachment content.
    /// </summary>
    /// <param name="path">The message file to read.</param>
    public static void Run(string path)
    {
        SampleConsole.Scenario(
            "Opening a .msg and reading subject, recipients, and attachments",
            what: "Opens the message written by this sample and prints the subject, sender, body, every recipient "
                + "with its type, and each attachment's name, size, and bytes.",
            why: "A .msg is an OLE2 compound file full of MAPI property streams - recipients and attachments are "
                + "child storages, and every scalar is a tagged record whose value may be inline or in a separate "
                + "stream named after its tag. Reading one by hand means implementing that layout, which is what "
                + "MsgAuthor.cs in this sample does in order to produce the input. The reader turns all of it into "
                + "ordinary properties and collections, so a consumer extracting mail never touches a tag or a "
                + "stream name.",
            expect: "The subject, sender, and body come back exactly as written. Two recipients print with their "
                + "types resolved to To and Cc rather than the raw 1 and 2 stored on the wire, and the single "
                + "attachment's CSV content reads back byte for byte.");

        // IsMsgFile sniffs the container before committing to a read - useful when the extension is not trusted.
        using (var probe = File.OpenRead(path))
        {
            Console.WriteLine($"  IsMsgFile             : {OutlookMessage.IsMsgFile(probe)}   (checks the container signature rather than trusting the file extension)");
        }

        using var message = OutlookMessage.OpenRead(path);

        Console.WriteLine($"  Subject               : {message.Subject}");
        Console.WriteLine($"  Sender                : {message.SenderName} <{message.SenderEmailAddress}>");
        Console.WriteLine($"  Body                  : {message.BodyText?.Replace("\r\n", " / ")}");
        Console.WriteLine();

        Console.WriteLine($"  Recipients ({message.Recipients.Count}):");
        foreach (OutlookRecipient recipient in message.Recipients)
        {
            // RecipientType is an enum here; on the wire it is the integer 1, 2, or 3.
            Console.WriteLine($"    {recipient.RecipientType,-4} {recipient.DisplayName,-16} <{recipient.EmailAddress}>");
        }

        Console.WriteLine();
        Console.WriteLine($"  Attachments ({message.Attachments.Count}):");
        foreach (OutlookAttachment attachment in message.Attachments)
        {
            Console.WriteLine($"    {attachment.FileName,-16} method={attachment.Method,-12} {attachment.Size} bytes");

            // OpenContentStream hands back the attachment's bytes without materializing the whole message.
            using var content = attachment.OpenContentStream();
            using var reader = new StreamReader(content);
            var text = reader.ReadToEnd();

            foreach (var line in text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
                Console.WriteLine($"      | {line}");
        }

        Console.WriteLine();
    }
}
