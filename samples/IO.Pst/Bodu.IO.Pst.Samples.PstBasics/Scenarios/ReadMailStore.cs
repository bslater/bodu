// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReadMailStore.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook;

namespace Bodu.IO.Pst.Samples.PstBasics.Scenarios;

/// <summary>
/// Demonstrates the mail-store view layered on the container by <c>Bodu.Formats.Outlook.Pst</c>: the same
/// file opened as an <see cref="OutlookMailStore" /> session, walking folders and messages with decoded MAPI
/// properties, recipients, attachments, bodies, and named-property resolution.
/// </summary>
public static class ReadMailStore
{
    /// <summary>
    /// Walks the sample store's folder hierarchy and dumps each message's headline facts.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- The mail-store view (Bodu.Formats.Outlook.Pst) ---");

        using var store = OutlookMailStore.OpenRead(Program.SamplePath);
        Console.WriteLine($"store: {store.DisplayName}");

        Walk(store.RootFolder, depth: 0);

        // Named-property resolution is store-wide: identifiers at or above 0x8000 map through the
        // file's name-to-id map to a durable (property-set GUID, numeric id or name) identity.
        var tag = new MapiPropertyTag(0x8000, MapiPropertyType.Unicode);
        if (store.TryGetPropertyName(tag, out MapiNamedProperty name))
            Console.WriteLine($"named property 0x8000 resolves to {name}");

        Console.WriteLine();
    }

    /// <summary>
    /// Recursively prints one folder subtree.
    /// </summary>
    /// <param name="folder">The folder to print.</param>
    /// <param name="depth">The indent depth.</param>
    private static void Walk(OutlookMailFolder folder, int depth)
    {
        string indent = new(' ', depth * 2);
        Console.WriteLine($"{indent}[{folder.DisplayName ?? "(unnamed)"}] " +
            $"({folder.MessageCount?.ToString() ?? "?"} messages)");

        foreach (OutlookMailMessage message in folder.EnumerateMessages())
        {
            Console.WriteLine($"{indent}  {message.Subject} — {message.SenderName}");

            foreach (OutlookRecipient recipient in message.Recipients)
                Console.WriteLine($"{indent}    to {recipient.DisplayName ?? recipient.EmailAddress}");

            foreach (OutlookMailAttachment attachment in message.Attachments)
                Console.WriteLine($"{indent}    attachment: {attachment}");

            if (message.BodyText is string body)
            {
                string preview = body.Length > 60 ? body[..60].ReplaceLineEndings(" ") + "…" : body.ReplaceLineEndings(" ");
                Console.WriteLine($"{indent}    body: {preview}");
            }
        }

        foreach (OutlookMailFolder child in folder.EnumerateSubfolders())
            Walk(child, depth + 1);
    }
}
